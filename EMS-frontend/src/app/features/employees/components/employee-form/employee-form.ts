
import {
  Component,
  effect,
  inject,
  input,
  output
} from '@angular/core';

import {
  FormBuilder,
  ReactiveFormsModule,
  Validators
} from '@angular/forms';

import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';

import { Check } from '@primeicons/angular/check';
import { Spinner } from '@primeicons/angular/spinner';

import {
  Employee,
  AvailableUser,
  EmploymentType,
  CreateEmployeeRequest,
  UpdateEmployeeRequest
} from '../../models/employee.model';

export type EmployeeFormValue =
  | CreateEmployeeRequest
  | UpdateEmployeeRequest;

@Component({
  selector: 'app-employee-form',
  imports: [
    ReactiveFormsModule,
    InputTextModule,
    SelectModule,
    ButtonModule,
    Check,
    Spinner
  ],
  templateUrl: './employee-form.html',
  styleUrl: './employee-form.scss'
})
export class EmployeeForm {
  private readonly fb = inject(FormBuilder);

  readonly employee = input<Employee | null>(null);
  readonly saving = input(false);

  // Accounts eligible for assignment
  readonly availableUsers = input<AvailableUser[]>([]);
  readonly loadingUsers = input(false);

  readonly save = output<EmployeeFormValue>();
  readonly cancel = output<void>();

  readonly employmentTypes = [
    { label: 'Permanent', value: 'PERMANENT' },
    { label: 'Contract', value: 'CONTRACT' },
    { label: 'Intern', value: 'INTERN' }
  ];

  readonly form = this.fb.group({
    userId: this.fb.control<number | null>(null),

    employeeCode: this.fb.nonNullable.control('', [
      Validators.required
    ]),

    firstName: this.fb.nonNullable.control('', [
      Validators.required
    ]),

    lastName: this.fb.nonNullable.control('', [
      Validators.required
    ]),

    workEmail: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.email
    ]),

    personalEmail: this.fb.nonNullable.control('', [
      Validators.email
    ]),

    phoneNumber: this.fb.nonNullable.control(''),

    dateOfBirth: this.fb.nonNullable.control(''),

    hireDate: this.fb.nonNullable.control('', [
      Validators.required
    ]),

    employmentType: this.fb.nonNullable.control<
      EmploymentType
    >('PERMANENT', [
      Validators.required
    ])
  });

  constructor() {
    effect(() => {
      const item = this.employee();

      this.form.reset({
        userId: item?.userId ?? null,
        employeeCode: item?.employeeCode ?? '',
        firstName: item?.firstName ?? '',
        lastName: item?.lastName ?? '',
        workEmail: item?.workEmail ?? '',
        personalEmail: item?.personalEmail ?? '',
        phoneNumber: item?.phoneNumber ?? '',
        dateOfBirth: item?.dateOfBirth?.slice(0, 10) ?? '',
        hireDate: item?.hireDate?.slice(0, 10) ?? '',
        employmentType: item?.employmentType ?? 'PERMANENT'
      });

      if (item) {
        this.form.controls.userId.clearValidators();
        this.form.controls.userId.disable({
          emitEvent: false
        });
      } else {
        this.form.controls.userId.setValidators([
          Validators.required
        ]);

        this.form.controls.userId.enable({
          emitEvent: false
        });
      }

      this.form.controls.userId.updateValueAndValidity({
        emitEvent: false
      });
    });
  }

  submit(): void {
    if (this.saving()) return;

    const raw = this.form.getRawValue();

    const requiredFields = [
      raw.firstName,
      raw.lastName,
      raw.workEmail,
      ...(
        this.employee()
          ? []
          : [raw.employeeCode, raw.hireDate]
      )
    ];

    if (
      this.form.invalid ||
      requiredFields.some(value => !value.trim())
    ) {
      this.form.markAllAsTouched();
      return;
    }

    const common: UpdateEmployeeRequest = {
      firstName: raw.firstName.trim(),
      lastName: raw.lastName.trim(),
      workEmail: raw.workEmail.trim(),
      personalEmail: raw.personalEmail.trim() || null,
      phoneNumber: raw.phoneNumber.trim() || null,
      dateOfBirth: raw.dateOfBirth || null,
      employmentType: raw.employmentType
    };

    // Update does not modify the assigned user
    if (this.employee()) {
      this.save.emit(common);
      return;
    }

    // User selection is mandatory for creation
    if (raw.userId === null || raw.userId <= 0) {
      this.form.controls.userId.markAsTouched();
      return;
    }

    this.save.emit({
      ...common,
      userId: raw.userId,
      employeeCode: raw.employeeCode.trim(),
      hireDate: raw.hireDate
    });
  }

  hasError(
    field: keyof typeof this.form.controls
  ): boolean {
    const control = this.form.controls[field];

    return control.invalid && control.touched;
  }
}
