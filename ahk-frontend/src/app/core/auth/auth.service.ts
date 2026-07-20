import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, of } from 'rxjs';
import { catchError, map, tap } from 'rxjs/operators';

import { AuthClient, CurrentUserResponse, LoginRequest } from '../../api/api-client';

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

  login(userName: string, password: string): Observable<boolean> {
    const request: LoginRequest = { userName, password, rememberMe: true };
    return this.authClient.login(request).pipe(
      tap((u) => this.setUser(u)),
      map(() => true),
      catchError(() => of(false)),
    );
  }

  logout(): Observable<void> {
    return this.authClient.logout().pipe(
      tap(() => this.setLoggedOut()),
      map(() => undefined),
      catchError(() => {
        this.setLoggedOut();
        return of(undefined);
      }),
    );
  }

  isMemberOf(slug: string): boolean {
    return this.isAdmin() || this.courses().some((c) => c.slug === slug);
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
