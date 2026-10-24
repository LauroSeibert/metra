import { Routes } from '@angular/router';
import { provideMaterialFormDefaults } from '../../shared/ui/material-defaults';
import { SALES_STRINGS } from './sales.strings';

/** Rotas de vendas, carregadas sob demanda a partir de app.routes.ts. */
export const SALES_ROUTES: Routes = [
  {
    path: '',
    providers: [provideMaterialFormDefaults()],
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./sale-list/sale-list').then((m) => m.SaleList),
      },
      {
        path: 'edit',
        pathMatch: 'full',
        data: { breadcrumb: SALES_STRINGS.form.newTitle },
        loadComponent: () => import('./sale-form/sale-form').then((m) => m.SaleForm),
      },
      {
        path: 'edit/:id',
        data: { breadcrumb: SALES_STRINGS.form.editBreadcrumb },
        loadComponent: () => import('./sale-form/sale-form').then((m) => m.SaleForm),
      },
    ],
  },
];
