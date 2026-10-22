import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthStore } from './auth-store';

export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthStore);
  if (auth.hasValidSession()) return true;
  auth.clearSession();
  return inject(Router).createUrlTree(['/login'], { queryParams: { returnUrl: state.url } });
};

export const guestGuard: CanActivateFn = () => {
  return inject(AuthStore).hasValidSession()
    ? inject(Router).createUrlTree(['/dashboard']) : true;
};
