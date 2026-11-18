
import { Component, computed, inject } from '@angular/core';
import {
  RouterLink,
  RouterLinkActive,
  RouterOutlet
} from '@angular/router';

import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';

import { SignOut } from '@primeicons/angular/sign-out';
import { Palette } from '@primeicons/angular/palette';

import { Auth } from '../../core/auth/auth';
import { AuthStore } from '../../core/auth/auth-store';

// ==========================================
// Navigation types
// ==========================================

type NavRole = 'SYSTEM_ADMIN' | 'HR_MANAGER';

interface NavItem {
  label: string;
  route: string;
  roles?: NavRole[];
}

// ==========================================
// Navigation configuration
// ==========================================

const ADMIN_HR_ROLES: NavRole[] = [
  'SYSTEM_ADMIN',
  'HR_MANAGER'
];

const NAV_ITEMS: NavItem[] = [
  {
    label: 'Workspace',
    route: '/dashboard'
  },
  {
    label: 'Departments',
    route: '/departments',
    roles: ADMIN_HR_ROLES
  },
  {
    label: 'Designations',
    route: '/designations',
    roles: ADMIN_HR_ROLES
  },
  {
    label: 'Shifts',
    route: '/shifts',
    roles: ADMIN_HR_ROLES
  },
  {
    label: 'Employees',
    route: '/employees',
    roles: ADMIN_HR_ROLES
  },
  {
    label: 'Users',
    route: '/users',
    roles: ADMIN_HR_ROLES
  },
  {
    label: 'Holidays',
    route: '/holiday-calendars',
    roles: ADMIN_HR_ROLES
  },
  {
    label: 'Work Schedules',
    route: '/work-schedules',
    roles: ADMIN_HR_ROLES
  }
];

// ==========================================
// Shell component
// ==========================================

@Component({
  selector: 'app-shell',
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    ButtonModule,
    SignOut,
    Palette,
    TooltipModule
  ],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss'
})
export class AppShell {
  readonly auth = inject(Auth);
  readonly store = inject(AuthStore);

  // ========================================
  // Navigation
  // ========================================

  readonly navItems = NAV_ITEMS;

  readonly visibleNavItems = computed(() => {
    // Subscribe to session changes.
    this.store.session();

    return this.navItems.filter(item => {
      // No roles specified = visible to all authenticated users.
      if (!item.roles?.length) {
        return true;
      }

      // Visible if the user has at least one allowed role.
      return item.roles.some(role =>
        this.store.hasRole(role)
      );
    });
  });
}
