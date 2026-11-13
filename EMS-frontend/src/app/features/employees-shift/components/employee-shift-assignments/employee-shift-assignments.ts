import { Component, DestroyRef, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, finalize, take } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { EmployeeShiftsApi } from '../../data-access/employee-shifts-api';
import { EmployeeShiftAssignment } from '../../models/employee-shift.model';
import { ShiftsApi } from '../../../shifts/data-access/shifts-api';
import { Shift } from '../../../shifts/models/shift.model';
import { ApiError } from '../../../../core/models/api.models';

function localDate(): string {
  const date = new Date();
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2,'0')}-${String(date.getDate()).padStart(2,'0')}`;
}

@Component({
  selector: 'app-employee-shift-assignments',
  imports: [FormsModule, ButtonModule, DialogModule, MessageModule, SelectModule],
  templateUrl: './employee-shift-assignments.html',
  styleUrl: './employee-shift-assignments.scss'
})
export class EmployeeShiftAssignments {
  readonly employeeId = input.required<number>();
  private readonly api = inject(EmployeeShiftsApi);
  private readonly shiftsApi = inject(ShiftsApi);
  private readonly destroyRef = inject(DestroyRef);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly formError = signal('');
  readonly notice = signal('');
  readonly assignments = signal<EmployeeShiftAssignment[]>([]);
  readonly shifts = signal<Shift[]>([]);
  readonly selectedDate = signal(localDate());
  readonly effective = signal<EmployeeShiftAssignment | null>(null);
  readonly effectiveLoading = signal(false);
  readonly dialogOpen = signal(false);
  readonly selectedShiftId = signal<number | null>(null);
  readonly effectiveFrom = signal(localDate());

  readonly activeShifts = computed(() => this.shifts()
    .filter(s => s.isActive)
    .map(s => ({ label: `${s.name} (${this.time(s.startTime)} – ${this.time(s.endTime)})`, value: s.shiftId })));

  readonly history = computed(() => [...this.assignments()]
    .sort((a, b) => b.effectiveFrom.localeCompare(a.effectiveFrom)));

  readonly current = computed(() => this.assignments().find(a =>
    a.effectiveFrom <= this.selectedDate() &&
    (!a.effectiveTo || a.effectiveTo >= this.selectedDate())
  ) ?? null);

  readonly hasHistory = computed(() => this.assignments().length > 0);

  ngOnInit(): void { this.refresh(); }

  refresh(): void {
    this.loading.set(true);
    this.error.set('');
    forkJoin({
      history: this.api.list(this.employeeId()),
      shifts: this.shiftsApi.list(true)
    }).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ history, shifts }) => {
          this.assignments.set(history);
          this.shifts.set(shifts);
          this.loadEffective();
        },
        error: (err: ApiError) => this.error.set(this.errorText(err))
      });
  }

  loadEffective(): void {
    if (!this.selectedDate()) return;
    this.effective.set(null);
    this.effectiveLoading.set(true);
    this.api.getEffective(this.employeeId(), this.selectedDate())
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.effectiveLoading.set(false)))
      .subscribe({
        next: (value) => this.effective.set(value),
        error: (err: ApiError) => {
          if (err.status !== 404) this.error.set(this.errorText(err));
        }
      });
  }

  openAssign(): void {
    this.formError.set('');
    this.effectiveFrom.set(localDate());
    this.selectedShiftId.set(null);
    this.dialogOpen.set(true);
  }

  saveAssignment(): void {
    const shiftId = this.selectedShiftId();
    const date = this.effectiveFrom();
    if (!shiftId || !/^\d{4}-\d{2}-\d{2}$/.test(date)) {
      this.formError.set('Select an active shift and an effective date.');
      return;
    }
    this.saving.set(true);
    this.formError.set('');
    this.api.assign(this.employeeId(), { shiftId, effectiveFrom: date })
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.dialogOpen.set(false);
          this.notice.set('Shift assignment saved successfully.');
          this.selectedDate.set(date);
          this.refresh();
        },
        error: (err: ApiError) => this.formError.set(this.errorText(err))
      });
  }

  initializeDefault(): void {
    if (this.hasHistory() || this.saving()) return;
    this.saving.set(true);
    this.error.set('');
    this.api.initializeDefault(this.employeeId())
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.notice.set('Default shift initialized.');
          this.refresh();
        },
        error: (err: ApiError) => this.error.set(this.errorText(err))
      });
  }

  time(value: string): string { return value?.slice(0, 5) ?? '—'; }
  isOvernight(row: EmployeeShiftAssignment): boolean { return row.endTime < row.startTime; }
  status(row: EmployeeShiftAssignment): string {
    const today = localDate();
    if (row.effectiveFrom > today) return 'Upcoming';
    if (row.effectiveTo && row.effectiveTo < today) return 'Previous';
    return 'Current';
  }
  private errorText(err: ApiError): string { return err.message || 'Unable to complete request.'; }
}
