
import { definePreset, palette } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';

import { DEFAULT_THEME } from './theme.model';

export const EmsPreset = definePreset(Aura, {

  semantic: {

    // Primary palette is managed at runtime by Theme service.
    primary: palette(DEFAULT_THEME.primary),

    // Shared focus styling.
    focusRing: {
      width: '2px',
      style: 'solid',
      color: '{primary.500}',
      offset: '2px'
    },

    colorScheme: {
      light: {

        // PrimeNG primary action colors.
        primary: {
          color: '{primary.500}',
          contrastColor: '#FFFFFF',
          hoverColor: '{primary.600}',
          activeColor: '{primary.700}'
        },

        // General text colors.
        text: {
          color: 'var(--app-text)',
          hoverColor: 'var(--app-heading)',
          mutedColor: 'var(--app-text)'
        },

        // Containers, cards and content areas.
        content: {
          background: 'var(--app-surface)',
          color: 'var(--app-text)',
          hoverBackground: 'var(--app-background)'
        },

        // Form controls.
        formField: {
          background: 'var(--app-surface)',
          color: 'var(--app-heading)',
          borderColor: 'var(--app-border)',
          hoverBorderColor: '{primary.400}',
          focusBorderColor: '{primary.500}',
          placeholderColor: 'var(--app-text)'
        }
      }
    }
  },

  components: {
    button: {
      root: {
        borderRadius: 'var(--app-button-radius)'
      }
    }
  }

});
