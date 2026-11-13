export interface WorkScheduleDay {
  dayOfWeek: number; // 1 = Monday, 7 = Sunday
  isWorkingDay: boolean;
}

export interface WorkSchedule {
  scheduleId: number;
  name: string;
  isActive: boolean;
  createdAt: string;
  days: WorkScheduleDay[];
}

export interface ScheduleSettings {
  settingsId: number;
  defaultShiftId: number | null;
  defaultShiftName: string | null;
  defaultScheduleId: number | null;
  defaultScheduleName: string | null;
  defaultHolidayCalendarId: number | null;
  updatedAt: string;
}

export interface CreateWorkScheduleRequest {
  name: string;
  workingDays: number[];
}

export interface UpdateScheduleSettingsRequest {
  defaultShiftId: number;
  defaultScheduleId: number;
  defaultHolidayCalendarId: number | null;
}

export const WEEKDAYS = [
  { value: 1, label: 'Monday', short: 'Mon' },
  { value: 2, label: 'Tuesday', short: 'Tue' },
  { value: 3, label: 'Wednesday', short: 'Wed' },
  { value: 4, label: 'Thursday', short: 'Thu' },
  { value: 5, label: 'Friday', short: 'Fri' },
  { value: 6, label: 'Saturday', short: 'Sat' },
  { value: 7, label: 'Sunday', short: 'Sun' }
] as const;
