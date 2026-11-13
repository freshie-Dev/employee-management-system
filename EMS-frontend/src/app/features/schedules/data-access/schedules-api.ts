import { inject, Injectable } from '@angular/core';
import { ApiClient } from '../../../core/api/api-client';
import { Shift } from '../../shifts/models/shift.model';
import {
  CreateWorkScheduleRequest,
  ScheduleSettings,
  UpdateScheduleSettingsRequest,
  WorkSchedule
} from '../models/schedule.model';

@Injectable({ providedIn: 'root' })
export class SchedulesApi {
  private readonly api = inject(ApiClient);

  getSettings() {
    return this.api.get<ScheduleSettings>('schedule-settings');
  }

  updateSettings(data: UpdateScheduleSettingsRequest) {
    return this.api.put<ScheduleSettings, UpdateScheduleSettingsRequest>(
      'schedule-settings', data);
  }

  listSchedules(includeInactive = true) {
    return this.api.get<WorkSchedule[]>('work-schedules', { includeInactive });
  }

  createSchedule(data: CreateWorkScheduleRequest) {
    return this.api.post<WorkSchedule, CreateWorkScheduleRequest>(
      'work-schedules', data);
  }

  listShifts() {
    return this.api.get<Shift[]>('shifts');
  }
}
