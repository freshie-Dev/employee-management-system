
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal
} from '@angular/core';

import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, forkJoin, Observable } from 'rxjs';

import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ConfirmationService } from 'primeng/api';

import { Plus } from '@primeicons/angular/plus';

import { AuthStore } from '../../../../core/auth/auth-store';
import { ApiError } from '../../../../core/models/api.models';

import { DataTable } from
  '../../../../core/shared/components/data-table/data-table';

import { DataTableColumnDirective } from
  '../../../../core/shared/components/data-table/data-table-column.directive';

import {
  DataTableConfig,
  TableAction,
  TableActionEvent,
  TableColumn
} from '../../../../core/shared/components/data-table/data-table.model';

import { UsersApi } from '../../data-access/users-api';

import {
  User,
  Role,
  UserFormMode,
  CreateUserRequest,
  UpdateUserRequest,
  UpdateUserRolesRequest
} from '../../models/user.model';

import {
  UserForm,
  UserFormValue
} from '../../components/user-form/user-form';

@Component({
  selector: 'app-user-list',
  imports: [
    ButtonModule,
    DialogModule,
    MessageModule,
    TagModule,
    ConfirmDialogModule,
    Plus,
    DataTable,
    DataTableColumnDirective,
    UserForm
  ],
  providers: [ConfirmationService],
  templateUrl: './user-list.html',
  styleUrl: './user-list.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserList {
  private readonly api = inject(UsersApi);
  private readonly authStore = inject(AuthStore);
  private readonly destroyRef = inject(DestroyRef);
  private readonly confirmation = inject(ConfirmationService);

  readonly users = signal<User[]>([]);
  readonly roles = signal<Role[]>([]);

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly processingId = signal<number | null>(null);

  readonly error = signal('');
  readonly formError = signal('');
  readonly notice = signal('');

  readonly dialogOpen = signal(false);
  readonly selected = signal<User | null>(null);
  readonly formMode = signal<UserFormMode>('create');

  readonly totalRecords = computed(() => this.users().length);

  readonly columns: TableColumn<User>[] = [
    {
      field: 'username',
      header: 'Username',
      type: 'custom'
    },
    {
      field: 'roles',
      header: 'Roles',
      type: 'custom'
    },
    {
      field: 'employeeId',
      header: 'Employee',
      type: 'custom'
    },
    {
      field: 'isActive',
      header: 'Status',
      type: 'status',
      trueLabel: 'Active',
      falseLabel: 'Inactive'
    },
    {
      field: 'createdAt',
      header: 'Created On',
      type: 'date'
    }
  ];

  readonly tableConfig: DataTableConfig = {
    paginator: true,
    rows: 10,
    rowsPerPageOptions: [10, 20, 50],
    searchMode: 'local',
    searchPlaceholder: 'Search users...',
    emptyMessage: 'No users found',
    stripedRows: false,
    showGridlines: false
  };

  readonly rowActions: TableAction<User>[] = [
    {
      id: 'edit',
      label: 'Edit username',
      icon: 'pi pi-pencil',
      severity: 'secondary'
    },
    {
      id: 'roles',
      label: 'Manage roles',
      icon: 'pi pi-shield',
      severity: 'secondary',
      disabled: row => this.isSelf(row)
    },
    {
      id: 'status',
      label: 'Activate / Deactivate',
      icon: 'pi pi-power-off',
      severity: 'secondary',
      disabled: row =>
        this.processingId() !== null ||
        (row.isActive && this.isSelf(row))
    }
  ];

  constructor() {
    this.load();
  }

  // -----------------------------------
  // API loading
  // -----------------------------------

  load(): void {
    this.loading.set(true);
    this.error.set('');

    forkJoin({
      users: this.api.list(),
      roles: this.api.listRoles()
    })
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.loading.set(false))
      )
      .subscribe({
        next: ({ users, roles }) => {
          this.users.set(users);
          this.roles.set(roles);
        },
        error: (err: ApiError) => {
          this.error.set(this.errorText(err));
        }
      });
  }

  // -----------------------------------
  // Table actions
  // -----------------------------------

  onTableAction(event: TableActionEvent<User>): void {
    switch (event.action) {
      case 'edit':
        this.openEdit(event.row);
        break;

      case 'roles':
        this.openRoles(event.row);
        break;

      case 'status':
        this.confirmStatus(event.row);
        break;
    }
  }

  // -----------------------------------
  // Dialogs
  // -----------------------------------

  openCreate(): void {
    this.selected.set(null);
    this.formMode.set('create');
    this.formError.set('');
    this.dialogOpen.set(true);
  }

  openEdit(user: User): void {
    this.selected.set(user);
    this.formMode.set('edit');
    this.formError.set('');
    this.dialogOpen.set(true);
  }

  openRoles(user: User): void {
    if (this.isSelf(user)) return;

    this.selected.set(user);
    this.formMode.set('roles');
    this.formError.set('');
    this.dialogOpen.set(true);
  }

  readonly dialogTitle = computed(() => {
    switch (this.formMode()) {
      case 'create': return 'New User';
      case 'edit': return 'Edit User';
      case 'roles': return 'Manage User Roles';
    }
  });

  // -----------------------------------
  // Create / Update
  // -----------------------------------

  save(data: UserFormValue): void {
    if (this.saving()) return;

    const mode = this.formMode();
    const user = this.selected();

    if (mode !== 'create' && !user) return;

    this.saving.set(true);
    this.formError.set('');

    const request$: Observable<unknown> = mode === 'create'
      ? this.api.create(data as CreateUserRequest)
      : mode === 'edit'
        ? this.api.update(
          user!.userId,
          data as UpdateUserRequest
        )
        : this.api.updateRoles(
          user!.userId,
          (data as UpdateUserRolesRequest).roleIds
        );
    request$
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false))
      )
      .subscribe({
        next: () => {
          this.dialogOpen.set(false);

          this.notice.set(
            mode === 'create'
              ? 'User created successfully.'
              : mode === 'edit'
                ? 'User updated successfully.'
                : 'User roles updated successfully.'
          );

          this.load();
        },
        error: (err: ApiError) => {
          this.formError.set(this.errorText(err));
        }
      });
  }

  // -----------------------------------
  // Account status
  // -----------------------------------

  confirmStatus(user: User): void {
    if (this.processingId() !== null) return;
    if (user.isActive && this.isSelf(user)) return;

    const nextStatus = !user.isActive;

    this.confirmation.confirm({
      header: nextStatus
        ? 'Activate User'
        : 'Deactivate User',
      message: nextStatus
        ? `Activate ${user.username}'s account?`
        : `Deactivate ${user.username}'s account? They will no longer be able to log in.`,
      acceptLabel: nextStatus ? 'Activate' : 'Deactivate',
      rejectLabel: 'Cancel',
      accept: () => this.updateStatus(user, nextStatus)
    });
  }

  private updateStatus(
    user: User,
    isActive: boolean
  ): void {
    this.processingId.set(user.userId);
    this.error.set('');

    this.api.setStatus(user.userId, isActive)
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.processingId.set(null))
      )
      .subscribe({
        next: () => {
          this.notice.set(
            isActive
              ? 'User activated successfully.'
              : 'User deactivated successfully.'
          );

          this.load();
        },
        error: (err: ApiError) => {
          this.error.set(this.errorText(err));
        }
      });
  }

  // -----------------------------------
  // Helpers
  // -----------------------------------

  isSelf(user: User): boolean {
    return this.authStore.session()?.userId === user.userId;
  }

  private errorText(err: ApiError): string {
    const issues = err?.validationErrors
      ? Object.values(err.validationErrors).flat()
      : [];

    return issues.length
      ? issues.join(' ')
      : err?.message || 'The request failed.';
  }
}
