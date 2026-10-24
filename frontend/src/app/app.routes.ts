import { Routes } from '@angular/router';
import { LAYOUT_STRINGS } from './core/layout/layout.strings';
import { MainLayout } from './core/layout/main-layout';
import { PRODUCTS_STRINGS } from './features/products/products.strings';

export const routes: Routes = [
  {
    path: '',
    component: MainLayout,
    children: [
      {
        path: '',
        pathMatch: 'full',
        data: { breadcrumb: LAYOUT_STRINGS.home },
        loadComponent: () => import('./features/home/home').then((m) => m.Home),
      },
      {
        path: 'products',
        data: { breadcrumb: PRODUCTS_STRINGS.breadcrumb },
        loadChildren: () =>
          import('./features/products/products.routes').then((m) => m.PRODUCTS_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
