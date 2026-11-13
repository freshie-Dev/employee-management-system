/** JSON contract: EMS.DTOs.Holidays (DateOnly is yyyy-MM-dd). */
export interface HolidayCalendar {
  calendarId: number;
  name: string;
  isActive: boolean;
  holidayCount: number;
  isDefault: boolean;
}

export interface Holiday {
  holidayId: number;
  calendarId: number;
  holidayDate: string; // yyyy-MM-dd, date only
  name: string;
}

export interface HolidayDateCheck {
  calendarId: number;
  workDate: string;
  isHoliday: boolean;
  holidayId: number | null;
  holidayName: string | null;
}

export interface SaveHolidayCalendarRequest {
  name: string;
}

export interface SaveHolidayRequest {
  name: string;
  holidayDate: string;
}
