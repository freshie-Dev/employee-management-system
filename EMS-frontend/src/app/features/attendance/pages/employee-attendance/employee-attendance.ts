import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Observable, catchError, finalize, forkJoin, of, throwError } from 'rxjs';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { DataTable } from '../../../../core/shared/components/data-table/data-table';
import { DataTableColumnDirective } from '../../../../core/shared/components/data-table/data-table-column.directive';
import { DataTableConfig, TableAction, TableActionEvent, TableColumn } from '../../../../core/shared/components/data-table/data-table.model';
import { ApiError } from '../../../../core/models/api.models';
import { EmployeesApi } from '../../../employees/data-access/employees-api';
import { Employee } from '../../../employees/models/employee.model';
import { AttendanceApi } from '../../data-access/attendance-api';
import {
  AttendanceCorrectionRequest, AttendanceDay, AttendanceEvaluation,
  AttendanceException, AttendanceHistoryItem, AttendancePunch,
  CorrectionAction, PunchType
} from '../../models/attendance.model';

function dateOnly(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function fromDateOnly(value: string): Date {
  const [y, m, d] = value.split('-').map(Number);
  return new Date(y, m - 1, d);
}

// Backend currently assumes UTC+05:00. Keep correction inputs independent
// of the browser's timezone; do not use local Date parsing for these fields.
function utcToPakistanInput(value: string): string {
  return new Date(new Date(value).getTime() + 5 * 60 * 60_000)
    .toISOString().slice(0, 16);
}

function pakistanInputToUtc(value: string): string | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(value);
  if (!match) return null;
  const [y, mo, d, h, mi] = match.slice(1).map(Number);
  const utc = new Date(Date.UTC(y, mo - 1, d, h - 5, mi));
  // Round-trip validation prevents invalid browser/user-entered dates.
  return utcToPakistanInput(utc.toISOString()) === value
    ? utc.toISOString() : null;
}

@Component({
  selector: 'app-employee-attendance',
  standalone: true,
  imports: [
    FormsModule, RouterLink, ButtonModule, ConfirmDialogModule,
    DatePickerModule, DialogModule, InputTextModule, MessageModule,
    SelectModule, TagModule, DataTable, DataTableColumnDirective
  ],
  providers: [ConfirmationService],
  templateUrl: './employee-attendance.html',
  styleUrl: './employee-attendance.scss'
})
export class EmployeeAttendance {
  private readonly route = inject(ActivatedRoute);
  private readonly employeesApi = inject(EmployeesApi);
  private readonly api = inject(AttendanceApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly confirmation = inject(ConfirmationService);

  readonly employeeId = Number(this.route.snapshot.paramMap.get('id'));
  readonly employee = signal<Employee | null>(null);
  readonly employeeLoading = signal(false);
  readonly workDate = signal(new Date());
  readonly workDateText = computed(() => dateOnly(this.workDate()));
  readonly day = signal<AttendanceDay | null>(null);
  readonly punches = signal<AttendancePunch[]>([]);
  readonly exceptions = signal<AttendanceException[]>([]);
  readonly evaluation = signal<AttendanceEvaluation | null>(null);
  readonly loadingDay = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly notice = signal('');
  readonly formError = signal('');
  readonly dialogOpen = signal(false);
  readonly correctionAction = signal<CorrectionAction>('INSERT');
  readonly correctionPunch = signal<AttendancePunch | null>(null);
  readonly correctionType = signal<PunchType>('IN');
  readonly correctionTime = signal('');
  readonly correctionReason = signal('');

  readonly fromDate = signal(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
  readonly toDate = signal(new Date(new Date().getFullYear(), new Date().getMonth() + 1, 0));
  readonly history = signal<AttendanceHistoryItem[]>([]);
  readonly historyTotal = signal(0);
  readonly historyPage = signal(1);
  readonly historyPageSize = 10;
  readonly historyLoading = signal(false);
  readonly historyError = signal('');
  private dayRequest = 0;
  private historyRequest = 0;

  readonly historyColumns: TableColumn<AttendanceHistoryItem>[] = [
    { field: 'workDate', header: 'Work date', type: 'custom' },
    { field: 'attendanceStatus', header: 'Status', type: 'custom' },
    { field: 'workedMinutes', header: 'Worked', type: 'custom' },
    { field: 'punchCount', header: 'Punches', type: 'number' },
    { field: 'exceptionCount', header: 'Exceptions', type: 'number' }
  ];
  readonly historyConfig: DataTableConfig = {
    paginator: false,
    emptyMessage: 'No attendance records for this range.'
  };
  readonly historyActions: TableAction<AttendanceHistoryItem>[] = [
    { id: 'open', label: 'View this day', icon: 'pi pi-eye', severity: 'secondary' }
  ];
  readonly typeOptions = [
    { label: 'Check-in (IN)', value: 'IN' as PunchType },
    { label: 'Check-out (OUT)', value: 'OUT' as PunchType }
  ];

  constructor() {
    if (!Number.isInteger(this.employeeId) || this.employeeId <= 0) {
      this.error.set('Invalid employee ID.');
      return;
    }
    this.employeeLoading.set(true);
    this.employeesApi.get(this.employeeId)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.employeeLoading.set(false)))
      .subscribe({
        next: employee => this.employee.set(employee),
        error: err => this.error.set(this.message(err))
      });
    this.loadDay();
    this.loadHistory();
  }

  onWorkDateChange(date: Date | null): void {
    if (!date || Number.isNaN(date.getTime())) return;
    this.workDate.set(date);
    this.evaluation.set(null);
    this.notice.set('');
    this.loadDay();
  }

  loadDay(): void {
    const requestId = ++this.dayRequest;
    const date = this.workDateText();
    this.loadingDay.set(true);
    this.error.set('');
    const day$ = this.api.day(this.employeeId, date).pipe(
      catchError((err: ApiError) => err.status === 404
        ? of(null) : throwError(() => err))
    );
    forkJoin({
      day: day$,
      punches: this.api.punches(this.employeeId, date),
      exceptions: this.api.exceptions(this.employeeId, date)
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => {
        if (requestId === this.dayRequest) this.loadingDay.set(false);
      })
    ).subscribe({
      next: result => {
        if (requestId !== this.dayRequest) return;
        this.day.set(result.day);
        this.punches.set(result.punches);
        this.exceptions.set(result.exceptions);
      },
      error: err => {
        if (requestId === this.dayRequest) this.error.set(this.message(err));
      }
    });
  }

  loadHistory(): void {
    const from = dateOnly(this.fromDate());
    const to = dateOnly(this.toDate());
    if (from > to || this.toDate().getTime() - this.fromDate().getTime() > 366 * 86400000) {
      this.historyError.set('Select a valid date range of no more than 366 days.');
      return;
    }
    const requestId = ++this.historyRequest;
    this.historyLoading.set(true);
    this.historyError.set('');
    this.api.history(this.employeeId, from, to, this.historyPage(), this.historyPageSize)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => {
        if (requestId === this.historyRequest) this.historyLoading.set(false);
      }))
      .subscribe({
        next: result => {
          if (requestId !== this.historyRequest) return;
          this.history.set(result.items);
          this.historyTotal.set(result.totalCount);
        },
        error: err => {
          if (requestId === this.historyRequest) this.historyError.set(this.message(err));
        }
      });
  }

  applyHistoryRange(): void {
    this.historyPage.set(1);
    this.loadHistory();
  }

  changeHistoryPage(direction: number): void {
    const next = this.historyPage() + direction;
    if (next < 1 || (direction > 0 && (next - 1) * this.historyPageSize >= this.historyTotal())) return;
    this.historyPage.set(next);
    this.loadHistory();
  }

  onHistoryAction(event: TableActionEvent<AttendanceHistoryItem>): void {
    if (event.action === 'open') this.onWorkDateChange(fromDateOnly(event.row.workDate));
  }

  recordPunch(type: PunchType): void {
    this.write(this.api.recordPunch(this.employeeId, this.workDateText(), type),
      `${type === 'IN' ? 'Check-in' : 'Check-out'} recorded using server time.`);
  }

  evaluate(finalizeAttendance: boolean): void {
    const perform = () => {
      this.busy.set(true);
      this.error.set('');
      this.api.evaluate(this.employeeId, this.workDateText(), finalizeAttendance)
        .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false)))
        .subscribe({
          next: result => {
            this.evaluation.set(result);
            this.notice.set(finalizeAttendance ? 'Attendance finalized.' : 'Attendance evaluated. Not finalized.');
            this.loadDay();
            this.loadHistory();
          },
          error: err => this.error.set(this.message(err))
        });
    };
    if (!finalizeAttendance) { perform(); return; }
    this.confirmation.confirm({
      header: 'Finalize attendance?',
      message: 'Finalization locks normal punch capture. Future changes require an audited HR correction.',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Finalize',
      rejectLabel: 'Cancel',
      accept: perform
    });
  }

  openCorrection(action: CorrectionAction, punch: AttendancePunch | null = null): void {
    if (!this.day()) {
      this.error.set('Create an attendance record before correcting punches.');
      return;
    }
    this.correctionAction.set(action);
    this.correctionPunch.set(punch);
    this.correctionType.set(punch?.punchType ?? 'IN');
    this.correctionTime.set(punch ? utcToPakistanInput(punch.punchAtUtc) : '');
    this.correctionReason.set('');
    this.formError.set('');
    this.dialogOpen.set(true);
  }

  submitCorrection(): void {
    if (this.busy()) return;
    const action = this.correctionAction();
    const punch = this.correctionPunch();
    const reason = this.correctionReason().trim();
    if (reason.length < 5 || reason.length > 500) {
      this.formError.set('Enter a reason between 5 and 500 characters.');
      return;
    }
    if (action !== 'INSERT' && !punch) {
      this.formError.set('Select a punch first.');
      return;
    }
    const utc = action === 'DELETE' ? null : pakistanInputToUtc(this.correctionTime());
    if (action !== 'DELETE' && !utc) {
      this.formError.set('Enter a valid date and time (Pakistan time, UTC+05:00).');
      return;
    }
    const body: AttendanceCorrectionRequest = { action, reason };
    if (punch && action !== 'INSERT') {
      body.punchId = punch.punchId;
      body.expectedPunchType = punch.punchType;
      body.expectedPunchAtUtc = punch.punchAtUtc;
    }
    if (action !== 'DELETE') {
      body.punchType = this.correctionType();
      body.punchAtUtc = utc;
    }
    this.write(this.api.correct(this.employeeId, this.workDateText(), body),
      'Correction saved. Re-evaluate and finalize this day.', true);
  }

  private write(request: Observable<unknown>, success: string, closeDialog = false): void {
    if (this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.formError.set('');
    request.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false)))
      .subscribe({
        next: () => {
          this.notice.set(success);
          this.evaluation.set(null);
          if (closeDialog) this.dialogOpen.set(false);
          this.loadDay();
          this.loadHistory();
        },
        error: err => {
          const message = this.message(err);
          if (closeDialog) this.formError.set(message);
          else this.error.set(message);
        }
      });
  }

  statusSeverity(status: string | null | undefined): 'success' | 'warn' | 'danger' | 'secondary' {
    switch (status) {
      case 'PRESENT': return 'success';
      case 'ABSENT': return 'danger';
      case 'INCOMPLETE': return 'warn';
      default: return 'secondary';
    }
  }

  displayWorkDate(value: string): string {
    return fromDateOnly(value).toLocaleDateString('en-PK', {
      day: '2-digit', month: 'short', year: 'numeric'
    });
  }

  displayTime(value: string | null | undefined): string {
    if (!value) return '—';
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return '—';
    return new Intl.DateTimeFormat('en-PK', {
      timeZone: 'Asia/Karachi', hour: '2-digit', minute: '2-digit',
      day: '2-digit', month: 'short', hour12: true
    }).format(date);
  }

  displayMinutes(value: number | null | undefined): string {
    return value == null ? '—' : `${Math.floor(value / 60)}h ${value % 60}m`;
  }

  private message(err: unknown): string {
    const apiError = err as Partial<ApiError> | null;
    return apiError?.message || 'The request could not be completed.';
  }
}
