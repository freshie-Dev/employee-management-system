
import {
  Component,
  DestroyRef,
  inject,
  signal
} from '@angular/core';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { Plus } from '@primeicons/angular/plus';

import { ApiError } from '../../../../core/models/api.models';

import { DesignationsApi } from '../../data-access/designations-api';

import {
  Designation,
  DesignationRequest
} from '../../models/designation.model';

import { DesignationForm } from '../../components/designation-form/designation-form';

import { DataTable } from '../../../../core/shared/components/data-table/data-table';
import { DataTableColumnDirective } from '../../../../core/shared/components/data-table/data-table-column.directive';
import { DataTableConfig, TableAction, TableActionEvent, TableColumn } from '../../../../core/shared/components/data-table/data-table.model';

@Component({
  selector: 'app-designation-list',
  imports: [
    ButtonModule,
    DialogModule,
    MessageModule,
    ConfirmDialogModule,
    DesignationForm,
    DataTable,
    DataTableColumnDirective,
    Plus
  ],
  providers: [ConfirmationService],
  templateUrl: './designation-list.html',
  styleUrl: './designation-list.scss'
})
export class DesignationList {

  // ========================================
  // Dependencies
  // ========================================

  private readonly api = inject(DesignationsApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly confirmation = inject(ConfirmationService);

  // ========================================
  // State
  // ========================================

  readonly designations = signal<Designation[]>([]);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly processingId = signal<number | null>(null);

  readonly dialogOpen = signal(false);
  readonly selected = signal<Designation | null>(null);

  readonly error = signal('');
  readonly formError = signal('');
  readonly notice = signal('');

  // Prevent outdated responses from replacing newer ones.
  private loadVersion = 0;

  // ========================================
  // Table columns
  // ========================================

  readonly columns: TableColumn<Designation>[] = [
    {
      field: 'title',
      header: 'Designation',
      type: 'custom',
      sortable: true
    },
    {
      field: 'code',
      header: 'Code',
      type: 'custom',
      sortable: true
    },
    {
      field: 'description',
      header: 'Description'
    },
    {
      field: 'isActive',
      header: 'Status',
      type: 'status',
      trueLabel: 'Active',
      falseLabel: 'Inactive'
    }
  ];

  // ========================================
  // Table configuration
  // ========================================

  readonly tableConfig: DataTableConfig = {
    paginator: true,
    rows: 10,
    rowsPerPageOptions: [10, 20, 50],

    searchMode: 'local',
    searchPlaceholder: 'Search designations...',

    stripedRows: false,
    showGridlines: false,

    emptyMessage: 'No designations found'
  };

  // ========================================
  // Row actions
  // ========================================

  readonly rowActions: TableAction<Designation>[] = [
    {
      id: 'edit',
      label: 'Edit designation',
      icon: 'pi pi-pencil',
      severity: 'secondary'
    },
    {
      id: 'toggle-status',
      label: 'Change designation status',
      icon: 'pi pi-power-off',
      severity: 'warn',
      disabled: row =>
        this.processingId() !== null,
      loading: row =>
        this.processingId() === row.designationId
    }
  ];

  // ========================================
  // Initialization
  // ========================================

  constructor() {
    this.load();
  }

  // ========================================
  // Load
  // ========================================

  load(): void {
    const version = ++this.loadVersion;

    this.error.set('');
    this.loading.set(true);

    this.api.list()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => {
          if (version === this.loadVersion) {
            this.loading.set(false);
          }
        })
      )
      .subscribe({
        next: items => {
          if (version !== this.loadVersion) return;
          this.designations.set(items);
        },
        error: (err: ApiError) => {
          if (version !== this.loadVersion) return;
          this.error.set(this.errorText(err));
        }
      });
  }

  // ========================================
  // Table actions
  // ========================================

  onTableAction(
    event: TableActionEvent<Designation>
  ): void {
    switch (event.action) {
      case 'edit':
        this.openEdit(event.row);
        break;

      case 'toggle-status':
        this.confirmStatus(event.row);
        break;
    }
  }

  // ========================================
  // Dialog handling
  // ========================================

  openCreate(): void {
    this.selected.set(null);
    this.formError.set('');
    this.dialogOpen.set(true);
  }

  openEdit(item: Designation): void {
    this.selected.set(item);
    this.formError.set('');
    this.dialogOpen.set(true);
  }

  // ========================================
  // Create / Update
  // ========================================

  save(data: DesignationRequest): void {
    if (this.saving()) return;

    this.formError.set('');
    this.saving.set(true);

    const existing = this.selected();

    const request = existing
      ? this.api.update(existing.designationId, data)
      : this.api.create(data);

    request
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false))
      )
      .subscribe({
        next: () => {
          this.dialogOpen.set(false);

          this.notice.set(
            existing
              ? 'Designation updated successfully.'
              : 'Designation created successfully.'
          );

          this.load();
        },

        error: (err: ApiError) => {
          this.formError.set(this.errorText(err));
        }
      });
  }

  // ========================================
  // Activate / Deactivate
  // ========================================

  confirmStatus(item: Designation): void {
    if (this.processingId() !== null) return;

    const nextStatus = !item.isActive;

    this.confirmation.confirm({
      header: nextStatus
        ? 'Activate designation'
        : 'Deactivate designation',

      message: `Are you sure you want to ${
        nextStatus ? 'activate' : 'deactivate'
      } ${item.title}?`,

      icon: 'pi pi-exclamation-circle',

      acceptLabel: 'Confirm',
      rejectLabel: 'Cancel',

      accept: () => this.setStatus(item, nextStatus)
    });
  }

  private setStatus(
    item: Designation,
    isActive: boolean
  ): void {
    if (this.processingId() !== null) return;

    this.error.set('');
    this.processingId.set(item.designationId);

    this.api.setStatus(item.designationId, isActive)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.processingId.set(null))
      )
      .subscribe({
        next: () => {
          this.notice.set(
            `Designation ${
              isActive ? 'activated' : 'deactivated'
            } successfully.`
          );

          this.load();
        },

        error: (err: ApiError) => {
          this.error.set(this.errorText(err));
        }
      });
  }

  // ========================================
  // Error handling
  // ========================================

  private errorText(err: ApiError): string {
    const issues = err?.validationErrors &&
      Object.values(err.validationErrors).flat();

    return issues?.length
      ? issues.join(' ')
      : err?.message || 'The request failed.';
  }
}
