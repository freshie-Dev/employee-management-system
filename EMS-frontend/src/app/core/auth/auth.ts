import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpContext } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap, timeout } from 'rxjs';
import { ApiClient } from '../api/api-client';
import { AuthSession, LoginRequest } from '../models/auth.model';
import { AuthStore } from './auth-store';
import { SKIP_AUTH } from './auth-interceptor';

@Injectable({ providedIn: 'root' })
export class Auth {
  private readonly http = inject(HttpClient);
  private readonly api = inject(ApiClient);
  private readonly store = inject(AuthStore);
  private readonly router = inject(Router);

  login(credentials: LoginRequest) {
    return this.http.post<AuthSession>(this.api.resourceUrl('auth/login'), credentials, {
      context: new HttpContext().set(SKIP_AUTH, true)
    }).pipe(timeout(20_000), tap(session => this.store.setSession(session)));
  }

  logout(): void {
    // The backend contract has no logout/revocation endpoint.
    this.store.clearSession();
    void this.router.navigate(['/login'], { replaceUrl: true });
  }
}
