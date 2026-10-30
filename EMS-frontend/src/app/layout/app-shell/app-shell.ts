import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { Auth } from '../../core/auth/auth';
import { AuthStore } from '../../core/auth/auth-store';
import { SignOut } from '@primeicons/angular/sign-out';


@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ButtonModule, SignOut],
  templateUrl: './app-shell.html', styleUrl: './app-shell.scss'
})
export class AppShell { readonly auth = inject(Auth); readonly store = inject(AuthStore); }
