import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, switchMap } from 'rxjs';

import {
  CourseDto,
  CourseRole,
  CoursesAdminClient,
  CreateUserRequest,
  UserAccessTokenDto,
  UserDto,
  UsersAdminClient,
} from '../../../api/api-client';
import { readApiError } from '../../../core/api-error';
import { AuthService } from '../../../core/auth/auth.service';

const SITE_ADMIN = 'Admin';

/**
 * The register of accounts: who exists, what they can do site-wide, and which courses they are assigned to.
 * Roles and course assignments are edited in place — they are the two things an admin changes often, and a
 * separate edit screen for each would double the clicks.
 */
@Component({
  selector: 'app-admin-users',
  imports: [FormsModule, DatePipe],
  templateUrl: './users.html',
  styleUrl: './users.scss',
})
export class AdminUsers implements OnInit {
  private readonly client = inject(UsersAdminClient);
  private readonly coursesClient = inject(CoursesAdminClient);
  private readonly auth = inject(AuthService);

  protected readonly users = signal<UserDto[]>([]);
  protected readonly total = signal(0);
  protected readonly courses = signal<CourseDto[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly saved = signal<string | null>(null);

  protected search = '';
  protected courseFilter = '';
  private readonly pageSize = 50;
  protected readonly skip = signal(0);

  private readonly searchTerm = new Subject<string>();

  /** The user row currently expanded for course assignment. */
  protected readonly expanded = signal<number | null>(null);

  /** The expanded user's access tokens, loaded when the drawer opens rather than for every row in the page. */
  protected readonly tokens = signal<UserAccessTokenDto[]>([]);
  protected readonly loadingTokens = signal(false);

  protected readonly adding = signal(false);
  protected readonly savingNew = signal(false);
  protected newUserName = '';
  protected newEmail = '';
  protected newDisplayName = '';
  protected newNeptun = '';
  protected newPassword = '';

  protected readonly showingRange = computed(() => {
    const from = this.users().length === 0 ? 0 : this.skip() + 1;
    return { from, to: this.skip() + this.users().length };
  });

  ngOnInit(): void {
    this.coursesClient.list().subscribe({
      next: (list) => this.courses.set(list),
      error: () => this.error.set('The course list could not be loaded, so course assignment is unavailable.'),
    });

    // Typing a search re-queries the server rather than filtering a page, so a name outside the first 50 rows
    // is still findable.
    this.searchTerm
      .pipe(
        debounceTime(250),
        distinctUntilChanged(),
        switchMap(() => {
          this.loading.set(true);
          return this.query();
        }),
      )
      .subscribe({
        next: (page) => {
          this.users.set(page.items ?? []);
          this.total.set(page.total ?? 0);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('The users could not be loaded.');
          this.loading.set(false);
        },
      });

    this.reload();
  }

  private query() {
    return this.client.list(
      this.search.trim() || undefined,
      this.courseFilter ? Number(this.courseFilter) : undefined,
      this.skip(),
      this.pageSize,
    );
  }

  protected reload(): void {
    this.loading.set(true);
    this.query().subscribe({
      next: (page) => {
        this.users.set(page.items ?? []);
        this.total.set(page.total ?? 0);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('The users could not be loaded.');
        this.loading.set(false);
      },
    });
  }

  protected onSearchChange(): void {
    this.skip.set(0);
    this.searchTerm.next(`${this.search}|${this.courseFilter}`);
  }

  protected page(delta: number): void {
    this.skip.set(Math.max(0, this.skip() + delta * this.pageSize));
    this.reload();
  }

  // ---- Impersonation ----

  /** Own row: there is nothing to impersonate, and the API refuses it anyway. */
  protected canImpersonate(user: UserDto): boolean {
    return user.id !== this.auth.currentUser()?.userId;
  }

  /** On success the browser navigates away as the other user, so only a failure returns here. */
  protected impersonate(user: UserDto): void {
    this.clearMessages();
    this.auth.impersonate(user.id ?? 0).subscribe((error) => {
      if (error) {
        this.error.set(error);
      }
    });
  }

  // ---- Site role ----

  protected isSiteAdmin(user: UserDto): boolean {
    return (user.roles ?? []).includes(SITE_ADMIN);
  }

  protected toggleSiteAdmin(user: UserDto, makeAdmin: boolean): void {
    this.clearMessages();
    const roles = makeAdmin ? [SITE_ADMIN] : [];

    this.client.updateRoles(user.id ?? 0, { roles }).subscribe({
      next: (updated) => {
        this.replace(updated);
        this.saved.set(
          makeAdmin
            ? `${updated.userName} is now a site admin.`
            : `${updated.userName} is no longer a site admin.`,
        );
      },
      error: (err: unknown) => {
        // The API refuses to let an admin strip their own role; surface its reason rather than a generic one.
        this.error.set(readApiError(err, 'That role could not be changed.'));
        this.reload();
      },
    });
  }

  // ---- Course assignment ----

  /**
   * The expanded user's tokens. Values are never returned — an administrator is here to cut a token off, and
   * one who genuinely has to act as someone has impersonation for that.
   */
  private loadTokens(user: UserDto): void {
    this.tokens.set([]);
    if (this.expanded() !== user.id) {
      return;
    }

    this.loadingTokens.set(true);
    this.client.listTokens(user.id ?? 0).subscribe({
      next: (tokens) => {
        this.tokens.set(tokens);
        this.loadingTokens.set(false);
      },
      error: () => this.loadingTokens.set(false),
    });
  }

  protected revokeToken(user: UserDto, token: UserAccessTokenDto): void {
    this.error.set(null);
    this.saved.set(null);

    this.client.revokeToken(user.id ?? 0, token.id ?? 0).subscribe({
      next: () => {
        this.saved.set(`A token of ${user.userName} was revoked.`);
        this.loadTokens(user);
      },
      error: (err: unknown) => this.error.set(readApiError(err, 'That token could not be revoked.')),
    });
  }

  protected toggleExpanded(user: UserDto): void {
    this.expanded.set(this.expanded() === user.id ? null : (user.id ?? null));
    this.loadTokens(user);
  }

  protected assignableCourses(user: UserDto): CourseDto[] {
    const assigned = new Set((user.courses ?? []).map((c) => c.courseId));
    return this.courses().filter((c) => !assigned.has(c.id));
  }

  protected assign(user: UserDto, courseId: string, role: CourseRole): void {
    if (!courseId) {
      return;
    }
    this.clearMessages();
    this.client.upsertCourse(user.id ?? 0, { courseId: Number(courseId), role }).subscribe({
      next: (updated) => {
        this.replace(updated);
        this.saved.set(`${updated.userName}'s course assignments were updated.`);
      },
      error: () => this.error.set('That course could not be assigned.'),
    });
  }

  protected changeCourseRole(user: UserDto, courseId: number, role: string): void {
    this.clearMessages();
    this.client.upsertCourse(user.id ?? 0, { courseId, role: role as CourseRole }).subscribe({
      next: (updated) => this.replace(updated),
      error: () => this.error.set('That role could not be changed.'),
    });
  }

  protected unassign(user: UserDto, courseId: number): void {
    this.clearMessages();
    this.client.removeCourse(user.id ?? 0, courseId).subscribe({
      next: (updated) => this.replace(updated),
      error: () => this.error.set('That course could not be removed.'),
    });
  }

  // ---- Create ----

  protected createUser(): void {
    this.clearMessages();
    this.savingNew.set(true);

    const request: CreateUserRequest = {
      userName: this.newUserName.trim(),
      email: this.newEmail.trim() || undefined,
      displayName: this.newDisplayName.trim() || undefined,
      neptunCode: this.newNeptun.trim() || undefined,
      password: this.newPassword,
    };

    this.client.create(request).subscribe({
      next: (user) => {
        this.savingNew.set(false);
        this.cancelAdding();
        this.saved.set(`${user.userName} was created.`);
        this.reload();
      },
      error: (err: unknown) => {
        this.savingNew.set(false);
        this.error.set(readApiError(err, 'That account could not be created.'));
      },
    });
  }

  /**
   * Fill the password field with a fresh 16-character secret drawn from the CSPRNG. The character set spans
   * upper/lower letters, digits and symbols; rejection sampling keeps the distribution uniform (a plain
   * `% length` would bias toward the first characters). We seed one of each class up front so the result
   * always satisfies a mixed-complexity policy, then shuffle so the class positions are not predictable.
   */
  protected generatePassword(): void {
    const classes = [
      'ABCDEFGHIJKLMNOPQRSTUVWXYZ',
      'abcdefghijklmnopqrstuvwxyz',
      '0123456789',
      '!@#$%^&*()-_=+[]{};:,.?',
    ];
    const all = classes.join('');
    const length = 16;

    // Uniform integer in [0, bound) via rejection sampling — a plain `% bound` would bias toward small values.
    const randomInt = (bound: number): number => {
      const max = Math.floor(256 / bound) * bound;
      const buf = new Uint8Array(1);
      let byte: number;
      do {
        crypto.getRandomValues(buf);
        byte = buf[0];
      } while (byte >= max);
      return byte % bound;
    };

    const pick = (set: string): string => set[randomInt(set.length)];

    const chars = classes.map((set) => pick(set));
    while (chars.length < length) {
      chars.push(pick(all));
    }

    // Fisher–Yates with CSPRNG-drawn indices so the guaranteed one-per-class characters are not stuck at the front.
    for (let i = chars.length - 1; i > 0; i--) {
      const j = randomInt(i + 1);
      [chars[i], chars[j]] = [chars[j], chars[i]];
    }

    this.newPassword = chars.join('');
  }

  protected cancelAdding(): void {
    this.adding.set(false);
    this.newUserName = '';
    this.newEmail = '';
    this.newDisplayName = '';
    this.newNeptun = '';
    this.newPassword = '';
  }

  protected deleteUser(user: UserDto): void {
    this.clearMessages();
    this.client.delete(user.id ?? 0).subscribe({
      next: () => {
        this.saved.set(`${user.userName} was deleted.`);
        this.reload();
      },
      error: (err: unknown) => this.error.set(readApiError(err, 'That account could not be deleted.')),
    });
  }

  private replace(user: UserDto): void {
    this.users.set(this.users().map((u) => (u.id === user.id ? user : u)));
  }

  private clearMessages(): void {
    this.error.set(null);
    this.saved.set(null);
  }
}
