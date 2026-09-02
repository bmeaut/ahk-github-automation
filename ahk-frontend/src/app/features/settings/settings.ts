import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { PersonalAccessTokenDto, PersonalAccessTokensClient } from '../../api/api-client';
import { readApiError } from '../../core/api-error';
import { AuthService } from '../../core/auth/auth.service';
import { copyToClipboard } from '../../core/clipboard';

/**
 * The signed-in user's own account page, which today means their access tokens: credentials they mint for
 * themselves so a script can read their courses' statuses and grades without driving the login form.
 *
 * Standalone rather than inside the shell, like {@link MyAssignments}: a student is a member of no course, so
 * the shell's course rail and switcher would have nothing to put in them.
 */
@Component({
  selector: 'app-settings',
  imports: [FormsModule, DatePipe, RouterLink],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
})
export class Settings implements OnInit {
  private readonly client = inject(PersonalAccessTokensClient);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly tokens = signal<PersonalAccessTokenDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly saved = signal<string | null>(null);

  protected readonly user = this.auth.currentUser;

  /** The site's own origin, so the example command is one this reader can paste as it stands. */
  protected readonly origin = window.location.origin;

  protected newDescription = '';
  protected readonly creating = signal(false);

  /** The token just minted, shown in full above the list so it can be copied straight away. */
  protected readonly issued = signal<PersonalAccessTokenDto | null>(null);

  /** Token ids currently shown in the clear; they are masked otherwise, even from their owner's shoulder. */
  protected readonly revealed = signal<Set<number>>(new Set());

  /** Key of the value most recently copied, so only the button pressed says "Copied". */
  protected readonly copied = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    this.loading.set(true);
    this.client.list().subscribe({
      next: (tokens) => {
        this.tokens.set(tokens);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(readApiError(err, 'Your access tokens could not be loaded.'));
        this.loading.set(false);
      },
    });
  }

  protected create(): void {
    this.clearMessages();
    this.creating.set(true);

    this.client.create({ description: this.newDescription.trim() || undefined }).subscribe({
      next: (token) => {
        this.creating.set(false);
        this.newDescription = '';
        this.issued.set(token);
        this.load();
      },
      error: (err: unknown) => {
        this.creating.set(false);
        this.error.set(readApiError(err, 'The token could not be created.'));
      },
    });
  }

  protected revoke(token: PersonalAccessTokenDto): void {
    this.clearMessages();

    this.client.revoke(token.id ?? 0).subscribe({
      next: () => {
        this.saved.set('That token was revoked. Anything using it is cut off immediately.');
        this.load();
      },
      error: (err: unknown) => this.error.set(readApiError(err, 'The token could not be revoked.')),
    });
  }

  protected dismissIssued(): void {
    this.issued.set(null);
  }

  /** Shows or hides one token's value inline, so it can be read as well as copied. */
  protected toggleReveal(token: PersonalAccessTokenDto): void {
    const id = token.id ?? 0;
    const next = new Set(this.revealed());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.revealed.set(next);
  }

  protected isRevealed(token: PersonalAccessTokenDto): boolean {
    return this.revealed().has(token.id ?? 0);
  }

  /** What the list shows until Show is pressed: the prefix, then the last four characters. */
  protected mask(token: PersonalAccessTokenDto): string {
    const value = token.token ?? '';
    return value.length <= 12 ? '••••' : `${value.slice(0, 5)}…${value.slice(-4)}`;
  }

  /**
   * Copies one value, with "Copied" feedback on the button that was pressed. The key identifies that button,
   * so two rows next to each other never both claim to have been copied.
   */
  protected async copy(key: string, value: string | undefined): Promise<void> {
    if (!value) {
      return;
    }

    if (await copyToClipboard(value)) {
      this.copied.set(key);
      setTimeout(() => {
        if (this.copied() === key) {
          this.copied.set(null);
        }
      }, 2500);
    } else {
      this.error.set('That value could not be copied automatically — select it and copy it by hand.');
    }
  }

  /** The first course the user can open, so the example command is one they can actually run. */
  protected exampleCourse(): string {
    return this.auth.courses()[0]?.slug ?? '{course}';
  }

  protected logout(): void {
    this.auth.logout().subscribe(() => this.router.navigate(['/login']));
  }

  private clearMessages(): void {
    this.error.set(null);
    this.saved.set(null);
  }
}
