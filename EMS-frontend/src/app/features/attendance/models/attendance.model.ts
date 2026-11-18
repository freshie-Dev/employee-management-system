export type PunchType = 'IN' | 'OUT';
export type CorrectionAction = 'INSERT' | 'UPDATE' | 'DELETE';

export interface AttendancePunch {
  punchId: number;
  attendanceId: number;
  employeeId: number;
  workDate: string;
  punchType: PunchType;
  punchAtUtc: string;
}

export interface AttendanceDay {
  attendanceId: number;
  employeeId: number;
  workDate: string;
  legacyStatus: string;
  assessmentStatus: string | null;
  isScheduled: boolean | null;
  isWeekend: boolean | null;
  isHoliday: boolean | null;
  isSpecialWorkDay: boolean | null;
  scheduledStartAt: string | null;
  scheduledEndAt: string | null;
  requiredMinutes: number | null;
  workedMinutes: number | null;
}

export interface AttendanceException {
  exceptionId: number;
  assessmentId: number;
  exceptionCode: string;
  exceptionName: string;
  minutes: number | null;
}

export interface AttendanceEvaluation {
  attendanceId: number;
  assessmentId: number;
  employeeId: number;
  workDate: string;
  attendanceStatus: string;
  legacyStatus: string;
  isFinalized: boolean;
  isScheduled: boolean;
  isWeekend: boolean;
  isHoliday: boolean;
  isSpecialWorkDay: boolean;
  scheduledStartAt: string | null;
  scheduledEndAt: string | null;
  requiredMinutes: number | null;
  workedMinutes: number | null;
  punchCount: number;
  firstInAtUtc: string | null;
  lastOutAtUtc: string | null;
  hasMissingCheckIn: boolean;
  hasMissingCheckOut: boolean;
  lateMinutes: number;
  earlyOutMinutes: number;
  evaluatedAtUtc: string;
}

export interface AttendanceHistoryItem {
  attendanceId: number;
  employeeId: number;
  workDate: string;
  attendanceStatus: string;
  legacyStatus: string;
  isFinalized: boolean;
  isScheduled: boolean | null;
  isWeekend: boolean | null;
  isHoliday: boolean | null;
  isSpecialWorkDay: boolean | null;
  scheduledStartAt: string | null;
  scheduledEndAt: string | null;
  requiredMinutes: number | null;
  workedMinutes: number | null;
  punchCount: number;
  firstInAtUtc: string | null;
  lastOutAtUtc: string | null;
  exceptionCount: number;
  totalCount: number;
}

export interface AttendanceHistoryResponse {
  items: AttendanceHistoryItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface AttendanceCorrectionRequest {
  action: CorrectionAction;
  punchId?: number | null;
  punchType?: PunchType | null;
  punchAtUtc?: string | null;
  expectedPunchType?: PunchType | null;
  expectedPunchAtUtc?: string | null;
  reason: string;
}

export interface AttendanceCorrectionResult {
  punchId: number;
  attendanceId: number;
  employeeId: number;
  workDate: string;
  correctionAction: CorrectionAction;
  punchType: PunchType | null;
  punchAtUtc: string | null;
  attendanceStatus: string;
  reason: string;
}
