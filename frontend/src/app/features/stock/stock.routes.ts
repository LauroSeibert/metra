import { Routes } from '@angular/router';
import { provideMaterialFormDefaults } from '../../shared/ui/material-defaults';
import { STOCK_STRINGS } from './stock.strings';

/** Rotas de estoque, carregadas sob demanda a partir de app.routes.ts. */
export const STOCK_ROUTES: Routes = [
  {
    path: '',
    providers: [provideMaterialFormDefaults()],
    children: [
      {
        path: '',
        pathMatch: 'full',
        loadComponent: () => import('./balance-list/balance-list').then((m) => m.BalanceList),
      },
      {
        path: 'movements',
        data: { breadcrumb: STOCK_STRINGS.history.breadcrumb },
        loadComponent: () =>
          import('./movement-history/movement-history').then((m) => m.MovementHistory),
      },
    ],
  },
];
