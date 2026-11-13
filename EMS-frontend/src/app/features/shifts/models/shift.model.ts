export interface Shift {
  shiftId: number;
  name: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  isActive: boolean;
  graceInMinutes: number;
  graceOutMinutes: number;
  configuredRequiredMinutes: number | null;
  effectiveRequiredMinutes: number;
  isOvernight: boolean;
}

export interface CreateShiftRequest {
  name: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  graceInMinutes: number;
  graceOutMinutes: number;
  requiredMinutes: number | null;
}

export type UpdateShiftRequest = Pick<CreateShiftRequest,
  'name' | 'startTime' | 'endTime' | 'breakMinutes'>;

export interface ShiftPolicyRequest {
  graceInMinutes: number;
  graceOutMinutes: number;
  requiredMinutes: number | null;
}
