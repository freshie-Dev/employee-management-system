
import { DOCUMENT, isPlatformBrowser } from '@angular/common';
import {
  Injectable,
  PLATFORM_ID,
  effect,
  inject,
  signal
} from '@angular/core';

import {
  palette,
  updatePrimaryPalette
} from '@primeuix/themes';

import {
  DEFAULT_THEME,
  EmsTheme
} from './theme.model';

@Injectable({
  providedIn: 'root'
})
export class Theme {

  private readonly storageKey = 'ems-theme';

  private readonly document = inject(DOCUMENT);

  private readonly browser =
    isPlatformBrowser(inject(PLATFORM_ID));

  readonly theme = signal<EmsTheme>(this.loadTheme());

  constructor() {

    effect(() => {
      const current = this.theme();

      if (!this.browser) return;

      // Update our application CSS variables.
      const root = this.document.documentElement;

      (Object.keys(current) as (keyof EmsTheme)[])
        .forEach(key => {
          root.style.setProperty(
            `--app-${key}`,
            current[key]
          );
        });

      // Update PrimeNG's primary color palette.
      // Other semantic tokens already reference --app-*.
      updatePrimaryPalette(palette(current.primary));

      // Persist user preferences.
      try {
        localStorage.setItem(
          this.storageKey,
          JSON.stringify(current)
        );
      } catch {
        // Storage may be unavailable.
      }
    });

  }

  updateColor(
    key: keyof EmsTheme,
    value: string
  ): void {

    if (!/^#[0-9a-fA-F]{6}$/.test(value)) return;

    this.theme.update(current => ({
      ...current,
      [key]: value
    }));
  }

  resetTheme(): void {
    this.theme.set({ ...DEFAULT_THEME });
  }

  private loadTheme(): EmsTheme {

    if (!this.browser) {
      return { ...DEFAULT_THEME };
    }

    try {

      const saved = localStorage.getItem(
        this.storageKey
      );

      if (!saved) {
        return { ...DEFAULT_THEME };
      }

      const parsed: unknown = JSON.parse(saved);

      if (!parsed || typeof parsed !== 'object') {
        return { ...DEFAULT_THEME };
      }

      const values = parsed as Record<string, unknown>;
      const result = { ...DEFAULT_THEME };

      for (
        const key of Object.keys(result) as (keyof EmsTheme)[]
      ) {
        const value = values[key];

        if (
          typeof value === 'string' &&
          /^#[0-9a-fA-F]{6}$/.test(value)
        ) {
          result[key] = value;
        }
      }

      return result;

    } catch {
      return { ...DEFAULT_THEME };
    }
  }

}
