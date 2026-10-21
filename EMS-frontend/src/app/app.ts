import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Theme } from './core/theme/theme';

@Component({
  imports: [RouterOutlet],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly title = signal('EMS-frontend');
  private readonly _themeService = inject(Theme);
}
