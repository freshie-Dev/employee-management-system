import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, forkJoin } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { ApiError } from '../../../../core/models/api.models';
import { Shift } from '../../../shifts/models/shift.model';
import { SchedulesApi } from '../../data-access/schedules-api';
import { ScheduleSettings, WEEKDAYS, WorkSchedule } from '../../models/schedule.model';

@Component({
  selector: 'app-schedule-settings-page',
  imports: [
    ReactiveFormsModule, ButtonModule, DialogModule, InputTextModule,
    InputNumberModule, SelectModule, MessageModule, TagModule
  ],
  templateUrl: './schedule-settings.html',
  styleUrl: './schedule-settings.scss'
})
export class ScheduleSettingsPage {
  private readonly api = inject(SchedulesApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);

  readonly weekdays = WEEKDAYS;
  readonly schedules = signal<WorkSchedule[]>([]);
  readonly shifts = signal<Shift[]>([]);
  readonly settings = signal<ScheduleSettings | null>(null);
  readonly loading = signal(false);
  readonly savingDefaults = signal(false);
  readonly savingSchedule = signal(false);
  readonly createOpen = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly formError = signal('');
  readonly loadFailed = signal(false);
  private loadVersion = 0;

  readonly scheduleOptions = computed(() => this.schedules()
    .filter(x => x.isActive)
    .map(x => ({ label: x.name, value: x.scheduleId })));
  readonly shiftOptions = computed(() => this.shifts()
    .filter(x => x.isActive)
    .map(x => ({ label: `${x.name} (${this.clock(x.startTime)} – ${this.clock(x.endTime)})`, value: x.shiftId })));

  readonly defaultsForm = this.fb.group({
    defaultShiftId: this.fb.control<number | null>(null, Validators.required),
    defaultScheduleId: this.fb.control<number | null>(null, Validators.required),
    defaultHolidayCalendarId: this.fb.control<number | null>(null, Validators.min(1))
  });

  readonly createForm = this.fb.group({
    name: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
    workingDays: this.fb.nonNullable.control<number[]>([1, 2, 3, 4, 5])
  });

  constructor() { this.load(); }

  load(): void {
    const version = ++this.loadVersion;
    this.loading.set(true);
    this.loadFailed.set(false);
    this.error.set('');
    forkJoin({
      settings: this.api.getSettings(),
      schedules: this.api.listSchedules(true),
      shifts: this.api.listShifts()
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => { if (version === this.loadVersion) this.loading.set(false); })
    ).subscribe({
      next: ({ settings, schedules, shifts }) => {
        if (version !== this.loadVersion) return;
        this.settings.set(settings);
        this.schedules.set(schedules);
        this.shifts.set(shifts);
        this.defaultsForm.reset({
          defaultShiftId: settings.defaultShiftId,
          defaultScheduleId: settings.defaultScheduleId,
          defaultHolidayCalendarId: settings.defaultHolidayCalendarId
        });
        this.defaultsForm.markAsPristine();
      },
      error: err => {
        if (version !== this.loadVersion) return;
        this.loadFailed.set(true);
        this.error.set(this.errorText(err));
      }
    });
  }

  saveDefaults(): void {
    if (this.savingDefaults() || this.loading()) return;
    if (this.defaultsForm.invalid) {
      this.defaultsForm.markAllAsTouched();
      return;
    }
    const { defaultShiftId, defaultScheduleId, defaultHolidayCalendarId } =
      this.defaultsForm.getRawValue();
    if (defaultShiftId === null || defaultScheduleId === null) return;
    this.savingDefaults.set(true);
    this.error.set('');
    this.notice.set('');
    this.api.updateSettings({ defaultShiftId, defaultScheduleId, defaultHolidayCalendarId })
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.savingDefaults.set(false)))
      .subscribe({
        next: settings => {
          this.settings.set(settings);
          this.defaultsForm.markAsPristine();
          this.notice.set('Organization schedule defaults saved.');
        },
        error: err => this.error.set(this.errorText(err))
      });
  }

  openCreate(): void {
    this.createForm.reset({ name: '', workingDays: [1, 2, 3, 4, 5] });
    this.formError.set('');
    this.createOpen.set(true);
  }

  toggleDay(day: number): void {
    const control = this.createForm.controls.workingDays;
    const current = control.value;
    control.setValue(current.includes(day)
      ? current.filter(x => x !== day)
      : [...current, day].sort((a, b) => a - b));
    control.markAsDirty();
  }

  createSchedule(): void {
    if (this.savingSchedule()) return;
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }
    const { name, workingDays } = this.createForm.getRawValue();
    if (!name.trim()) {
      this.formError.set('Enter a schedule name.');
      return;
    }
    this.savingSchedule.set(true);
    this.formError.set('');
    this.api.createSchedule({ name: name.trim(), workingDays })
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.savingSchedule.set(false)))
      .subscribe({
        next: () => {
          this.createOpen.set(false);
          this.notice.set('Work schedule created successfully.');
          this.load();
        },
        error: err => this.formError.set(this.errorText(err))
      });
  }

  worksOn(schedule: WorkSchedule, day: number): boolean {
    return schedule.days.some(x => x.dayOfWeek === day && x.isWorkingDay);
  }

  workingCount(schedule: WorkSchedule): number {
    return schedule.days.filter(x => x.isWorkingDay).length;
  }

  clock(time: string): string { return time.slice(0, 5); }

  private errorText(err: ApiError): string {
    const issues = err?.validationErrors && Object.values(err.validationErrors).flat();
    return issues?.length ? issues.join(' ') : err?.message || 'Request failed. Check the API connection.';
  }
}
