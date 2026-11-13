import { Component, DestroyRef, computed, inject, input, OnInit, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ButtonDirective } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { TextareaModule } from 'primeng/textarea';
import { ApiError } from '../../../../core/models/api.models';
import { ScheduleOverridesApi } from '../../data-access/schedule-overrides-api';
import {
  EmployeeScheduleOverride, OverrideType, ResolvedEmployeeSchedule,
  ScheduleOverridePayload
} from '../../models/schedule-override.model';

// The SQL resolver currently constructs regular shifts with a +05:00 offset.
// Adjust only if the backend changes to an organization-specific timezone.
const ORGANIZATION_OFFSET = '+05:00';
function toYmd(date: Date): string {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`;
}
function fromYmd(value: string): Date {
  const [year, month, day] = value.slice(0, 10).split('-').map(Number);
  return new Date(year, month - 1, day);
}
function dateFromApi(value: string | null): string {
  return value ? value.slice(11, 16) : '';
}
function plusDays(ymd: string, days: number): string {
  const date = fromYmd(ymd);
  date.setDate(date.getDate() + days);
  return toYmd(date);
}

@Component({
  selector: 'app-employee-schedule-overrides',
  standalone: true,
  imports: [
    FormsModule, ReactiveFormsModule, ButtonDirective, DatePickerModule, DialogModule,
    InputNumberModule, InputTextModule, MessageModule, SelectModule, TagModule, TextareaModule
  ],
  templateUrl: './employee-schedule-overrides.html',
  styleUrl: './employee-schedule-overrides.scss'
})
export class EmployeeScheduleOverrides implements OnInit {
  readonly employeeId = input.required<number>();
  private readonly api = inject(ScheduleOverridesApi);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly formError = signal('');
  readonly overrides = signal<EmployeeScheduleOverride[]>([]);
  readonly includeCancelled = signal(false);
  readonly dialogVisible = signal(false);
  readonly editing = signal<EmployeeScheduleOverride | null>(null);
  readonly cancelTarget = signal<EmployeeScheduleOverride | null>(null);
  readonly confirmationVisible = signal(false);

  readonly previewDate = signal<Date>(new Date());
  readonly resolved = signal<ResolvedEmployeeSchedule | null>(null);
  readonly resolving = signal(false);
  readonly resolveError = signal('');
  readonly types = [
    { label: 'Custom working hours', value: 'WORK' as OverrideType },
    { label: 'Approved day off', value: 'DAY_OFF' as OverrideType }
  ];
  readonly activeCount = computed(() => this.overrides().filter(o => o.recordStatus === 'ACTIVE').length);
  readonly visibleRows = computed(() => this.overrides()
    .filter(o => this.includeCancelled() || o.recordStatus !== 'CANCELLED')
    .sort((a, b) => b.workDate.localeCompare(a.workDate)));

  readonly form = new FormGroup({
    workDate: new FormControl<Date | null>(new Date(), { validators: [Validators.required] }),
    overrideType: new FormControl<OverrideType>('WORK', { nonNullable: true, validators: [Validators.required] }),
    startTime: new FormControl('09:00', { nonNullable: true }),
    endTime: new FormControl('17:00', { nonNullable: true }),
    endNextDay: new FormControl(false, { nonNullable: true }),
    breakMinutes: new FormControl(0, { nonNullable: true, validators: [Validators.min(0), Validators.max(1440)] }),
    graceInMinutes: new FormControl(0, { nonNullable: true, validators: [Validators.min(0), Validators.max(240)] }),
    graceOutMinutes: new FormControl(0, { nonNullable: true, validators: [Validators.min(0), Validators.max(240)] }),
    requiredMinutes: new FormControl<number | null>(null, { validators: [Validators.min(0), Validators.max(1440)] }),
    reason: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(500)] })
  });

  get isDayOff(): boolean { return this.form.controls.overrideType.value === 'DAY_OFF'; }
  ngOnInit(): void { this.refresh(); this.resolve(); }

  refresh(): void {
    this.loading.set(true);
    this.error.set('');
    this.api.list(this.employeeId(), true)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.loading.set(false)))
      .subscribe({
        next: rows => this.overrides.set(rows),
        error: (err: ApiError) => this.error.set(err.message || 'Unable to load schedule overrides.')
      });
  }

  resolve(): void {
    const date = this.previewDate();
    if (!date) return;
    this.resolving.set(true);
    this.resolveError.set('');
    this.resolved.set(null);
    this.api.resolve(this.employeeId(), toYmd(date))
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.resolving.set(false)))
      .subscribe({
        next: value => this.resolved.set(value),
        error: (err: ApiError) => this.resolveError.set(err.message || 'Could not resolve this work date.')
      });
  }

  openCreate(): void {
    this.editing.set(null);
    this.formError.set('');
    this.form.reset({
      workDate: new Date(), overrideType: 'WORK', startTime: '09:00', endTime: '17:00',
      endNextDay: false, breakMinutes: 0, graceInMinutes: 0,
      graceOutMinutes: 0, requiredMinutes: null, reason: ''
    });
    this.form.controls.workDate.enable({ emitEvent: false });
    this.dialogVisible.set(true);
  }

  openEdit(row: EmployeeScheduleOverride): void {
    if (row.recordStatus !== 'ACTIVE') return;
    this.editing.set(row);
    this.formError.set('');
    this.form.reset({
      workDate: fromYmd(row.workDate),
      overrideType: row.overrideType,
      startTime: dateFromApi(row.startAt), endTime: dateFromApi(row.endAt),
      endNextDay: !!row.endAt && row.endAt.slice(0, 10) !== row.workDate,
      breakMinutes: row.breakMinutes, graceInMinutes: row.graceInMinutes,
      graceOutMinutes: row.graceOutMinutes, requiredMinutes: row.requiredMinutes,
      reason: row.reason || ''
    });
    this.form.controls.workDate.disable({ emitEvent: false }); // API does not support changing WorkDate
    this.dialogVisible.set(true);
  }

  save(): void {
    if (this.saving()) return;
    const date = this.form.controls.workDate.value;
    if (this.form.invalid || !date) {
      this.form.markAllAsTouched();
      this.formError.set('Please complete the required fields with valid values.');
      return;
    }
    const day = toYmd(date);
    const type = this.form.controls.overrideType.value;
    const value = this.form.getRawValue();
    let payload: ScheduleOverridePayload;

    if (type === 'DAY_OFF') {
      payload = {
        overrideType: 'DAY_OFF', startAt: null, endAt: null, breakMinutes: 0,
        graceInMinutes: 0, graceOutMinutes: 0, requiredMinutes: null,
        reason: value.reason?.trim() || null
      };
    } else {
      const start = value.startTime;
      const end = value.endTime;
      const timePattern = /^([01]\d|2[0-3]):[0-5]\d$/;
      if (!timePattern.test(start) || !timePattern.test(end)) {
        this.formError.set('Enter valid start/end times in HH:mm format.'); return;
      }
      const endDate = value.endNextDay ? plusDays(day, 1) : day;
      const startAt = `${day}T${start}:00${ORGANIZATION_OFFSET}`;
      const endAt = `${endDate}T${end}:00${ORGANIZATION_OFFSET}`;
      const duration = (Date.parse(endAt) - Date.parse(startAt)) / 60000;
      const breakMinutes = value.breakMinutes;
      if (!(duration >= 1 && duration <= 1440) || breakMinutes >= duration ||
          (value.requiredMinutes !== null && value.requiredMinutes > duration - breakMinutes)) {
        this.formError.set('Check the shift duration, next-day setting, break and required minutes. Maximum duration: 24 hours.');
        return;
      }
      payload = {
        overrideType: 'WORK', startAt, endAt,
        breakMinutes, graceInMinutes: value.graceInMinutes,
        graceOutMinutes: value.graceOutMinutes,
        requiredMinutes: value.requiredMinutes,
        reason: value.reason?.trim() || null
      };
    }

    this.saving.set(true);
    this.formError.set('');
    const current = this.editing();
    const request = current
      ? this.api.update(this.employeeId(), current.overrideId, { ...payload, rowVersion: current.rowVersion })
      : this.api.create(this.employeeId(), { ...payload, workDate: day });
    request.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.dialogVisible.set(false);
          this.notice.set(current ? 'Override updated.' : 'Override created.');
          this.refresh();
          this.resolve();
        },
        error: (err: ApiError) => this.formError.set(err.status === 409
          ? 'This override has changed or conflicts with another schedule. Refresh before retrying.'
          : (err.message || 'Could not save the override.'))
      });
  }

  requestCancel(row: EmployeeScheduleOverride): void {
    if (row.recordStatus !== 'ACTIVE') return;
    this.cancelTarget.set(row);
    this.confirmationVisible.set(true);
  }

  confirmCancel(): void {
    const target = this.cancelTarget();
    if (!target || this.saving()) return;
    this.saving.set(true);
    this.error.set('');
    this.api.cancel(this.employeeId(), target.overrideId, target.rowVersion)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.confirmationVisible.set(false);
          this.cancelTarget.set(null);
          this.notice.set('Override cancelled.');
          this.refresh(); this.resolve();
        },
        error: (err: ApiError) => {
          this.confirmationVisible.set(false);
          this.error.set(err.status === 409 ? 'Override was changed by another user. Refresh and try again.' : err.message);
          this.refresh();
        }
      });
  }

  displayTime(value: string | null): string { return value ? value.slice(11, 16) : '—'; }
  displayDate(value: string): string { return fromYmd(value).toLocaleDateString('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }); }
}
