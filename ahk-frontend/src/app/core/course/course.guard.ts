import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs/operators';

import { AuthService } from '../auth/auth.service';
import { CourseContextService } from './course-context.service';

/**
 * Guards /{course}/... routes: the course must be one the user can open. Site admins can open every course,
 * which the API already reflects in the list this reads, so no special case is needed here. On success the
 * course context is set so the shell can display it. Backend authorization is the real gate; this just avoids
 * navigating into a course that would only return 403.
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
      return router.createUrlTree([auth.landingUrl()]);
    }),
  );
};

/**
 * Guards /admin/courses/:id, the course-management screen. It is not an <code>adminGuard</code> route: a course
 * admin reaches it for their own course, and a site admin holds that role on every course (the API synthesizes
 * it), so one membership check covers both. The course context is set either way, which is what gives the page
 * a course rail. Backend authorization is the real gate; this only avoids landing on a screen that would 403.
 */
export const courseManageGuard: CanActivateFn = (route) => {
  const auth = inject(AuthService);
  const courseContext = inject(CourseContextService);
  const router = inject(Router);
  const id = Number(route.paramMap.get('id'));

  return auth.ensureLoaded().pipe(
    map((user) => {
      if (!user) {
        return router.createUrlTree(['/login']);
      }
      const course = auth.courses().find((c) => c.id === id);
      if (course?.role === 'Admin' && course.slug) {
        courseContext.setActiveSlug(course.slug);
        return true;
      }
      return router.createUrlTree([auth.landingUrl()]);
    }),
  );
};
