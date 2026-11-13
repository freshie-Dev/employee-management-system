import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import {
  CreateScheduleOverrideRequest,
  EmployeeScheduleOverride,
  ResolvedEmployeeSchedule,
  UpdateScheduleOverrideRequest
} from '../models/schedule-override.model';

@Injectable({ providedIn: 'root' })
export class ScheduleOverridesApi {
  private readonly client = inject(ApiClient);
  private path(employeeId: number): string {
    return `employees/${employeeId}/schedule-overrides`;
  }
  list(employeeId: number, includeCancelled = false) {
    return this.client.get<EmployeeScheduleOverride[]>(this.path(employeeId), { includeCancelled });
  }
  get(employeeId: number, id: number) {
    return this.client.get<EmployeeScheduleOverride>(`${this.path(employeeId)}/${id}`);
  }
  create(employeeId: number, body: CreateScheduleOverrideRequest) {
    return this.client.post<EmployeeScheduleOverride, CreateScheduleOverrideRequest>(this.path(employeeId), body);
  }
  update(employeeId: number, id: number, body: UpdateScheduleOverrideRequest) {
    return this.client.put<EmployeeScheduleOverride, UpdateScheduleOverrideRequest>(`${this.path(employeeId)}/${id}`, body);
  }
  cancel(employeeId: number, id: number, rowVersion: string) {
    return this.client.patch<EmployeeScheduleOverride, { rowVersion: string }>(
      `${this.path(employeeId)}/${id}/cancel`, { rowVersion }
    );
  }
  resolve(employeeId: number, workDate: string) {
    return this.client.get<ResolvedEmployeeSchedule>(`employees/${employeeId}/schedule`, { workDate });
  }
}
