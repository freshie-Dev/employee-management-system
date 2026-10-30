import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import {
  Department,
  DepartmentRequest
} from '../models/department.model';

@Injectable({ providedIn: 'root' })
export class DepartmentsApi {
  private readonly api = inject(ApiClient);

  list(search?: string) {
    return this.api.get<Department[]>(
      'departments',
      search ? { search } : undefined
    );
  }

  get(id: number) {
    return this.api.get<Department>(`departments/${id}`);
  }

  create(data: DepartmentRequest) {
    return this.api.post<Department, DepartmentRequest>(
      'departments',
      data
    );
  }

  update(id: number, data: DepartmentRequest) {
    return this.api.put<Department, DepartmentRequest>(
      `departments/${id}`,
      data
    );
  }

  setStatus(id: number, isActive: boolean) {
    return this.api.patch<void, { isActive: boolean }>(
      `departments/${id}/status`,
      { isActive }
    );
  }
}