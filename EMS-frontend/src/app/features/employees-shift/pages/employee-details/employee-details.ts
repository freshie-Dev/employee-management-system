
import {
  Component,
  DestroyRef,
  inject,
  signal
} from '@angular/core';

import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MessageModule } from 'primeng/message';

import { EmployeesApi } from '../../../employees/data-access/employees-api';
import { Employee } from '../../../employees/models/employee.model';

import { EmployeeShiftAssignments } from '../../components/employee-shift-assignments/employee-shift-assignments';

import { EmployeeScheduleOverrides } from '../../../employees-schedule/components/employee-schedule-overrides/employee-schedule-overrides';

import { ApiError } from '../../../../core/models/api.models';

@Component({
  selector: 'app-employee-details',
  imports: [
    RouterLink,
    MessageModule,
    EmployeeShiftAssignments,
    EmployeeScheduleOverrides
  ],
  templateUrl: './employee-details.html',
  styleUrl: './employee-details.scss'
})
export class EmployeeDetails {
  private readonly route = inject(ActivatedRoute);
  private readonly api = inject(EmployeesApi);
  private readonly destroyRef = inject(DestroyRef);

  readonly employeeId = Number(
    this.route.snapshot.paramMap.get('id')
  );

  readonly employee = signal<Employee | null>(null);
  readonly loading = signal(true);
  readonly error = signal('');

  constructor() {
    if (
      !Number.isInteger(this.employeeId) ||
      this.employeeId <= 0
    ) {
      this.error.set('Invalid employee ID.');
      this.loading.set(false);
      return;
    }

    this.api.get(this.employeeId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: employee => {
          this.employee.set(employee);
          this.loading.set(false);
        },
        error: (err: ApiError) => {
          this.error.set(
            err.message || 'Could not load employee.'
          );
          this.loading.set(false);
        }
      });
  }
}
