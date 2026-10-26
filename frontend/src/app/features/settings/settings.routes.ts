import { Routes } from '@angular/router';

/** /settings, carregada sob demanda a partir de app.routes.ts. */
export const SETTINGS_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./settings').then((m) => m.Settings),
  },
];
