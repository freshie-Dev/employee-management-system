import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize, TimeoutError } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { Auth } from '../../../core/auth/auth';
import { ApiError } from '../../../core/models/api.models';

import { InputPasswordModule } from 'primeng/inputpassword';
import { ButtonDirective } from 'primeng/button';
import { MessageModule } from 'primeng/message';
@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, ButtonModule, InputTextModule, InputPasswordModule, InputPasswordModule, ButtonDirective, MessageModule],
  templateUrl: './login.html', styleUrl: './login.scss'
})
export class Login {
  private readonly auth = inject(Auth);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  readonly showPassword = signal(false);
  readonly busy = signal(false);
  readonly error = signal('');
  readonly expired = this.route.snapshot.queryParamMap.get('reason') === 'expired';
  readonly form = inject(FormBuilder).nonNullable.group({
    username: ['', [Validators.required, Validators.pattern(/\S/)]],
    password: ['', Validators.required]
  });

  submit(): void {
    if (this.busy()) return;
    this.error.set('');
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const credentials = this.form.getRawValue();
    this.busy.set(true);
    this.auth.login({ ...credentials, username: credentials.username.trim() }).pipe(
      takeUntilDestroyed(this.destroyRef), finalize(() => this.busy.set(false))
    ).subscribe({
      next: () => {
        this.form.controls.password.reset();
        const url = this.route.snapshot.queryParamMap.get('returnUrl');
        const safe = url?.startsWith('/') && !url.startsWith('//')
          && !url.includes('\\') && !/^\/login(?:[/?#;]|$)/.test(url);
        void this.router.navigateByUrl(safe ? url! : '/dashboard', { replaceUrl: true });
      },
      error: (error: unknown) => {
        const failure = error as ApiError;
        this.error.set(failure?.status === 401 ? 'The username or password is incorrect.'
          : error instanceof TimeoutError ? 'The server took too long to respond. Please try again.'
          : failure?.status === 0 ? 'Cannot reach the server. Please try again.'
          : failure?.status === 400 ? 'Please check your username and password.'
          : 'Unable to sign in. Please try again.');
      }
    });
  }
}
