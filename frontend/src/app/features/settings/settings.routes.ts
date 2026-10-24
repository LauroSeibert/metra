import { Routes } from '@angular/router';
import { provideMaterialFormDefaults } from '../../shared/ui/material-defaults';

/** /settings, carregada sob demanda a partir de app.routes.ts. */
export const SETTINGS_ROUTES: Routes = [
  {
    path: '',
    providers: [provideMaterialFormDefaults()],
    loadComponent: () => import('./settings').then((m) => m.Settings),
  },
];
