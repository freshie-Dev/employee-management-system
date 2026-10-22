import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AuthStore } from '../../../../core/auth/auth-store';
@Component({
  selector: 'app-dashboard', imports: [RouterLink, ButtonModule],
  templateUrl: './dashboard.html', styleUrl: './dashboard.scss'
})
export class Dashboard { readonly auth = inject(AuthStore); }
