import { DOCUMENT } from '@angular/common';
import { inject } from '@angular/core';
import { HttpContextToken, HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';
import { API_CONFIG } from '../config/api.config';
import { AuthStore } from './auth-store';

export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const config = inject(API_CONFIG);
  const document = inject(DOCUMENT);
  const base = new URL(config.baseUrl, document.baseURI);
  const target = new URL(request.url, document.baseURI);
  const prefix = base.pathname.replace(/\/+$/, '');
  const backendRequest = target.origin === base.origin
    && (target.pathname === prefix || target.pathname.startsWith(prefix + '/'));
  if (!backendRequest || request.context.get(SKIP_AUTH)) return next(request);

  const auth = inject(AuthStore);
  if (auth.accessToken() && !auth.hasValidSession()) {
    auth.expireSession();
    return throwError(() => new HttpErrorResponse({ status: 401, statusText: 'Session expired', url: request.url }));
  }
  const token = auth.accessToken();
  const outgoing = token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
  return next(outgoing).pipe(catchError((error: unknown) => {
    // A late failure from an older session must not log out a newly signed-in user.
    if (error instanceof HttpErrorResponse && error.status === 401
      && token && token === auth.accessToken()) auth.expireSession();
    return throwError(() => error);
  }));
};
