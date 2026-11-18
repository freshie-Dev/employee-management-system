import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import {
  AttendanceCorrectionRequest, AttendanceCorrectionResult,
  AttendanceDay, AttendanceEvaluation, AttendanceException,
  AttendanceHistoryResponse, AttendancePunch, PunchType
} from '../models/attendance.model';

@Injectable({ providedIn: 'root' })
export class AttendanceApi {
  private readonly api = inject(ApiClient);

  private base(employeeId: number): string {
    return `employees/${employeeId}/attendance`;
  }

  day(employeeId: number, workDate: string) {
    return this.api.get<AttendanceDay>(`${this.base(employeeId)}/days/${workDate}`);
  }

  punches(employeeId: number, workDate: string) {
    return this.api.get<AttendancePunch[]>(`${this.base(employeeId)}/days/${workDate}/punches`);
  }

  exceptions(employeeId: number, workDate: string) {
    return this.api.get<AttendanceException[]>(`${this.base(employeeId)}/days/${workDate}/exceptions`);
  }

  recordPunch(employeeId: number, workDate: string, punchType: PunchType) {
    return this.api.post<AttendancePunch, { workDate: string; punchType: PunchType }>(
      `${this.base(employeeId)}/punches`, { workDate, punchType });
  }

  evaluate(employeeId: number, workDate: string, finalize: boolean) {
    return this.api.post<AttendanceEvaluation, { finalize: boolean }>(
      `${this.base(employeeId)}/days/${workDate}/evaluate`, { finalize });
  }

  history(employeeId: number, fromDate: string, toDate: string, page = 1, pageSize = 10) {
    return this.api.get<AttendanceHistoryResponse>(`${this.base(employeeId)}/history`, {
      fromDate, toDate, page, pageSize
    });
  }

  correct(employeeId: number, workDate: string, request: AttendanceCorrectionRequest) {
    return this.api.post<AttendanceCorrectionResult, AttendanceCorrectionRequest>(
      `${this.base(employeeId)}/days/${workDate}/punch-corrections`, request);
  }
}
