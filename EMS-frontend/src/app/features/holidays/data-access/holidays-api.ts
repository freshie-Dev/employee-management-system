import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import {
  Holiday,
  HolidayCalendar,
  HolidayDateCheck,
  SaveHolidayCalendarRequest,
  SaveHolidayRequest
} from '../models/holiday.model';

@Injectable({ providedIn: 'root' })
export class HolidaysApi {
  private readonly api = inject(ApiClient);
  private readonly root = 'holiday-calendars';

  listCalendars(includeInactive = true) {
    return this.api.get<HolidayCalendar[]>(this.root, { includeInactive });
  }

  getCalendar(calendarId: number) {
    return this.api.get<HolidayCalendar>(`${this.root}/${calendarId}`);
  }

  createCalendar(body: SaveHolidayCalendarRequest) {
    return this.api.post<HolidayCalendar, SaveHolidayCalendarRequest>(this.root, body);
  }

  updateCalendar(calendarId: number, body: SaveHolidayCalendarRequest) {
    return this.api.put<HolidayCalendar, SaveHolidayCalendarRequest>(
      `${this.root}/${calendarId}`, body);
  }

  setCalendarStatus(calendarId: number, isActive: boolean) {
    return this.api.patch<HolidayCalendar, { isActive: boolean }>(
      `${this.root}/${calendarId}/status`, { isActive });
  }

  listHolidays(calendarId: number, fromDate?: string, toDate?: string) {
    return this.api.get<Holiday[]>(`${this.root}/${calendarId}/holidays`, {
      fromDate, toDate
    });
  }

  getHoliday(calendarId: number, holidayId: number) {
    return this.api.get<Holiday>(`${this.root}/${calendarId}/holidays/${holidayId}`);
  }

  createHoliday(calendarId: number, body: SaveHolidayRequest) {
    return this.api.post<Holiday, SaveHolidayRequest>(
      `${this.root}/${calendarId}/holidays`, body);
  }

  updateHoliday(calendarId: number, holidayId: number, body: SaveHolidayRequest) {
    return this.api.put<Holiday, SaveHolidayRequest>(
      `${this.root}/${calendarId}/holidays/${holidayId}`, body);
  }

  deleteHoliday(calendarId: number, holidayId: number) {
    return this.api.delete<void>(`${this.root}/${calendarId}/holidays/${holidayId}`);
  }

  checkDate(calendarId: number, workDate: string) {
    return this.api.get<HolidayDateCheck>(`${this.root}/${calendarId}/check-date`, {
      workDate
    });
  }
}
