import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

/** Local username/password login plus an "OIDC login" entry point (navigates to the backend challenge). */
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

  protected submit(): void {
    this.error.set(null);
    this.busy.set(true);
    this.auth.login(this.userName, this.password).subscribe((ok) => {
      this.busy.set(false);
      if (ok) {
        this.router.navigateByUrl(this.landingUrl());
      } else {
        this.error.set('Invalid username or password.');
      }
    });
  }

  protected loginWithOidc(): void {
    // Full-page navigation so the browser follows the OIDC redirect chain; proxied to the backend in dev.
    const returnUrl = this.landingUrl();
    window.location.href = `/api/auth/external/challenge?returnUrl=${encodeURIComponent(returnUrl)}`;
  }

  private landingUrl(): string {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
    if (returnUrl) {
      return returnUrl;
    }
    if (this.auth.isAdmin()) {
      return '/admin/courses';
    }
    const first = this.auth.courses()[0];
    return first ? `/${first.slug}/dashboard` : '/login';
  }
}
