import { Injectable, signal, computed } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AuthStore {

  private readonly _accessToken = signal<string | null>(null);
  private readonly _roles = signal<string[]>([]);

  readonly accessToken = this._accessToken.asReadonly();
  readonly roles = this._roles.asReadonly();

  readonly isAuthenticated = computed(
    () => !!this._accessToken()
  );

  setSession(token: string, roles: string[]): void {
    this._accessToken.set(token);
    this._roles.set(roles);
  }

  clearSession(): void {
    this._accessToken.set(null);
    this._roles.set([]);
  }

  hasRole(role: string): boolean {
    return this._roles().includes(role);
  }
}