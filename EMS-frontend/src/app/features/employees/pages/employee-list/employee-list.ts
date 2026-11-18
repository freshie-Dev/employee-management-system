
import {
  Component,
  DestroyRef,
  inject,
  signal,
  computed
} from '@angular/core';

import {
  takeUntilDestroyed
} from '@angular/core/rxjs-interop';

import {
  Subject,
  EMPTY,
  catchError,
  debounceTime,
  switchMap,
  tap,
  finalize
} from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';
import { SelectModule } from 'primeng/select';
import { FormsModule } from '@angular/forms';

import { Plus } from '@primeicons/angular/plus';

import { ApiError } from '../../../../core/models/api.models';



import { EmployeesApi } from '../../data-access/employees-api';

import {
  Employee,
  EmployeePage,
  EmploymentStatus,
  CreateEmployeeRequest,
  UpdateEmployeeRequest,
  AvailableUser,
} from '../../models/employee.model';

import {
  EmployeeForm,
  EmployeeFormValue
} from '../../components/employee-form/employee-form';
import { DataTable } from '../../../../core/shared/components/data-table/data-table';
import { DataTableColumnDirective } from '../../../../core/shared/components/data-table/data-table-column.directive';
import { DataTableConfig, DataTablePageEvent, TableAction, TableActionEvent, TableColumn } from '../../../../core/shared/components/data-table/data-table.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-employee-list',
  imports: [
    FormsModule,
    ButtonModule,
    DialogModule,
    MessageModule,
    ConfirmDialogModule,
    SelectModule,
    Plus,
    DataTable,
    DataTableColumnDirective,
    EmployeeForm,
  ],
  providers: [ConfirmationService],
  templateUrl: './employee-list.html',
  styleUrl: './employee-list.scss'
})
export class EmployeeList {
  private readonly api = inject(EmployeesApi);
  private readonly destroyRef = inject(DestroyRef);
  private readonly confirmation = inject(ConfirmationService);
  private readonly router = inject(Router);

  // Data state
  readonly employees = signal<Employee[]>([]);
  readonly totalRecords = signal(0);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly processingId = signal<number | null>(null);
  readonly availableUsers = signal<AvailableUser[]>([]);
  readonly loadingUsers = signal(false);

  // Search and pagination state
  readonly search = signal('');
  readonly page = signal(1);
  readonly pageSize = signal(20);

  readonly first = computed(
    () => (this.page() - 1) * this.pageSize()
  );

  // Dialog state
  readonly dialogOpen = signal(false);
  readonly selected = signal<Employee | null>(null);

  readonly statusDialogOpen = signal(false);
  readonly statusEmployee = signal<Employee | null>(null);
  readonly statusValue = signal<EmploymentStatus>('ACTIVE');

  // Feedback
  readonly error = signal('');
  readonly formError = signal('');
  readonly notice = signal('');

  // Cancel outdated list requests
  private readonly loadRequests = new Subject<void>();

  // Table
  readonly columns: TableColumn<Employee>[] = [
    {
      field: 'employeeCode',
      header: 'Employee Code',
      sortable: false
    },
    {
      field: 'firstName',
      header: 'Employee',
      type: 'custom'
    },
    {
      field: 'workEmail',
      header: 'Work Email'
    },
    {
      field: 'employmentType',
      header: 'Employment Type',
      type: 'custom'
    },
    {
      field: 'employmentStatus',
      header: 'Status',
      type: 'status'
    },
    {
      field: 'hireDate',
      header: 'Hire Date',
      type: 'date'
    }
  ];

  readonly tableConfig = computed<DataTableConfig>(() => ({
    paginator: true,
    rows: this.pageSize(),
    rowsPerPageOptions: [10, 20, 50, 100],
    searchMode: 'server',
    serverSide: true,
    searchPlaceholder: 'Search employees...',
    emptyMessage: 'No employees found',
    stripedRows: false,
    showGridlines: false
  }));

  readonly rowActions: TableAction<Employee>[] = [
    {
      id: 'edit',
      label: 'Edit employee',
      icon: 'pi pi-pencil',
      severity: 'secondary'
    },
    {
      id: 'shifts',
      label: 'Shift assignments',
      icon: 'pi pi-calendar-clock',
      severity: 'secondary'
    },
    {
      id: 'status',
      label: 'Change employment status',
      icon: 'pi pi-user-edit',
      severity: 'secondary',
      disabled: () => this.processingId() !== null,
      loading: (row: any) =>
        this.processingId() === row.employeeId
    },
    {
      id: 'attendance',
      label: 'Attendance',
      icon: 'pi pi-calendar',
      severity: 'secondary'
    }
  ];

  readonly statusOptions = [
    { label: 'Active', value: 'ACTIVE' },
    { label: 'On Leave', value: 'ON_LEAVE' },
    { label: 'Terminated', value: 'TERMINATED' }
  ];

  constructor() {
    this.loadRequests
      .pipe(
        debounceTime(250),
        tap(() => {
          this.loading.set(true);
          this.error.set('');
        }),
        switchMap(() =>
          this.api.list({
            search: this.search() || undefined,
            page: this.page(),
            pageSize: this.pageSize()
          }).pipe(
            tap((response: EmployeePage) => {
              this.employees.set(response.items);
              this.totalRecords.set(response.total);
            }),
            catchError((err: ApiError) => {
              this.error.set(this.errorText(err));
              return EMPTY;
            }),
            finalize(() => this.loading.set(false))
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe();

    this.load();
  }

  // ----------------------------------------
  // Data loading
  // ----------------------------------------

  load(): void {
    this.loadRequests.next();
  }

  onTableSearch(value: string): void {
    this.search.set(value.trim());
    this.page.set(1);
    this.load();
  }

  onTablePage(event: DataTablePageEvent): void {
    this.page.set(event.page);
    this.pageSize.set(event.pageSize);
    this.load();
  }

  // ----------------------------------------
  // Table actions
  // ----------------------------------------

  onTableAction(event: TableActionEvent<Employee>): void {
    switch (event.action) {
      case 'edit':
        this.openEdit(event.row);
        break;

      case 'status':
        this.openStatus(event.row);
        break;

      case 'shifts':
        void this.router.navigate([
          '/employees',
          event.row.employeeId,
          'shifts'
        ]);
        break;
      case 'attendance':
        void this.router.navigate([
          '/employees',
          event.row.employeeId,
          'attendance'
        ]);
        break;
    }
  }

  // ----------------------------------------
  // Form dialog
  // ----------------------------------------


  openCreate(): void {
    this.selected.set(null);
    this.formError.set('');
    this.availableUsers.set([]);
    this.dialogOpen.set(true);

    this.loadingUsers.set(true);

    this.api.getAvailableUsers()
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loadingUsers.set(false))
      )
      .subscribe({
        next: (users) => {
          this.availableUsers.set(users);
        },
        error: (err: ApiError) => {
          this.formError.set(
            this.errorText(err)
          );
        }
      });
  }


  openEdit(employee: Employee): void {
    this.selected.set(employee);
    this.formError.set('');
    this.dialogOpen.set(true);
  }

  save(data: EmployeeFormValue): void {
    if (this.saving()) return;

    const existing = this.selected();
    this.formError.set('');
    this.saving.set(true);

    const request = existing
      ? this.api.update(
        existing.employeeId,
        data as UpdateEmployeeRequest
      )
      : this.api.create(
        data as CreateEmployeeRequest
      );

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
              ? 'Employee updated successfully.'
              : 'Employee created successfully.'
          );

          if (!existing) {
            this.page.set(1);
          }

          this.load();
        },
        error: (err: ApiError) => {
          this.formError.set(this.errorText(err));
        }
      });
  }

  // ----------------------------------------
  // Employment status
  // ----------------------------------------

  openStatus(employee: Employee): void {
    this.statusEmployee.set(employee);
    this.statusValue.set(employee.employmentStatus);
    this.statusDialogOpen.set(true);
  }

  confirmStatus(): void {
    const employee = this.statusEmployee();
    const status = this.statusValue();

    if (!employee || this.processingId() !== null) return;

    if (status === employee.employmentStatus) {
      this.statusDialogOpen.set(false);
      return;
    }

    this.confirmation.confirm({
      header: 'Change employment status',
      message: `Change ${employee.firstName} ${employee.lastName}'s status to ${status.replace('_', ' ')}?`,
      icon: 'pi pi-exclamation-circle',
      acceptLabel: 'Confirm',
      rejectLabel: 'Cancel',
      accept: () => this.updateStatus(employee, status)
    });
  }

  private updateStatus(
    employee: Employee,
    status: EmploymentStatus
  ): void {
    this.processingId.set(employee.employeeId);
    this.error.set('');

    this.api.setStatus(employee.employeeId, status)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.processingId.set(null))
      )
      .subscribe({
        next: () => {
          this.statusDialogOpen.set(false);
          this.notice.set('Employment status updated.');
          this.load();
        },
        error: (err: ApiError) => {
          this.error.set(this.errorText(err));
        }
      });
  }

  // ----------------------------------------
  // Helpers
  // ----------------------------------------

  typeLabel(value: string): string {
    return value.charAt(0) +
      value.slice(1).toLowerCase();
  }

  private errorText(err: ApiError): string {
    const issues = err?.validationErrors &&
      Object.values(err.validationErrors).flat();

    return issues?.length
      ? issues.join(' ')
      : err?.message || 'The request failed.';
  }
}
