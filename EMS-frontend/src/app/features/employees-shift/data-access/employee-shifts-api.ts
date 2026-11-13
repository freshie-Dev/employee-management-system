import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { AssignEmployeeShiftRequest, EmployeeShiftAssignment } from '../models/employee-shift.model';

@Injectable({ providedIn: 'root' })
export class EmployeeShiftsApi {
  private readonly api = inject(ApiClient);
  list(employeeId: number) {
    return this.api.get<EmployeeShiftAssignment[]>(`employees/${employeeId}/shifts`);
  }
  getEffective(employeeId: number, workDate: string) {
    return this.api.get<EmployeeShiftAssignment>(
      `employees/${employeeId}/shifts/effective`, { workDate }
    );
  }
  assign(employeeId: number, body: AssignEmployeeShiftRequest) {
    return this.api.post<EmployeeShiftAssignment, AssignEmployeeShiftRequest>(
      `employees/${employeeId}/shifts`, body
    );
  }
  initializeDefault(employeeId: number) {
    return this.api.post<EmployeeShiftAssignment[], Record<string, never>>(
      `employees/${employeeId}/shifts/initialize-default`, {}
    );
  }
}
