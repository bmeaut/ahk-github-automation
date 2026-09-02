import { Routes } from '@angular/router';

import { adminGuard, authGuard, rootRedirectGuard } from './core/auth/auth.guard';
import { courseGuard, courseManageGuard } from './core/course/course.guard';
import { Shell } from './layout/shell/shell';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login').then((m) => m.Login),
  },
  {
    // A student's own repositories, across every course, and where signing in lands anyone who staffs none.
    // Its empty state explains what to do next, which is why there is no separate "no access" screen: a user
    // with no courses is a student who has not accepted an assignment yet, not an error.
    path: 'my',
    canActivate: [authGuard],
    loadComponent: () => import('./features/my/my-assignments').then((m) => m.MyAssignments),
  },
  {
    // The course-management screen. Declared before 'admin' so it matches first, and deliberately outside
    // that route: its guard is adminGuard, which would bounce the course admins this screen exists for.
    path: 'admin/courses/:id',
    component: Shell,
    canActivate: [authGuard, courseManageGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/admin/courses/course-editor').then((m) => m.CourseEditor),
      },
    ],
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
        path: 'users',
        loadComponent: () => import('./features/admin/users/users').then((m) => m.AdminUsers),
      },
      {
        path: 'health',
        loadComponent: () => import('./features/admin/health/health').then((m) => m.AdminHealth),
      },
      {
        path: 'deliveries',
        loadComponent: () => import('./features/admin/deliveries/deliveries').then((m) => m.AdminDeliveries),
      },
      {
        path: 'help/github',
        loadComponent: () => import('./features/admin/help/github-setup').then((m) => m.GitHubSetupHelp),
      },
      { path: '', pathMatch: 'full', redirectTo: 'courses' },
    ],
  },
  {
    // The assignment invite link. Deliberately outside the course shell and guarded only by authGuard:
    // students are members of no course, and accepting is how they first appear in one at all — courseGuard
    // would bounce every one of them. Declared before ':course' so the match is unambiguous.
    path: ':course/invite/:token',
    canActivate: [authGuard],
    loadComponent: () => import('./features/invite/invite').then((m) => m.Invite),
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
      {
        path: 'assignments',
        loadComponent: () => import('./features/course/assignments/assignments').then((m) => m.CourseAssignments),
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  // "/" resolves per user rather than always going to the login form: the OIDC callback lands here when it
  // has no return URL, and by then the session already exists.
  { path: '', pathMatch: 'full', canActivate: [rootRedirectGuard], children: [] },
  { path: '**', redirectTo: '' },
];
