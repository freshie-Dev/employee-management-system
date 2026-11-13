import { Component, effect, inject, input, output } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { Shift, CreateShiftRequest } from '../../models/shift.model';

@Component({
  selector: 'app-shift-form',
  imports: [ReactiveFormsModule, InputTextModule, InputNumberModule, ButtonModule],
  templateUrl: './shift-form.html',
  styleUrl: './shift-form.scss'
})
export class ShiftForm {
  private readonly fb = inject(FormBuilder);
  readonly shift = input<Shift | null>(null);
  readonly saving = input(false);
  readonly save = output<CreateShiftRequest>();
  readonly cancel = output<void>();

  readonly form = this.fb.group({
    name: this.fb.nonNullable.control('', [Validators.required, Validators.maxLength(100)]),
    startTime: this.fb.nonNullable.control('09:00', Validators.required),
    endTime: this.fb.nonNullable.control('17:00', Validators.required),
    breakMinutes: this.fb.nonNullable.control(0, [Validators.required, Validators.min(0)]),
    graceInMinutes: this.fb.nonNullable.control(0, [Validators.required, Validators.min(0), Validators.max(240)]),
    graceOutMinutes: this.fb.nonNullable.control(0, [Validators.required, Validators.min(0), Validators.max(240)]),
    requiredMinutes: this.fb.control<number | null>(null, [Validators.min(0)])
  });

  readonly durationMinutes = () => {
    const v = this.form.getRawValue();
    const parse = (s: string) => { const [h, m] = s.split(':').map(Number); return h * 60 + m; };
    const start = parse(v.startTime);
    const end = parse(v.endTime);
    if (!Number.isFinite(start) || !Number.isFinite(end)) return 0;
    return (end - start + 1440) % 1440;
  };

  constructor() {
    effect(() => {
      const s = this.shift();
      this.form.reset({
        name: s?.name ?? '',
        startTime: s?.startTime?.slice(0, 5) ?? '09:00',
        endTime: s?.endTime?.slice(0, 5) ?? '17:00',
        breakMinutes: s?.breakMinutes ?? 0,
        graceInMinutes: s?.graceInMinutes ?? 0,
        graceOutMinutes: s?.graceOutMinutes ?? 0,
        requiredMinutes: s?.configuredRequiredMinutes ?? null
      });
    });
  }

  submit(): void {
    if (this.saving()) return;
    const v = this.form.getRawValue();
    const name = v.name.trim();
    const duration = this.durationMinutes();
    if (this.form.invalid || !name || duration === 0 || v.breakMinutes >= duration ||
      (v.requiredMinutes !== null && v.requiredMinutes > duration - v.breakMinutes)) {
      this.form.markAllAsTouched();
      return;
    }
    this.save.emit({
      name,
      startTime: `${v.startTime}:00`,
      endTime: `${v.endTime}:00`,
      breakMinutes: v.breakMinutes,
      graceInMinutes: v.graceInMinutes,
      graceOutMinutes: v.graceOutMinutes,
      requiredMinutes: v.requiredMinutes
    });
  }
}
