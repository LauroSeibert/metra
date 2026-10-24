import { Routes } from '@angular/router';
import { FinancialEntryKind } from '../../api/models/financial-entry-kind';
import { provideMaterialFormDefaults } from '../../shared/ui/material-defaults';
import { FINANCE_STRINGS } from './finance.strings';

/** Lista, cadastro e edição para um tipo (a receber ou a pagar), sob /finance/receivables ou /payables. */
function kindRoutes(kind: FinancialEntryKind): Routes {
  return [
    {
      path: '',
      pathMatch: 'full',
      data: { kind },
      loadComponent: () => import('./entry-list/entry-list').then((m) => m.EntryList),
    },
    {
      path: 'edit',
      pathMatch: 'full',
      data: { kind, breadcrumb: FINANCE_STRINGS.form.newTitle },
      loadComponent: () => import('./entry-form/entry-form').then((m) => m.EntryForm),
    },
    {
      path: 'edit/:id',
      data: { kind, breadcrumb: FINANCE_STRINGS.form.editTitle },
      loadComponent: () => import('./entry-form/entry-form').then((m) => m.EntryForm),
    },
  ];
}

/** Rotas do financeiro, carregadas sob demanda a partir de app.routes.ts. */
export const FINANCE_ROUTES: Routes = [
  {
    path: '',
    providers: [provideMaterialFormDefaults()],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'receivables' },
      {
        path: 'receivables',
        data: { breadcrumb: FINANCE_STRINGS.kinds.Receivable.breadcrumb },
        children: kindRoutes('Receivable'),
      },
      {
        path: 'payables',
        data: { breadcrumb: FINANCE_STRINGS.kinds.Payable.breadcrumb },
        children: kindRoutes('Payable'),
      },
      {
        path: 'categories',
        data: { breadcrumb: FINANCE_STRINGS.categories.breadcrumb },
        loadComponent: () =>
          import('./categories/financial-category-list').then((m) => m.FinancialCategoryList),
      },
    ],
  },
];
