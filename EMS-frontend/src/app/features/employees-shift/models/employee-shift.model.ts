export interface EmployeeShiftAssignment {
  assignmentId: number;
  employeeId: number;
  shiftId: number;
  shiftName: string;
  startTime: string;
  endTime: string;
  breakMinutes: number;
  effectiveFrom: string; // YYYY-MM-DD (inclusive)
  effectiveTo: string | null; // YYYY-MM-DD (inclusive)
}

export interface AssignEmployeeShiftRequest {
  shiftId: number;
  effectiveFrom: string;
}
