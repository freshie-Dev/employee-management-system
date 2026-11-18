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
        path: 'users',
        canActivate: [roleGuard],
        data: {
          roles: ['SYSTEM_ADMIN', 'HR_MANAGER']
        },
        loadComponent: () =>
          import('./features/users/pages/user-list/user-list')
            .then(m => m.UserList)
      },
      {
        path: 'shifts',
        loadComponent: () =>
          import('./features/shifts/pages/shift-list/shift-list')
            .then(m => m.ShiftList),
      },
      {
        path: 'work-schedules',
        canActivate: [roleGuard],
        data: {
          roles: ['SYSTEM_ADMIN', 'HR_MANAGER']
        },
        loadComponent: () =>
          import('./features/schedules/pages/schedule-settings/schedule-settings')
            .then(m => m.ScheduleSettingsPage)
      },
      {
        path: 'employees/:id/shifts',
        canActivate: [roleGuard],
        data: {
          roles: ['SYSTEM_ADMIN', 'HR_MANAGER']
        },
        loadComponent: () =>
          import('./features/employees-shift/pages/employee-details/employee-details')
            .then(m => m.EmployeeDetails)
      },
      {
        path: 'holiday-calendars',
        canActivate: [roleGuard],
        data: {
          roles: ['SYSTEM_ADMIN', 'HR_MANAGER']
        },
        loadComponent: () =>
          import('./features/holidays/pages/holiday-management/holiday-management')
            .then(m => m.HolidayManagement)
      },
      {
        path: 'employees/:id/attendance',
        canActivate: [roleGuard],
        data: { roles: ['SYSTEM_ADMIN', 'HR_MANAGER'] },
        loadComponent: () =>
          import('./features/attendance/pages/employee-attendance/employee-attendance')
            .then(m => m.EmployeeAttendance)
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