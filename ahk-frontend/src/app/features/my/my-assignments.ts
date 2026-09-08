import { DatePipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { MyAssignmentsClient, ProfileClient, StudentRepository } from '../../api/api-client';
import { readApiError } from '../../core/api-error';
import { AuthService } from '../../core/auth/auth.service';

/** One course's worth of repositories, which is how the page is grouped. */
interface CourseGroup {
  slug: string;
  name: string;
  repositories: StudentRepository[];
}

/**
 * A student's own page: every repository they hold, across every course.
 *
 * This is also where a student lands when they sign in without an invite link, so it doubles as the
 * "you have nothing yet" screen — the empty state has to explain what to do, not just report a void.
 */
@Component({
  selector: 'app-my-assignments',
  imports: [DatePipe, FormsModule, RouterLink],
  templateUrl: './my-assignments.html',
  styleUrl: './my-assignments.scss',
})
export class MyAssignments implements OnInit {
  private readonly client = inject(MyAssignmentsClient);
  private readonly profileClient = inject(ProfileClient);
  private readonly auth = inject(AuthService);

  protected readonly repositories = signal<StudentRepository[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly resending = signal<number | null>(null);
  protected readonly note = signal<string | null>(null);

  protected readonly user = this.auth.currentUser;
  protected readonly canManageTokens = this.auth.canManageTokens;

  /** The GitHub username editor, open only while the account is unconfirmed. */
  protected readonly editingGitHub = signal(false);
  protected readonly savingGitHub = signal(false);
  protected gitHubUsername = '';

  /** Grouped by course, preserving the API's newest-first order within each. */
  protected readonly groups = computed<CourseGroup[]>(() => {
    const byCourse = new Map<string, CourseGroup>();

    for (const repository of this.repositories()) {
      const slug = repository.courseSlug ?? '';
      const group = byCourse.get(slug) ?? { slug, name: repository.courseName ?? slug, repositories: [] };
      group.repositories.push(repository);
      byCourse.set(slug, group);
    }

    return [...byCourse.values()];
  });

  ngOnInit(): void {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.client.list().subscribe({
      next: (repositories) => {
        this.repositories.set(repositories);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(readApiError(err, 'Your assignments could not be loaded.'));
        this.loading.set(false);
      },
    });
  }

  /**
   * GitHub cannot extend an invitation, so this withdraws the stale one and issues a fresh one. The student
   * still has to click through it on GitHub — the button only puts a live invitation back in their inbox.
   */
  protected resend(repository: StudentRepository): void {
    const id = repository.acceptanceId ?? 0;

    this.error.set(null);
    this.note.set(null);
    this.resending.set(id);

    this.client.resendInvitation(id).subscribe({
      next: (updated) => {
        this.resending.set(null);
        this.repositories.update((list) => list.map((r) => (r.acceptanceId === id ? updated : r)));

        this.note.set(
          updated.access === 'Active'
            ? 'You already have access to that repository — no invitation was needed.'
            : 'A new invitation is on its way. Open it from the link next to the repository, or from your GitHub notifications.',
        );
      },
      error: (err: unknown) => {
        this.resending.set(null);
        this.error.set(readApiError(err, 'The invitation could not be sent. Try again in a few minutes.'));
      },
    });
  }

  /**
   * Opens the editor on the username as it stands, so a misspelling is corrected rather than retyped. Only
   * reachable while the account is unconfirmed — once an invitation has been accepted with it, the backend
   * refuses the change and the button is not rendered.
   */
  protected editGitHubUsername(): void {
    this.gitHubUsername = this.user()?.gitHubUsername ?? '';
    this.error.set(null);
    this.note.set(null);
    this.editingGitHub.set(true);
  }

  protected cancelGitHubUsername(): void {
    this.editingGitHub.set(false);
  }

  /**
   * Saves the corrected username. The repositories already created keep their names — only who they are shared
   * with changes — so the reply's count is what the student needs to hear: an invitation is waiting for the
   * account they just named.
   */
  protected saveGitHubUsername(): void {
    const login = this.gitHubUsername.trim();
    if (!login) {
      return;
    }

    this.error.set(null);
    this.note.set(null);
    this.savingGitHub.set(true);

    this.profileClient.setGitHubUsername({ gitHubUsername: login }).subscribe({
      next: (profile) => {
        this.savingGitHub.set(false);
        this.editingGitHub.set(false);

        const shared = profile.repositoriesShared ?? 0;
        const failed = profile.repositoriesFailed ?? 0;
        const account = `Your GitHub account is now ${profile.gitHubUsername}`;

        // One message, not two: the rename and what it did to the repositories are the same event, and a
        // green "saved" above a red "but not everywhere" reads as two contradictory outcomes.
        if (failed > 0) {
          this.error.set(
            `${account}, but ${failed === 1 ? 'one repository could' : `${failed} repositories could`} not be shared with it just now. Use "Send a new invitation" below, or try again in a few minutes.`,
          );
        } else if (shared > 0) {
          this.note.set(
            `${account}, and ${shared === 1 ? 'your repository was' : `all ${shared} of your repositories were`} shared with it. Accept the invitation${shared === 1 ? '' : 's'} on GitHub to get in.`,
          );
        } else {
          this.note.set(`${account}.`);
        }

        // The session carries the username and its confirmed state; the list carries the invitation state.
        this.auth.reload().subscribe();
        this.load();
      },
      error: (err: unknown) => {
        this.savingGitHub.set(false);
        this.error.set(readApiError(err, 'That username could not be checked. Try again in a moment.'));
      },
    });
  }

  protected logout(): void {
    this.auth.logout().subscribe();
  }
}
