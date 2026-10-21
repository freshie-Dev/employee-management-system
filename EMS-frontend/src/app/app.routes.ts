import { Routes } from '@angular/router';

export const routes: Routes = [
    {
        path: 'settings/appearance',
        loadComponent: () =>
            import('./features/settings/appearance/appearance')
                .then(c => c.Appearance)
    }
];
