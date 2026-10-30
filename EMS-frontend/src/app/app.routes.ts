import { Routes } from '@angular/router';

import {
  authGuard,
  guestGuard
} from './core/auth/auth-guard';

import { roleGuard } from './core/auth/role-guard';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/auth/login/login')
        .then(m => m.Login)
  },
  {
    path: '',
    canActivate: [authGuard],
    canActivateChild: [authGuard],
    loadComponent: () =>
      import('./layout/app-shell/app-shell')
        .then(m => m.AppShell),

    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard'
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/pages/dashboard/dashboard')
            .then(m => m.Dashboard)
      },
      {
        path: 'departments',
        canActivate: [roleGuard],
        data: {
          roles: ['SYSTEM_ADMIN', 'HR_MANAGER']
        },
        loadComponent: () =>
          import('./features/departments/pages/department-list/department-list')
            .then(m => m.DepartmentList)
      },
      {
        path: 'designations',
        canActivate: [roleGuard],
        data: {
          roles: ['SYSTEM_ADMIN', 'HR_MANAGER']
        },
        loadComponent: () =>
          import('./features/designations/pages/designation-list/designation-list')
            .then(m => m.DesignationList)
      },
      {
        path: 'employees',
        canActivate: [roleGuard],
        data: {
          roles: ['SYSTEM_ADMIN', 'HR_MANAGER']
        },
        loadComponent: () =>
          import('./features/employees/pages/employee-list/employee-list')
            .then(m => m.EmployeeList)
      },
      {
        path: 'settings/appearance',
        loadComponent: () =>
          import('./features/settings/appearance/appearance')
            .then(m => m.Appearance)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'dashboard'
  }
];