import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

import { AssignmentInviteClient, InviteState, ProfileClient } from '../../api/api-client';
import { readApiError } from '../../core/api-error';
import { AuthService } from '../../core/auth/auth.service';

/**
 * What a student sees when they follow an assignment's invite link — the replacement for GitHub Classroom's
 * accept page.
 *
 * One component for the whole flow, driven entirely by the `status` the API returns rather than by local
 * step-counting: the server decides what is still missing, and re-decides it on accept, so a reload or a
 * back-button never lands the student in a state the server disagrees with.
 */
@Component({
  selector: 'app-invite',
  imports: [FormsModule],
  templateUrl: './invite.html',
  styleUrl: './invite.scss',
})
export class Invite implements OnInit, OnDestroy {
  private readonly inviteClient = inject(AssignmentInviteClient);
  private readonly profileClient = inject(ProfileClient);
  private readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  protected readonly state = signal<InviteState | null>(null);
  protected readonly loading = signal(true);
  protected readonly accepting = signal(false);
  protected readonly error = signal<string | null>(null);

  /** Counts down the redirect to GitHub, so the confirmation is readable before the page moves on. */
  protected readonly redirectingIn = signal<number | null>(null);

  protected gitHubUsername = '';
  protected readonly savingUsername = signal(false);

  private course = '';
  private token = '';
  private redirectTimer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.course = this.route.snapshot.paramMap.get('course') ?? '';
    this.token = this.route.snapshot.paramMap.get('token') ?? '';
    this.load();
  }

  ngOnDestroy(): void {
    this.clearRedirect();
  }

  private load(): void {
    this.loading.set(true);
    this.inviteClient.get(this.token, this.course).subscribe({
      next: (state) => {
        this.state.set(state);
        this.loading.set(false);
      },
      error: (err: unknown) => {
        this.error.set(readApiError(err, 'This invite link could not be opened.'));
        this.loading.set(false);
      },
    });
  }

  /** Verified server-side against the GitHub API, so a typo comes back as a message rather than a broken repo. */
  protected saveGitHubUsername(): void {
    const login = this.gitHubUsername.trim();
    if (!login) {
      return;
    }

    this.error.set(null);
    this.savingUsername.set(true);

    this.profileClient.setGitHubUsername({ gitHubUsername: login, courseSlug: this.course }).subscribe({
      next: () => {
        this.savingUsername.set(false);
        this.gitHubUsername = '';

        // The session carries the username too, and the student may go to /my straight afterwards.
        this.auth.reload().subscribe();
        this.load();
      },
      error: (err: unknown) => {
        this.savingUsername.set(false);
        this.error.set(readApiError(err, 'That username could not be checked. Try again in a moment.'));
      },
    });
  }

  protected accept(): void {
    this.error.set(null);
    this.accepting.set(true);

    this.inviteClient.accept(this.token, this.course).subscribe({
      next: (state) => {
        this.accepting.set(false);
        this.state.set(state);

        // Only send them onward when the repository is actually open to them. With an invitation still
        // outstanding the repository 404s, so the page keeps them here and points at the invitation instead.
        if (state.status === 'Accepted' && state.repoUrl && !state.invitationUrl) {
          this.startRedirect(state.repoUrl);
        }
      },
      error: (err: unknown) => {
        this.accepting.set(false);
        this.error.set(readApiError(err, 'The repository could not be set up. Tell your instructor.'));
      },
    });
  }

  protected openRepository(): void {
    const url = this.state()?.invitationUrl ?? this.state()?.repoUrl;
    if (url) {
      this.clearRedirect();
      window.location.href = url;
    }
  }

  protected cancelRedirect(): void {
    this.clearRedirect();
  }

  private startRedirect(url: string): void {
    this.redirectingIn.set(4);
    this.redirectTimer = setInterval(() => {
      const left = (this.redirectingIn() ?? 0) - 1;
      if (left <= 0) {
        this.clearRedirect();
        window.location.href = url;
        return;
      }
      this.redirectingIn.set(left);
    }, 1000);
  }

  private clearRedirect(): void {
    if (this.redirectTimer) {
      clearInterval(this.redirectTimer);
      this.redirectTimer = undefined;
    }
    this.redirectingIn.set(null);
  }
}
