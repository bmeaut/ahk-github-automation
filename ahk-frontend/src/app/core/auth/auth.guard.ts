import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs/operators';

import { CourseContextService } from '../course/course-context.service';
import { AuthService } from './auth.service';

/** Requires an authenticated session; otherwise redirects to /login preserving the target URL. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.ensureLoaded().pipe(
    map((user) => (user ? true : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } }))),
  );
};

/**
 * Sends "/" to wherever this particular user belongs. This is also where the OIDC callback lands when it has
 * no return URL to honour, so it has to work for a session that was established outside the SPA.
 */
export const rootRedirectGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.ensureLoaded().pipe(map((user) => router.createUrlTree([user ? auth.landingUrl() : '/login'])));
};

/**
 * Requires the site-admin role. Also clears the course context: the site screens have no course, and the shell
 * picks which rail to show from it.
 */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const courseContext = inject(CourseContextService);
  const router = inject(Router);

  return auth.ensureLoaded().pipe(
    map(() => {
      if (!auth.isAdmin()) {
        return router.createUrlTree([auth.landingUrl()]);
      }
      courseContext.setActiveSlug(null);
      return true;
    }),
  );
};
