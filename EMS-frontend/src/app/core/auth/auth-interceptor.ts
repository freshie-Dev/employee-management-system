import { inject } from '@angular/core';
import { HttpInterceptorFn } from '@angular/common/http';

import { AuthStore } from './auth-store';

export const authInterceptor: HttpInterceptorFn =
  (request, next) => {

    const auth = inject(AuthStore);
    const token = auth.accessToken();

    // Only attach credentials to our backend.
    if (!request.url.startsWith('/api/') || !token) {
      return next(request);
    }

    const authenticatedRequest = request.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });

    return next(authenticatedRequest);
  };