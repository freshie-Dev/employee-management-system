import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, provideRouter, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { authGuard, guestGuard } from './auth-guard';
import { roleGuard } from './role-guard';
import { AuthStore } from './auth-store';

describe('Route protection', () => {
  beforeEach(() => { sessionStorage.clear(); TestBed.configureTestingModule({ providers: [provideRouter([])] }); });
  afterEach(() => TestBed.inject(AuthStore).clearSession());
  const route = () => new ActivatedRouteSnapshot();
  const state = { url: '/settings/appearance' } as RouterStateSnapshot;
  function signIn() {
    TestBed.inject(AuthStore).setSession({ accessToken: 'token', tokenType: 'Bearer',
      expiresAt: new Date(Date.now() + 60_000).toISOString(), userId: 1, username: 'user', roles: ['EMPLOYEE'] });
  }
  it('redirects guests to login and preserves the destination', () => {
    const result = TestBed.runInInjectionContext(() => authGuard(route(), state)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(result)).toContain('/login?returnUrl=%2Fsettings%2Fappearance');
  });
  it('allows authenticated users', () => {
    signIn(); expect(TestBed.runInInjectionContext(() => authGuard(route(), state))).toBe(true);
  });
  it('redirects signed-in users away from login', () => {
    signIn(); const result = TestBed.runInInjectionContext(() => guestGuard(route(), state)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/dashboard');
  });
  it('rejects a role mismatch', () => {
    signIn(); const target = route(); target.data = { roles: ['SYSTEM_ADMIN'] };
    const result = TestBed.runInInjectionContext(() => roleGuard(target, state)) as UrlTree;
    expect(TestBed.inject(Router).serializeUrl(result)).toBe('/dashboard');
  });
});
