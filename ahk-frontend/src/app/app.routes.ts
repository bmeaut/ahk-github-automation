import { Routes } from '@angular/router';

import { adminGuard, authGuard, rootRedirectGuard } from './core/auth/auth.guard';
import { courseGuard } from './core/course/course.guard';
import { Shell } from './layout/shell/shell';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login').then((m) => m.Login),
  },
  {
    // Signed in, but assigned to nothing yet. Without this the guards would bounce such a user back to the
    // login screen they just came from, with no explanation.
    path: 'no-access',
    canActivate: [authGuard],
    loadComponent: () => import('./features/no-access/no-access').then((m) => m.NoAccess),
  },
  {
    // Host/admin context (no course segment).
    path: 'admin',
    component: Shell,
    canActivate: [authGuard, adminGuard],
    children: [
      {
        path: 'courses',
        loadComponent: () => import('./features/admin/courses/courses').then((m) => m.AdminCourses),
      },
      {
        path: 'courses/:id',
        loadComponent: () => import('./features/admin/courses/course-editor').then((m) => m.CourseEditor),
      },
      {
        path: 'users',
        loadComponent: () => import('./features/admin/users/users').then((m) => m.AdminUsers),
      },
      {
        path: 'health',
        loadComponent: () => import('./features/admin/health/health').then((m) => m.AdminHealth),
      },
      { path: '', pathMatch: 'full', redirectTo: 'courses' },
    ],
  },
  {
    // Course context: /{course}/...
    path: ':course',
    component: Shell,
    canActivate: [authGuard, courseGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/course/dashboard/dashboard').then((m) => m.CourseDashboard),
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  // "/" resolves per user rather than always going to the login form: the OIDC callback lands here when it
  // has no return URL, and by then the session already exists.
  { path: '', pathMatch: 'full', canActivate: [rootRedirectGuard], children: [] },
  { path: '**', redirectTo: '' },
];
