import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';

import { AuthClient, CurrentUserResponse, LoginRequest } from '../../api/api-client';
import { readApiError } from '../api-error';

/**
 * Session state for the SPA. Hydrates from GET /api/auth/me (cookie-based), exposes the current user and
 * derived flags as signals, and wraps the generated AuthClient for login/logout.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly authClient = inject(AuthClient);

  private readonly user = signal<CurrentUserResponse | null>(null);
  private readonly loaded = signal(false);

  readonly currentUser = this.user.asReadonly();
  readonly isAuthenticated = computed(() => this.user() !== null);
  readonly isAdmin = computed(() => this.user()?.roles?.includes('Admin') ?? false);

  /**
   * Every course this user can open. The API already folds a site admin's implicit access into this list, so
   * the course switcher and the course guard both read it without a special case.
   */
  readonly courses = computed(() => this.user()?.courses ?? []);

  /** Loads the session once (cached). Returns the user, or null if not authenticated. */
  ensureLoaded(): Observable<CurrentUserResponse | null> {
    if (this.loaded()) {
      return of(this.user());
    }
    return this.authClient.me().pipe(
      tap((u) => this.setUser(u)),
      catchError(() => {
        this.setLoggedOut();
        return of(null);
      }),
    );
  }

  /**
   * Signs in. Resolves to null on success, or to the reason it failed — the API's own message for a bad
   * password or a locked-out account, and a distinct one when the backend cannot be reached at all.
   */
  login(userName: string, password: string): Observable<string | null> {
    const request: LoginRequest = { userName, password, rememberMe: true };
    return this.authClient.login(request).pipe(
      tap((u) => this.setUser(u)),
      map(() => null),
      catchError((err: unknown) => of(readApiError(err, 'That username and password do not match an account.'))),
    );
  }

  /**
   * Clears the portal session. When the API returns an `endSessionUrl` the provider supports RP-initiated
   * logout, so the browser is sent there to end the SSO session too; the BME IdP advertises no such endpoint
   * today, so sign-out is local-only and this stays null.
   */
  logout(): Observable<void> {
    return this.authClient.logout().pipe(
      tap((result) => {
        this.setLoggedOut();
        if (result?.endSessionUrl) {
          window.location.href = result.endSessionUrl;
        }
      }),
      map(() => undefined),
      catchError(() => {
        this.setLoggedOut();
        return of(undefined);
      }),
    );
  }

  isMemberOf(slug: string): boolean {
    return this.courses().some((c) => c.slug === slug);
  }

  /** Where signing in should land: the admin console for admins, otherwise the first course they can open. */
  landingUrl(): string {
    if (this.isAdmin()) {
      return '/admin/courses';
    }
    const first = this.courses()[0];
    return first ? `/${first.slug}/dashboard` : '/no-access';
  }

  private setUser(u: CurrentUserResponse): void {
    this.user.set(u);
    this.loaded.set(true);
  }

  private setLoggedOut(): void {
    this.user.set(null);
    this.loaded.set(true);
  }
}
