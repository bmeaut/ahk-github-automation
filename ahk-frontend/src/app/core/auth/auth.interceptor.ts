import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

/**
 * Redirects to the login page on an unexpected 401. The session-probe endpoint (/api/auth/me) is exempt:
 * a 401 there is the normal "not logged in yet" signal handled by the auth guard, not an error to redirect on.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  return next(req).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && !req.url.includes('/api/auth/me')) {
        void router.navigate(['/login']);
      }
      return throwError(() => error);
    }),
  );
};
