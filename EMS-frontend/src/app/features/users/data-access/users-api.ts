
import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';

import {
  User,
  Role,
  CreateUserRequest,
  UpdateUserRequest,
  UpdateUserRolesRequest
} from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class UsersApi {
  private readonly api = inject(ApiClient);

  list() {
    return this.api.get<User[]>('users');
  }

  get(id: number) {
    return this.api.get<User>(`users/${id}`);
  }

  create(data: CreateUserRequest) {
    return this.api.post<User, CreateUserRequest>(
      'users',
      data
    );
  }

  update(id: number, data: UpdateUserRequest) {
    return this.api.patch<User, UpdateUserRequest>(
      `users/${id}`,
      data
    );
  }

  setStatus(id: number, isActive: boolean) {
    return this.api.patch<void, { isActive: boolean }>(
      `users/${id}/status`,
      { isActive }
    );
  }

  updateRoles(id: number, roleIds: number[]) {
    return this.api.put<
      void,
      UpdateUserRolesRequest
    >(
      `users/${id}/roles`,
      { roleIds }
    );
  }

  // New read-only backend endpoint described below.
  listRoles() {
    return this.api.get<Role[]>('roles');
  }
}
