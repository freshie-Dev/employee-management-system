import { TestBed } from '@angular/core/testing';
import { provideHttpClient, withInterceptors, HttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthStore } from './auth-store';
import { Auth } from './auth';
import { authInterceptor } from './auth-interceptor';
import { errorInterceptor } from '../http/error-interceptor';
import { API_CONFIG } from '../config/api.config';
import { AuthSession } from '../models/auth.model';
import { vi } from 'vitest';

const session = (): AuthSession => ({ accessToken: 'test-token', tokenType: 'Bearer',
  expiresAt: new Date(Date.now() + 60_000).toISOString(), userId: 1, username: 'admin', roles: ['SYSTEM_ADMIN'] });

describe('Authentication', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let store: AuthStore;
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({ providers: [provideRouter([]),
      provideHttpClient(withInterceptors([errorInterceptor, authInterceptor])), provideHttpClientTesting()] });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    store = TestBed.inject(AuthStore);
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  });
  afterEach(() => { backend.verify(); store.clearSession(); vi.useRealTimers(); });

  it('logs in without a bearer token and stores the returned session', () => {
    TestBed.inject(Auth).login({ username: 'admin', password: 'secret' }).subscribe();
    const req = backend.expectOne('/api/auth/login');
    expect(req.request.headers.has('Authorization')).toBe(false);
    expect(req.request.body).toEqual({ username: 'admin', password: 'secret' });
    req.flush(session());
    expect(store.isAuthenticated()).toBe(true);
    expect(JSON.parse(sessionStorage.getItem('ems-session')!).username).toBe('admin');
  });

  it('attaches tokens only to the API origin and path', () => {
    store.setSession(session());
    for (const url of ['/api/users', '/api-evil/users', 'https://other.example/api/users']) {
      http.get(url).subscribe();
      const req = backend.expectOne(url);
      expect(req.request.headers.get('Authorization')).toBe(url === '/api/users' ? 'Bearer test-token' : null);
      req.flush([]);
    }
  });

  it('supports an absolute configured API URL', () => {
    TestBed.inject(API_CONFIG).baseUrl = 'https://backend.example/api/';
    store.setSession(session());
    http.get('https://backend.example/api/users').subscribe();
    const req = backend.expectOne('https://backend.example/api/users');
    expect(req.request.headers.get('Authorization')).toBe('Bearer test-token'); req.flush([]);
  });

  it('clears the session on protected 401 responses and preserves normalized errors', () => {
    store.setSession(session());
    let failure: any;
    http.get('/api/users').subscribe({ error: e => failure = e });
    backend.expectOne('/api/users').flush({ message: 'Expired' }, { status: 401, statusText: 'Unauthorized' });
    expect(store.isAuthenticated()).toBe(false);
    expect(failure).toEqual({ status: 401, message: 'Expired', validationErrors: undefined });
    expect(TestBed.inject(Router).navigate).toHaveBeenCalled();
  });

  it('does not redirect on invalid login credentials', () => {
    TestBed.inject(Auth).login({ username: 'wrong', password: 'wrong' }).subscribe({ error: () => {} });
    backend.expectOne('/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(TestBed.inject(Router).navigate).not.toHaveBeenCalled();
  });

  it('does not sign out on a forbidden response', () => {
    store.setSession(session());
    http.get('/api/users').subscribe({ error: () => {} });
    backend.expectOne('/api/users').flush(null, { status: 403, statusText: 'Forbidden' });
    expect(store.isAuthenticated()).toBe(true);
  });

  it('ignores a late 401 belonging to an older token', () => {
    store.setSession(session());
    http.get('/api/users').subscribe({ error: () => {} });
    store.setSession({ ...session(), accessToken: 'new-token' });
    backend.expectOne('/api/users').flush(null, { status: 401, statusText: 'Unauthorized' });
    expect(store.accessToken()).toBe('new-token');
  });

  it('expires the session automatically', () => {
    vi.useFakeTimers(); store.setSession(session()); vi.advanceTimersByTime(60_001);
    expect(store.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem('ems-session')).toBeNull();
  });

  it('logs out locally without calling a nonexistent endpoint', () => {
    store.setSession(session()); TestBed.inject(Auth).logout();
    expect(store.accessToken()).toBeNull();
    expect(sessionStorage.getItem('ems-session')).toBeNull();
    backend.expectNone('/api/auth/logout');
  });
});

describe('Session restoration', () => {
  afterEach(() => { TestBed.inject(AuthStore).clearSession(); });
  for (const kind of ['valid', 'expired', 'corrupt']) {
    it(`handles ${kind} saved sessions`, () => {
      const value = session();
      if (kind === 'expired') value.expiresAt = new Date(Date.now() - 1000).toISOString();
      sessionStorage.setItem('ems-session', kind === 'corrupt' ? '{invalid' : JSON.stringify(value));
      TestBed.configureTestingModule({ providers: [provideRouter([])] });
      expect(TestBed.inject(AuthStore).hasValidSession()).toBe(kind === 'valid');
    });
  }
});
