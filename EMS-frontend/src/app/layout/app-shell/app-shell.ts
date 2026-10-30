import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { Auth } from '../../core/auth/auth';
import { AuthStore } from '../../core/auth/auth-store';
import { SignOut } from '@primeicons/angular/sign-out';
import { Palette } from '@primeicons/angular/palette';
import { TooltipModule } from 'primeng/tooltip';


@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ButtonModule, SignOut, Palette, TooltipModule],
  templateUrl: './app-shell.html', styleUrl: './app-shell.scss'
})
export class AppShell {
  readonly auth = inject(Auth);
  readonly store = inject(AuthStore);
}
