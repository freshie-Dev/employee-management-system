import {
  Component,
  computed,
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

import {
  Department,
  DepartmentRequest
} from '../../models/department.model';

@Component({
  selector: 'app-department-form',
  imports: [
    ReactiveFormsModule,
    InputTextModule,
    SelectModule,
    ButtonModule
  ],
  templateUrl: './department-form.html',
  styleUrl: './department-form.scss'
})
export class DepartmentForm {
  readonly department = input<Department | null>(null);
  readonly departments = input<Department[]>([]);
  readonly saving = input(false);

  readonly save = output<DepartmentRequest>();
  readonly cancel = output<void>();

  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.group({
    name: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(100)
    ]),
    code: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(20)
    ]),
    parentDepartmentId: this.fb.control<number | null>(null)
  });

  readonly parentOptions = computed(() =>
    this.departments()
      .filter(
        d =>
          d.departmentId !== this.department()?.departmentId &&
          d.isActive
      )
      .map(d => ({
        label: `${d.name} (${d.code})`,
        value: d.departmentId
      }))
  );

  constructor() {
    effect(() => {
      const item = this.department();

      this.form.reset({
        name: item?.name ?? '',
        code: item?.code ?? '',
        parentDepartmentId: item?.parentDepartmentId ?? null
      });
    });
  }

  submit(): void {
    if (this.saving()) return;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();

    this.save.emit({
      name: raw.name.trim(),
      code: raw.code.trim(),
      parentDepartmentId: raw.parentDepartmentId
    });
  }
}