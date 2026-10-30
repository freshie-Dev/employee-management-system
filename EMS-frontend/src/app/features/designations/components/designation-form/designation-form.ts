
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
import { TextareaModule } from 'primeng/textarea';
import { ButtonModule } from 'primeng/button';

import {
  Designation,
  DesignationRequest
} from '../../models/designation.model';

@Component({
  selector: 'app-designation-form',
  imports: [
    ReactiveFormsModule,
    InputTextModule,
    TextareaModule,
    ButtonModule
  ],
  templateUrl: './designation-form.html',
  styleUrl: './designation-form.scss'
})
export class DesignationForm {
  private readonly fb = inject(FormBuilder);

  readonly designation = input<Designation | null>(null);
  readonly saving = input(false);

  readonly save = output<DesignationRequest>();
  readonly cancel = output<void>();

  readonly form = this.fb.group({
    title: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(100)
    ]),

    code: this.fb.nonNullable.control('', [
      Validators.required,
      Validators.maxLength(20)
    ]),

    description: this.fb.nonNullable.control('')
  });

  constructor() {
    effect(() => {
      const item = this.designation();

      this.form.reset({
        title: item?.title ?? '',
        code: item?.code ?? '',
        description: item?.description ?? ''
      });
    });
  }

  submit(): void {
    if (this.saving()) return;

    const raw = this.form.getRawValue();

    const title = raw.title.trim();
    const code = raw.code.trim();
    const description = raw.description.trim();

    if (
      this.form.invalid ||
      !title ||
      !code
    ) {
      this.form.markAllAsTouched();
      return;
    }

    this.save.emit({
      title,
      code,
      description: description || null
    });
  }
}
