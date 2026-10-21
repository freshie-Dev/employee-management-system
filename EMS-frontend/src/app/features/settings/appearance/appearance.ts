import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonDirective, ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { CardModule } from 'primeng/card';
import { EmsTheme } from '../../../core/theme/theme.model';
import { Theme } from '../../../core/theme/theme';

@Component({
  selector: 'app-appearance',
  imports: [FormsModule, ButtonModule, InputTextModule, CardModule, ButtonDirective],
  templateUrl: './appearance.html',
  styleUrl: './appearance.scss'
})
export class Appearance {
  readonly themeService = inject(Theme);
  readonly colorOptions: { key: keyof EmsTheme; label: string; description: string }[] = [
    { key: 'primary', label: 'Primary', description: 'Buttons, links, focus and selection' },
    { key: 'heading', label: 'Heading', description: 'Titles and prominent text' },
    { key: 'text', label: 'Text', description: 'Body and secondary text' },
    { key: 'background', label: 'Background', description: 'Workspace background' },
    { key: 'surface', label: 'Surface', description: 'Cards, panels and inputs' },
    { key: 'accent', label: 'Accent', description: 'Subtle highlights and editorial details' }
  ];
}
