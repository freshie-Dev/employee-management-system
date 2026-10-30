
import {
  Component,
  DestroyRef,
  inject,
  signal
} from '@angular/core';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Subject, finalize, switchMap, tap } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { Plus } from '@primeicons/angular/plus';

import { DepartmentsApi } from '../../data-access/departments-api';

import {
  Department,
  DepartmentRequest
} from '../../models/department.model';

import { DepartmentForm } from '../../components/department-form/department-form';

import { ApiError } from '../../../../core/models/api.models';
import { DataTable } from '../../../../core/shared/components/data-table/data-table';
import { DataTableColumnDirective } from '../../../../core/shared/components/data-table/data-table-column.directive';
import { DataTableConfig, TableAction, TableActionEvent, TableColumn } from '../../../../core/shared/components/data-table/data-table.model';




@Component({
  selector: 'app-department-list',
  imports: [
    ButtonModule,
    DialogModule,
    MessageModule,
    ConfirmDialogModule,
    DepartmentForm,
    DataTable,
    DataTableColumnDirective,
    Plus
  ],
  providers: [ConfirmationService],
  templateUrl: './department-list.html',
  styleUrl: './department-list.scss'
})
export class DepartmentList {

  // ========================================
  // Dependencies
  // ========================================

  private readonly api = inject(DepartmentsApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly confirmation = inject(ConfirmationService);

  // ========================================
  // State
  // ========================================

  readonly departments = signal<Department[]>([]);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly processingId = signal<number | null>(null);

  readonly dialogOpen = signal(false);
  readonly selected = signal<Department | null>(null);

  readonly error = signal('');
  readonly formError = signal('');
  readonly notice = signal('');

  private readonly loadRequests = new Subject<string>();

  search = '';

  // ========================================
  // Table configuration
  // ========================================

  readonly columns: TableColumn<Department>[] = [
    {
      field: 'name',
      header: 'Name',
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
      field: 'parentDepartmentId',
      header: 'Parent Department',
      type: 'custom'
    },
    {
      field: 'isActive',
      header: 'Status',
      type: 'status',
      trueLabel: 'Active',
      falseLabel: 'Inactive'
    }
  ];

  readonly tableConfig: DataTableConfig = {
    paginator: true,
    rows: 10,
    rowsPerPageOptions: [10, 20, 50],

    searchMode: "local",
    searchPlaceholder: 'Search departments...',

    stripedRows: false,
    showGridlines: false,

    emptyMessage: 'No departments found'
  };

  readonly rowActions: TableAction<Department>[] = [
    {
      id: 'edit',
      label: 'Edit department',
      icon: 'pi pi-pencil',
      severity: 'secondary'
    },
    {
      id: 'toggle-status',
      label: 'Change department status',
      icon: 'pi pi-power-off',
      severity: 'warn',
      loading: (row:any) =>
        this.processingId() === row.departmentId
    }
  ];

  // ========================================
  // Initialization
  // ========================================

  constructor() {
    this.loadRequests
      .pipe(
        tap(() => {
          this.error.set('');
          this.loading.set(true);
        }),
        switchMap(search =>
          this.api.list(search || undefined).pipe(
            // A replacement request cancels the previous subscription.
            // Loading is cleared when the latest request completes.
            finalize(() => {})
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: items => {
          this.departments.set(items);
          this.loading.set(false);
        },
        error: (err: ApiError) => {
          this.error.set(this.errorText(err));
          this.loading.set(false);
        }
      });

    this.load();
  }

  // ========================================
  // Load departments
  // ========================================

  load(): void {
    this.loadRequests.next(this.search.trim());
  }

  onTableSearch(value: string): void {
    this.search = value;
    this.load();
  }

  // ========================================
  // Table actions
  // ========================================

  onTableAction(
    event: TableActionEvent<Department>
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
  // Department helpers
  // ========================================

  parentName(id: number | null): string {
    if (id === null) {
      return '—';
    }

    return this.departments().find(
      department => department.departmentId === id
    )?.name ?? `#${id}`;
  }

  // ========================================
  // Dialog
  // ========================================

  openCreate(): void {
    this.selected.set(null);
    this.formError.set('');
    this.dialogOpen.set(true);
  }

  openEdit(item: Department): void {
    this.selected.set(item);
    this.formError.set('');
    this.dialogOpen.set(true);
  }

  // ========================================
  // Create / Update
  // ========================================

  save(data: DepartmentRequest): void {
    if (this.saving()) {
      return;
    }

    this.formError.set('');
    this.saving.set(true);

    const existing = this.selected();

    const request = existing
      ? this.api.update(existing.departmentId, data)
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
              ? 'Department updated successfully.'
              : 'Department created successfully.'
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

  confirmStatus(item: Department): void {
    const nextStatus = !item.isActive;

    this.confirmation.confirm({
      header: nextStatus
        ? 'Activate department'
        : 'Deactivate department',

      message: `Are you sure you want to ${
        nextStatus ? 'activate' : 'deactivate'
      } ${item.name}?`,

      icon: 'pi pi-exclamation-circle',

      acceptLabel: 'Confirm',
      rejectLabel: 'Cancel',

      accept: () => this.setStatus(item, nextStatus)
    });
  }

  private setStatus(
    item: Department,
    isActive: boolean
  ): void {
    this.error.set('');
    this.processingId.set(item.departmentId);

    this.api.setStatus(item.departmentId, isActive)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.processingId.set(null))
      )
      .subscribe({
        next: () => {
          this.notice.set(
            `Department ${
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
