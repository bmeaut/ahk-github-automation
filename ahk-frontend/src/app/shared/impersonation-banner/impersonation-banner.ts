import { Component, inject, signal } from '@angular/core';

import { AuthService } from '../../core/auth/auth.service';

/**
 * The strip that says "you are not looking at your own account", plus the way back.
 *
 * Rendered from the root component rather than the shell on purpose: impersonating a student lands on /my,
 * which is deliberately outside the course shell, and that is precisely the session an admin most needs an
 * exit from. Renders nothing at all in an ordinary session.
 */
@Component({
  selector: 'app-impersonation-banner',
  templateUrl: './impersonation-banner.html',
  styleUrl: './impersonation-banner.scss',
})
export class ImpersonationBanner {
  private readonly auth = inject(AuthService);

  protected readonly user = this.auth.currentUser;
  protected readonly isImpersonating = this.auth.isImpersonating;
  protected readonly impersonator = this.auth.impersonatorUserName;

  protected readonly leaving = signal(false);
  protected readonly error = signal<string | null>(null);

  protected stop(): void {
    this.leaving.set(true);
    this.error.set(null);
    this.auth.stopImpersonation().subscribe((error) => {
      // On success the page navigates away, so only a failure ever gets to re-enable the button.
      this.leaving.set(false);
      this.error.set(error);
    });
  }
}
