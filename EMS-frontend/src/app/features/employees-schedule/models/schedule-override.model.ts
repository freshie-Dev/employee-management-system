export type OverrideType = 'WORK' | 'DAY_OFF';
export type OverrideStatus = 'ACTIVE' | 'CANCELLED';

export interface EmployeeScheduleOverride {
  overrideId: number;
  employeeId: number;
  workDate: string;
  overrideType: OverrideType;
  startAt: string | null;
  endAt: string | null;
  breakMinutes: number;
  graceInMinutes: number;
  graceOutMinutes: number;
  requiredMinutes: number | null;
  reason: string | null;
  assignedByUserId: number;
  recordStatus: OverrideStatus;
  createdAt: string;
  updatedAt: string;
  rowVersion: string; // Base64 SQL rowversion - required for edit / cancellation
}

export interface ScheduleOverridePayload {
  overrideType: OverrideType;
  startAt: string | null;
  endAt: string | null;
  breakMinutes: number;
  graceInMinutes: number;
  graceOutMinutes: number;
  requiredMinutes: number | null;
  reason: string | null;
}

export interface CreateScheduleOverrideRequest extends ScheduleOverridePayload {
  workDate: string;
}
export interface UpdateScheduleOverrideRequest extends ScheduleOverridePayload {
  rowVersion: string;
}
export interface ResolvedEmployeeSchedule {
  employeeId: number;
  workDate: string;
  scheduleSource: 'REGULAR' | 'OVERRIDE';
  overrideId: number | null;
  shiftId: number | null;
  overrideType: OverrideType | null;
  isScheduled: boolean;
  scheduledStartAt: string | null;
  scheduledEndAt: string | null;
  breakMinutes: number | null;
  graceInMinutes: number | null;
  graceOutMinutes: number | null;
  requiredMinutes: number | null;
}
