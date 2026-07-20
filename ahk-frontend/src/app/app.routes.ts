import { Routes } from '@angular/router';

import { authGuard, adminGuard } from './core/auth/auth.guard';
import { courseGuard } from './core/course/course.guard';
import { Shell } from './layout/shell/shell';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/login/login').then((m) => m.Login),
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
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  { path: '**', redirectTo: 'login' },
];
