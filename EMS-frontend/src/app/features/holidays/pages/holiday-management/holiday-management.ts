import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, switchMap, throwError } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { DatePickerModule } from 'primeng/datepicker';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';

import { DataTable } from '../../../../core/shared/components/data-table/data-table';
import { DataTableColumnDirective } from '../../../../core/shared/components/data-table/data-table-column.directive';
import { DataTableConfig, TableAction, TableActionEvent, TableColumn } from '../../../../core/shared/components/data-table/data-table.model';
import { ApiError } from '../../../../core/models/api.models';
import { SchedulesApi } from '../../../schedules/data-access/schedules-api';
import { HolidaysApi } from '../../data-access/holidays-api';
import { Holiday, HolidayCalendar, SaveHolidayRequest } from '../../models/holiday.model';

/** Keep DateOnly strings date-only: never use toISOString(), which can shift the day in timezones. */
function toDateOnly(value: Date): string {
  const y = value.getFullYear();
  const m = String(value.getMonth() + 1).padStart(2, '0');
  const d = String(value.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

function fromDateOnly(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}

@Component({
  selector: 'app-holiday-management',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    ConfirmDialogModule,
    DatePickerModule,
    DialogModule,
    InputTextModule,
    MessageModule,
    TagModule,
    DataTable,
    DataTableColumnDirective
  ],
  providers: [ConfirmationService],
  templateUrl: './holiday-management.html',
  styleUrl: './holiday-management.scss'
})
export class HolidayManagement {
  private readonly api = inject(HolidaysApi);
  private readonly schedulesApi = inject(SchedulesApi);
  private readonly confirmation = inject(ConfirmationService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly fb = inject(FormBuilder);

  readonly calendars = signal<HolidayCalendar[]>([]);
  readonly selectedCalendarId = signal<number | null>(null);
  readonly selectedCalendar = computed(() =>
    this.calendars().find(c => c.calendarId === this.selectedCalendarId()) ?? null
  );
  readonly holidays = signal<Holiday[]>([]);
  readonly year = signal(new Date().getFullYear());
  readonly loadingCalendars = signal(false);
  readonly loadingHolidays = signal(false);
  readonly savingCalendar = signal(false);
  readonly savingHoliday = signal(false);
  readonly processing = signal(false);
  readonly calendarDialogOpen = signal(false);
  readonly holidayDialogOpen = signal(false);
  readonly editingCalendar = signal<HolidayCalendar | null>(null);
  readonly editingHoliday = signal<Holiday | null>(null);
  readonly notice = signal('');
  readonly error = signal('');
  readonly formError = signal('');

  private calendarRequestId = 0;
  private holidayRequestId = 0;

  readonly activeCount = computed(() => this.calendars().filter(c => c.isActive).length);

  readonly calendarForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(100)]]
  });

  readonly holidayForm = this.fb.group({
    name: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(150)]),
    holidayDate: this.fb.control<Date | null>(null, Validators.required)
  });

  readonly columns: TableColumn<Holiday>[] = [
    { field: 'holidayDate', header: 'Date', type: 'custom', sortable: true, width: '190px' },
    { field: 'name', header: 'Holiday', type: 'custom', sortable: true }
  ];

  readonly tableConfig: DataTableConfig = {
    searchMode: 'local',
    searchPlaceholder: 'Search holidays by date or name...',
    paginator: true,
    rows: 10,
    rowsPerPageOptions: [10, 20, 50],
    emptyMessage: 'No holidays in this calendar for the selected year.'
  };

  readonly rowActions: TableAction<Holiday>[] = [
    { id: 'edit', label: 'Edit holiday', icon: 'pi pi-pencil', severity: 'secondary',
      disabled: () => !this.selectedCalendar()?.isActive || this.processing() },
    { id: 'delete', label: 'Delete holiday', icon: 'pi pi-trash', severity: 'danger',
      disabled: () => !this.selectedCalendar()?.isActive || this.processing() }
  ];

  constructor() {
    this.loadCalendars();
  }

  loadCalendars(preferredId?: number): void {
    const requestId = ++this.calendarRequestId;
    this.loadingCalendars.set(true);
    this.error.set('');
    this.api.listCalendars(true)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => {
        if (requestId === this.calendarRequestId) this.loadingCalendars.set(false);
      }))
      .subscribe({
        next: items => {
          if (requestId !== this.calendarRequestId) return;
          this.calendars.set(items);
          const requested = preferredId ?? this.selectedCalendarId();
          const chosen = items.find(c => c.calendarId === requested)
            ?? items.find(c => c.isDefault)
            ?? items.find(c => c.isActive)
            ?? items[0];
          this.selectedCalendarId.set(chosen?.calendarId ?? null);
          this.loadHolidays();
        },
        error: err => {
          if (requestId === this.calendarRequestId) {
            this.error.set(this.errorText(err));
            this.holidays.set([]);
          }
        }
      });
  }

  chooseCalendar(id: number): void {
    if (id === this.selectedCalendarId()) return;
    this.selectedCalendarId.set(id);
    this.notice.set('');
    this.loadHolidays();
  }

  moveYear(delta: number): void {
    const next = this.year() + delta;
    if (next < 1900 || next > 9998) return;
    this.year.set(next);
    this.loadHolidays();
  }

  loadHolidays(): void {
    const requestId = ++this.holidayRequestId;
    const calendarId = this.selectedCalendarId();
    if (calendarId === null) {
      this.holidays.set([]);
      this.loadingHolidays.set(false);
      return;
    }
    this.error.set('');
    this.loadingHolidays.set(true);
    const y = this.year();
    this.api.listHolidays(calendarId, `${y}-01-01`, `${y}-12-31`)
      .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => {
        if (requestId === this.holidayRequestId) this.loadingHolidays.set(false);
      }))
      .subscribe({
        next: items => {
          if (requestId === this.holidayRequestId) this.holidays.set(items);
        },
        error: err => {
          if (requestId === this.holidayRequestId) {
            this.error.set(this.errorText(err));
            this.holidays.set([]);
          }
        }
      });
  }

  openNewCalendar(): void {
    this.editingCalendar.set(null);
    this.calendarForm.reset({ name: '' });
    this.formError.set('');
    this.calendarDialogOpen.set(true);
  }

  openEditCalendar(item: HolidayCalendar): void {
    this.editingCalendar.set(item);
    this.calendarForm.reset({ name: item.name });
    this.formError.set('');
    this.calendarDialogOpen.set(true);
  }

  saveCalendar(): void {
    if (this.savingCalendar()) return;
    if (this.calendarForm.invalid) {
      this.calendarForm.markAllAsTouched();
      return;
    }
    const name = this.calendarForm.getRawValue().name.trim();
    if (!name) { this.formError.set('Enter a calendar name.'); return; }
    const existing = this.editingCalendar();
    this.savingCalendar.set(true);
    this.formError.set('');
    const request = existing
      ? this.api.updateCalendar(existing.calendarId, { name })
      : this.api.createCalendar({ name });
    request.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.savingCalendar.set(false)))
      .subscribe({
        next: result => {
          this.calendarDialogOpen.set(false);
          this.notice.set(existing ? 'Calendar updated.' : 'Holiday calendar created.');
          this.loadCalendars(result.calendarId);
        },
        error: err => this.formError.set(this.errorText(err))
      });
  }

  confirmCalendarStatus(item: HolidayCalendar): void {
    if (this.processing()) return;
    if (item.isDefault && item.isActive) {
      this.error.set('Change the organization default calendar before deactivating this calendar.');
      return;
    }
    const nextStatus = !item.isActive;
    this.confirmation.confirm({
      header: nextStatus ? 'Activate calendar' : 'Deactivate calendar',
      message: `Are you sure you want to ${nextStatus ? 'activate' : 'deactivate'} "${item.name}"?`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: nextStatus ? 'Activate' : 'Deactivate',
      accept: () => {
        this.processing.set(true);
        this.error.set('');
        this.api.setCalendarStatus(item.calendarId, nextStatus)
          .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.processing.set(false)))
          .subscribe({
            next: () => {
              this.notice.set(`Calendar ${nextStatus ? 'activated' : 'deactivated'}.`);
              this.loadCalendars(item.calendarId);
            },
            error: err => this.error.set(this.errorText(err))
          });
      }
    });
  }

  setAsDefault(item: HolidayCalendar): void {
    if (!item.isActive || item.isDefault || this.processing()) return;
    this.confirmation.confirm({
      header: 'Set organization default',
      message: `Use "${item.name}" as the default holiday calendar? Existing shifts and work schedules will be preserved.`,
      acceptLabel: 'Set default',
      accept: () => {
        this.processing.set(true);
        this.error.set('');
        // Read the current defaults first. PUT requires all three IDs.
        this.schedulesApi.getSettings().pipe(
          switchMap(settings => {
            if (settings.defaultShiftId === null || settings.defaultScheduleId === null) {
              return throwError(() => new Error(
                'Configure a default shift and work schedule on Work Schedules before setting a holiday calendar.'));
            }
            return this.schedulesApi.updateSettings({
              defaultShiftId: settings.defaultShiftId,
              defaultScheduleId: settings.defaultScheduleId,
              defaultHolidayCalendarId: item.calendarId
            });
          }),
          takeUntilDestroyed(this.destroyRef),
          finalize(() => this.processing.set(false))
        ).subscribe({
          next: () => {
            this.notice.set(`"${item.name}" is now the organization default calendar.`);
            this.loadCalendars(item.calendarId);
          },
          error: err => this.error.set(this.errorText(err))
        });
      }
    });
  }

  openNewHoliday(): void {
    if (!this.selectedCalendar()?.isActive) return;
    this.editingHoliday.set(null);
    this.holidayForm.reset({ name: '', holidayDate: new Date(this.year(), 0, 1) });
    this.formError.set('');
    this.holidayDialogOpen.set(true);
  }

  openEditHoliday(item: Holiday): void {
    if (!this.selectedCalendar()?.isActive) return;
    this.editingHoliday.set(item);
    this.holidayForm.reset({ name: item.name, holidayDate: fromDateOnly(item.holidayDate) });
    this.formError.set('');
    this.holidayDialogOpen.set(true);
  }

  onHolidayTableAction(event: TableActionEvent<Holiday>): void {
    switch (event.action) {
      case 'edit': this.openEditHoliday(event.row); break;
      case 'delete': this.confirmDeleteHoliday(event.row); break;
    }
  }

  saveHoliday(): void {
    const calendarId = this.selectedCalendarId();
    if (this.savingHoliday() || calendarId === null || !this.selectedCalendar()?.isActive) return;
    if (this.holidayForm.invalid) {
      this.holidayForm.markAllAsTouched();
      return;
    }
    const { name, holidayDate } = this.holidayForm.getRawValue();
    if (!name.trim() || !holidayDate) {
      this.formError.set('Provide both a name and date.');
      return;
    }
    const body: SaveHolidayRequest = { name: name.trim(), holidayDate: toDateOnly(holidayDate) };
    const existing = this.editingHoliday();
    this.savingHoliday.set(true);
    this.formError.set('');
    const request = existing
      ? this.api.updateHoliday(calendarId, existing.holidayId, body)
      : this.api.createHoliday(calendarId, body);
    request.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.savingHoliday.set(false)))
      .subscribe({
        next: () => {
          this.holidayDialogOpen.set(false);
          this.notice.set(existing ? 'Holiday updated.' : 'Holiday added.');
          this.year.set(holidayDate.getFullYear());
          this.loadCalendars(calendarId);
        },
        error: err => this.formError.set(this.errorText(err))
      });
  }

  confirmDeleteHoliday(item: Holiday): void {
    const calendarId = this.selectedCalendarId();
    if (calendarId === null || this.processing() || !this.selectedCalendar()?.isActive) return;
    this.confirmation.confirm({
      header: 'Delete holiday',
      message: `Delete "${item.name}" (${item.holidayDate})? This permanently removes the holiday record.`,
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      acceptButtonProps: { severity: 'danger' },
      accept: () => {
        this.processing.set(true);
        this.error.set('');
        this.api.deleteHoliday(calendarId, item.holidayId)
          .pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.processing.set(false)))
          .subscribe({
            next: () => {
              this.notice.set('Holiday deleted.');
              this.loadHolidays();
              this.loadCalendars(calendarId);
            },
            error: err => this.error.set(this.errorText(err))
          });
      }
    });
  }

  /** Local date constructor avoids accidental timezone shifts on DateOnly values. */
  displayDate(value: string): string {
    return new Intl.DateTimeFormat('en-PK', {
      day: '2-digit', month: 'short', year: 'numeric'
    }).format(fromDateOnly(value));
  }

  private errorText(err: ApiError | Error): string {
    if (err instanceof Error) return err.message;
    const validation = err?.validationErrors && Object.values(err.validationErrors).flat();
    return validation?.length ? validation.join(' ') : err?.message || 'Request failed. Check the API connection.';
  }
}
