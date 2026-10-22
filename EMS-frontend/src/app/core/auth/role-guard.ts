import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth-store';

// Use after authGuard with route data: { roles: ['SYSTEM_ADMIN'] }.
// Backend authorization remains authoritative.
export const roleGuard: CanActivateFn = (route) => {
  const allowed = route.data['roles'] as string[] | undefined;
  return !allowed?.length || allowed.some(role => inject(AuthStore).hasRole(role))
    ? true : inject(Router).createUrlTree(['/dashboard']);
};
