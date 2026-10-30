
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  input,
  output,
  signal
} from '@angular/core';

import {
  form,
  FormField,
  required,
  minLength,
  maxLength,
  submit
} from '@angular/forms/signals';

import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';

import { Check } from '@primeicons/angular/check';
import { Spinner } from '@primeicons/angular/spinner';

import { InputPasswordModule } from 'primeng/inputpassword';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';

import { Lock } from '@primeicons/angular/lock';
import { Eye } from '@primeicons/angular/eye';
import { EyeSlash } from '@primeicons/angular/eye-slash';
import { ChipModule } from 'primeng/chip';
import { Times } from '@primeicons/angular/times';
import { pattern } from '@angular/forms/signals';

import {
  User,
  Role,
  UserFormMode,
  CreateUserRequest,
  UpdateUserRequest,
  UpdateUserRolesRequest
} from '../../models/user.model';
import { SelectModule } from 'primeng/select';

export type UserFormValue =
  | CreateUserRequest
  | UpdateUserRequest
  | UpdateUserRolesRequest;

interface UserFormModel {
  username: string;
  password: string;
  roleIds: number[];
}

@Component({
  selector: 'app-user-form',
  imports: [
    FormField,
    InputTextModule,
    InputPasswordModule,
    IconFieldModule,
    InputIconModule,
    SelectModule,
    ButtonModule,
    Lock,
    Eye,
    EyeSlash,
    Check,
    Spinner,
    ChipModule,
    Times,
  ],
  templateUrl: './user-form.html',
  styleUrl: './user-form.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserForm {
  readonly mode = input<UserFormMode>('create');
  readonly user = input<User | null>(null);
  readonly roles = input<Role[]>([]);
  readonly saving = input(false);

  readonly save = output<UserFormValue>();
  readonly cancel = output<void>();

  readonly model = signal<UserFormModel>({
    username: '',
    password: '',
    roleIds: []
  });

  readonly passwordRules = [
    {
      label: '12+ characters',
      test: (value: string) => value.length >= 12
    },
    {
      label: 'Number',
      test: (value: string) => /\d/.test(value)
    },
    {
      label: 'Uppercase letter',
      test: (value: string) => /[A-Z]/.test(value)
    },
    {
      label: 'Special character',
      test: (value: string) => /[^a-zA-Z0-9]/.test(value)
    }
  ];

  readonly passwordValue = computed(() => this.model().password);

  readonly passwordMasked = signal(true);

  readonly userForm = form(this.model, path => {
    required(path.username, {
      message: 'Username is required.'
    });

    minLength(path.username, 3, {
      message: 'Username must contain at least 3 characters.'
    });

    maxLength(path.username, 100);

    required(path.password, {
      when: () => this.mode() === 'create',
      message: 'Password is required.'
    });

    minLength(path.password, 12, {
      when: () => this.mode() === 'create',
      message: 'Password must contain at least 12 characters.'
    });

    minLength(path.roleIds, 1, {
      when: () => this.mode() !== 'edit',
      message: 'Select at least one role.'
    });

    pattern(
      path.password,
      /^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{12,}$/,
      {
        when: () => this.mode() === 'create',
        message:
          'Password must include an uppercase letter, a number and a special character.'
      }
    );
  });

  readonly title = () => {
    switch (this.mode()) {
      case 'create': return 'Create User';
      case 'edit': return 'Save Changes';
      case 'roles': return 'Update Roles';
    }
  };

  constructor() {
    effect(() => {
      const user = this.user();
      const mode = this.mode();
      const roles = this.roles();

      this.model.set({
        username: user?.username ?? '',
        password: '',
        roleIds: user
          ? roles
            .filter(role => user.roles.includes(role.name))
            .map(role => role.roleId)
          : []
      });
    });
  }

  async onSubmit(event: Event): Promise<void> {
    event.preventDefault();

    if (this.saving()) return;

    await submit(this.userForm, async () => {
      const data = this.model();

      if (this.mode() === 'create') {
        const request: CreateUserRequest = {
          username: data.username.trim(),
          password: data.password,
          roleIds: [...data.roleIds]
        };

        this.save.emit(request);
        return;
      }

      if (this.mode() === 'edit') {
        const request: UpdateUserRequest = {
          username: data.username.trim()
        };

        this.save.emit(request);
        return;
      }

      const request: UpdateUserRolesRequest = {
        roleIds: [...data.roleIds]
      };

      this.save.emit(request);
    });
  }

  fieldError(
    field: 'username' | 'password' | 'roleIds'
  ): string | null {
    const state = this.userForm[field]();

    if (!state.touched() || !state.invalid()) {
      return null;
    }

    return state.errors()[0]?.message ?? 'Invalid value.';
  }
}
