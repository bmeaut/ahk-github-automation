import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

/**
 * Sign-in screen. eduID (the institutional federated login) is the primary and expected path, so it leads.
 * Local username/password is for the handful of administrator-issued accounts, so it stays collapsed behind a
 * link and only appears when asked for.
 */
@Component({
  selector: 'app-login',
  imports: [FormsModule],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected userName = '';
  protected password = '';
  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal(false);

  /** The local username/password form is hidden until the user says they have no eduID account. */
  protected readonly showLocal = signal(false);

  protected loginWithEduId(): void {
    // Full-page navigation so the browser follows the OIDC redirect chain; proxied to the backend in dev.
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '';
    window.location.href = `/api/auth/external/challenge?returnUrl=${encodeURIComponent(returnUrl)}`;
  }

  protected revealLocal(): void {
    this.showLocal.set(true);
  }

  protected submit(): void {
    this.error.set(null);
    this.busy.set(true);
    this.auth.login(this.userName, this.password).subscribe((failure) => {
      this.busy.set(false);
      if (failure) {
        this.error.set(failure);
      } else {
        this.router.navigateByUrl(this.landingUrl());
      }
    });
  }

  private landingUrl(): string {
    return this.route.snapshot.queryParamMap.get('returnUrl') ?? this.auth.landingUrl();
  }
}
