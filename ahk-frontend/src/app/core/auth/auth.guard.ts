import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs/operators';

import { AuthService } from './auth.service';

/** Requires an authenticated session; otherwise redirects to /login preserving the target URL. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.ensureLoaded().pipe(
    map((user) => (user ? true : router.createUrlTree(['/login'], { queryParams: { returnUrl: state.url } }))),
  );
};

/** Requires the site-admin role; otherwise sends the user to their default landing. */
export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.ensureLoaded().pipe(map(() => (auth.isAdmin() ? true : router.createUrlTree(['/login']))));
};
