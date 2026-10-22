import { isPlatformBrowser } from '@angular/common';
import { computed, DestroyRef, inject, Injectable, PLATFORM_ID, signal } from '@angular/core';
import { Router } from '@angular/router';
import { AuthSession } from '../models/auth.model';

@Injectable({ providedIn: 'root' })
export class AuthStore {
  private readonly browser = isPlatformBrowser(inject(PLATFORM_ID));
  private readonly router = inject(Router);
  private readonly key = 'ems-session';
  private timer?: ReturnType<typeof setTimeout>;
  private readonly state = signal<AuthSession | null>(this.restore());
  readonly session = this.state.asReadonly();
  readonly accessToken = computed(() => this.state()?.accessToken ?? null);
  readonly roles = computed(() => this.state()?.roles ?? []);
  readonly isAuthenticated = computed(() => this.state() !== null);

  constructor() {
    this.scheduleExpiry();
    inject(DestroyRef).onDestroy(() => clearTimeout(this.timer));
  }

  hasRole(role: string): boolean { return this.roles().includes(role); }

  // Check time again on requests/navigation: background tabs can throttle timers.
  hasValidSession(): boolean {
    const session = this.state();
    return !!session && Date.parse(session.expiresAt) > Date.now();
  }

  setSession(session: AuthSession): void {
    if (!this.valid(session)) throw new Error('Invalid or expired login response.');
    this.state.set({ ...session, roles: [...session.roles] });
    if (this.browser) {
      try { sessionStorage.setItem(this.key, JSON.stringify(session)); } catch { /* Memory-only fallback. */ }
    }
    this.scheduleExpiry();
  }

  clearSession(): void {
    clearTimeout(this.timer);
    this.state.set(null);
    if (this.browser) {
      try { sessionStorage.removeItem(this.key); } catch { /* Storage unavailable. */ }
    }
  }

  expireSession(): void {
    if (!this.state()) return;
    const returnUrl = this.router.url;
    this.clearSession();
    void this.router.navigate(['/login'], {
      queryParams: { reason: 'expired', returnUrl }, replaceUrl: true
    });
  }

  private scheduleExpiry(): void {
    clearTimeout(this.timer);
    const session = this.state();
    if (!session || !this.browser) return;
    const remaining = Date.parse(session.expiresAt) - Date.now();
    this.timer = setTimeout(() => {
      if (this.hasValidSession()) this.scheduleExpiry();
      else this.expireSession();
    }, Math.max(0, Math.min(remaining, 2_147_483_647)));
  }

  private valid(value: unknown): value is AuthSession {
    if (!value || typeof value !== 'object') return false;
    const s = value as AuthSession;
    return typeof s.accessToken === 'string' && !!s.accessToken.trim()
      && s.tokenType === 'Bearer' && typeof s.expiresAt === 'string'
      && Date.parse(s.expiresAt) > Date.now()
      && Number.isInteger(s.userId) && typeof s.username === 'string'
      && Array.isArray(s.roles) && s.roles.every(role => typeof role === 'string');
  }

  private restore(): AuthSession | null {
    if (!this.browser) return null;
    try {
      const value: unknown = JSON.parse(sessionStorage.getItem(this.key) ?? 'null');
      if (this.valid(value)) return value;
      sessionStorage.removeItem(this.key);
    } catch { /* Invalid or unavailable storage: start signed out. */ }
    return null;
  }
}
