import { Routes } from '@angular/router';
import { authGuard, guestGuard, roleGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'login',
    title: 'Sign in · TimeReady',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'no-access',
    title: 'No access · TimeReady',
    canActivate: [authGuard],
    loadComponent: () => import('./features/auth/no-access.component').then(m => m.NoAccessComponent)
  },
  {
    path: 'dashboard',
    title: 'Team overview · TimeReady',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'employees',
    title: 'Employees · TimeReady',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/employees/employee-list.component').then(m => m.EmployeeListComponent)
  },
  {
    path: 'notifications',
    title: 'Notifications · TimeReady',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/notifications/notifications.component').then(m => m.NotificationsComponent)
  },
  {
    path: 'audit',
    title: 'Audit trail · TimeReady',
    canActivate: [roleGuard('Admin')],
    loadComponent: () => import('./features/audit/audit.component').then(m => m.AuditComponent)
  },
  { path: '**', redirectTo: 'dashboard' }
];
