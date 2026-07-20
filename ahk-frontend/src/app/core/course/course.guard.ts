import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs/operators';

import { AuthService } from '../auth/auth.service';
import { CourseContextService } from './course-context.service';

/**
 * Guards /{course}/... routes: the user must be a member of the course (or a site admin). On success the
 * course context is set so the shell can display it. Backend authorization is the real gate; this avoids
 * navigating into a course the user cannot access.
 */
export const courseGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const courseContext = inject(CourseContextService);
  const router = inject(Router);
  const slug = route.paramMap.get('course');

  return auth.ensureLoaded().pipe(
    map((user) => {
      if (!user) {
        return router.createUrlTree(['/login']);
      }
      if (slug && auth.isMemberOf(slug)) {
        courseContext.setActiveSlug(slug);
        return true;
      }
      return router.createUrlTree(['/login']);
    }),
  );
};
