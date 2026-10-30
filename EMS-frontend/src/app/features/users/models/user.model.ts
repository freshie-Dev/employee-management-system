
export interface User {
  userId: number;
  employeeId: number | null;
  username: string;
  isActive: boolean;
  createdAt: string;
  roles: string[];
}

export interface Role {
  roleId: number;
  name: string;
  description: string | null;
}

export interface CreateUserRequest {
  username: string;
  password: string;
  roleIds: number[];
}

export interface UpdateUserRequest {
  username: string;
}

export interface UpdateUserStatusRequest {
  isActive: boolean;
}

export interface UpdateUserRolesRequest {
  roleIds: number[];
}

export type UserFormMode = 'create' | 'edit' | 'roles';
