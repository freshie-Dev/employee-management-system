import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth-guard';
export const routes: Routes = [
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./features/auth/login/login').then(m => m.Login) },
  {
    path: '', canActivate: [authGuard], canActivateChild: [authGuard],
    loadComponent: () => import('./layout/app-shell/app-shell').then(m => m.AppShell),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/pages/dashboard/dashboard').then(m => m.Dashboard) },
      { path: 'settings/appearance', loadComponent: () => import('./features/settings/appearance/appearance').then(m => m.Appearance) }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
