import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, switchMap } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { Plus } from '@primeicons/angular/plus';
import { ApiError } from '../../../../core/models/api.models';
import { DataTable } from '../../../../core/shared/components/data-table/data-table';
import { DataTableColumnDirective } from '../../../../core/shared/components/data-table/data-table-column.directive';
import { DataTableConfig, TableAction, TableActionEvent, TableColumn } from '../../../../core/shared/components/data-table/data-table.model';
import { ShiftsApi } from '../../data-access/shifts-api';
import { Shift, CreateShiftRequest } from '../../models/shift.model';
import { ShiftForm } from '../../components/shift-form/shift-form';

@Component({
  selector: 'app-shift-list',
  imports: [ButtonModule, DialogModule, MessageModule, ConfirmDialogModule, Plus,
    DataTable, DataTableColumnDirective, ShiftForm],
  providers: [ConfirmationService],
  templateUrl: './shift-list.html',
  styleUrl: './shift-list.scss'
})
export class ShiftList {
  private readonly api = inject(ShiftsApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly confirmations = inject(ConfirmationService);
  readonly shifts = signal<Shift[]>([]);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly processingId = signal<number | null>(null);
  readonly selected = signal<Shift | null>(null);
  readonly dialogOpen = signal(false);
  readonly error = signal('');
  readonly formError = signal('');
  readonly notice = signal('');
  private loadVersion = 0;
  readonly columns: TableColumn<Shift>[] = [
    { field: 'name', header: 'Shift', type: 'custom', sortable: true },
    { field: 'startTime', header: 'Starts', type: 'custom' },
    { field: 'endTime', header: 'Ends', type: 'custom' },
    { field: 'breakMinutes', header: 'Break (min)', type: 'number' },
    { field: 'effectiveRequiredMinutes', header: 'Required (min)', type: 'number' },
    { field: 'graceInMinutes', header: 'Entry grace', type: 'number' },
    { field: 'graceOutMinutes', header: 'Exit grace', type: 'number' },
    { field: 'isActive', header: 'Status', type: 'status' }
  ];
  readonly tableConfig: DataTableConfig = {
    paginator: true, rows: 10, rowsPerPageOptions: [10, 20, 50],
    searchMode: 'local', searchPlaceholder: 'Search shifts...', emptyMessage: 'No shifts found'
  };
  readonly rowActions: TableAction<Shift>[] = [
    { id: 'edit', label: 'Edit shift and policy', icon: 'pi pi-pencil', severity: 'secondary' },
    { id: 'toggle', label: 'Change status', icon: 'pi pi-power-off', severity: 'warn',
      disabled: () => this.processingId() !== null,
      loading: row => this.processingId() === row.shiftId }
  ];
  constructor() { this.load(); }
  load(): void {
    const version = ++this.loadVersion;
    this.error.set(''); this.loading.set(true);
    this.api.list(true).pipe(takeUntilDestroyed(this.destroyRef), finalize(() => {
      if (version === this.loadVersion) this.loading.set(false);
    })).subscribe({
      next: items => { if (version === this.loadVersion) this.shifts.set(items); },
      error: err => { if (version === this.loadVersion) this.error.set(this.errorText(err)); }
    });
  }
  openCreate(): void { this.selected.set(null); this.formError.set(''); this.dialogOpen.set(true); }
  openEdit(row: Shift): void { this.selected.set(row); this.formError.set(''); this.dialogOpen.set(true); }
  onAction(event: TableActionEvent<Shift>): void {
    if (event.action === 'edit') this.openEdit(event.row);
    if (event.action === 'toggle') this.confirmStatus(event.row);
  }
  save(body: CreateShiftRequest): void {
    if (this.saving()) return;
    this.saving.set(true); this.formError.set('');
    const selected = this.selected();
    // The backend intentionally has separate PUTs for shift details and policy.
    // If details save but policy fails, the dialog remains open and displays the error.
    const request = selected
      ? this.api.update(selected.shiftId, {
          name: body.name, startTime: body.startTime, endTime: body.endTime,
          breakMinutes: body.breakMinutes
        }).pipe(switchMap(() => this.api.updatePolicy(selected.shiftId, {
          graceInMinutes: body.graceInMinutes,
          graceOutMinutes: body.graceOutMinutes,
          requiredMinutes: body.requiredMinutes
        })))
      : this.api.create(body);
    request.pipe(takeUntilDestroyed(this.destroyRef), finalize(() => this.saving.set(false)))
      .subscribe({
        next: () => {
          this.dialogOpen.set(false);
          this.notice.set(selected ? 'Shift updated successfully.' : 'Shift created successfully.');
          this.load();
        },
        error: (err: ApiError) => this.formError.set(this.errorText(err))
      });
  }
  confirmStatus(row: Shift): void {
    const next = !row.isActive;
    this.confirmations.confirm({
      header: next ? 'Activate shift' : 'Deactivate shift',
      message: `Are you sure you want to ${next ? 'activate' : 'deactivate'} ${row.name}?`,
      acceptLabel: 'Confirm', rejectLabel: 'Cancel',
      accept: () => this.setStatus(row, next)
    });
  }
  private setStatus(row: Shift, active: boolean): void {
    if (this.processingId() !== null) return;
    this.processingId.set(row.shiftId); this.error.set('');
    this.api.setStatus(row.shiftId, active).pipe(
      takeUntilDestroyed(this.destroyRef), finalize(() => this.processingId.set(null)))
      .subscribe({
        next: () => { this.notice.set(`Shift ${active ? 'activated' : 'deactivated'}.`); this.load(); },
        error: (err: ApiError) => this.error.set(this.errorText(err))
      });
  }
  private errorText(err: ApiError): string {
    const issues = err?.validationErrors && Object.values(err.validationErrors).flat();
    return issues?.length ? issues.join(' ') : err?.message || 'The request failed.';
  }
}
