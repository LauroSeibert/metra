import { Routes } from '@angular/router';
import { provideMaterialFormDefaults } from '../../shared/ui/material-defaults';
import { PARTNERS_STRINGS } from './partners.strings';

/** Rotas de parceiros, carregadas sob demanda a partir de app.routes.ts. */
export const PARTNERS_ROUTES: Routes = [
  {
    path: '',
    providers: [provideMaterialFormDefaults()],
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./partner-list/partner-list').then((m) => m.PartnerList),
      },
      // Cadastro em edit (sem id) e edição em edit/:id, como em produtos.
      {
        path: 'edit',
        pathMatch: 'full',
        data: { breadcrumb: PARTNERS_STRINGS.form.newTitle },
        loadComponent: () => import('./partner-form/partner-form').then((m) => m.PartnerForm),
      },
      {
        path: 'edit/:id',
        data: { breadcrumb: PARTNERS_STRINGS.form.editTitle },
        loadComponent: () => import('./partner-form/partner-form').then((m) => m.PartnerForm),
      },
    ],
  },
];
