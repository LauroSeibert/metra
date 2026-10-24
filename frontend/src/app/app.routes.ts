import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guards';
import { LAYOUT_STRINGS } from './core/layout/layout.strings';
import { MainLayout } from './core/layout/main-layout';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadChildren: () => import('./features/access/access.routes').then((m) => m.LOGIN_ROUTES),
  },
  {
    path: '',
    component: MainLayout,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        data: { breadcrumb: LAYOUT_STRINGS.home },
        loadComponent: () => import('./features/home/home').then((m) => m.Home),
      },
      {
        path: 'products',
        data: { breadcrumb: LAYOUT_STRINGS.breadcrumbs.products },
        loadChildren: () =>
          import('./features/products/products.routes').then((m) => m.PRODUCTS_ROUTES),
      },
      {
        path: 'partners',
        data: { breadcrumb: LAYOUT_STRINGS.breadcrumbs.partners },
        loadChildren: () =>
          import('./features/partners/partners.routes').then((m) => m.PARTNERS_ROUTES),
      },
      {
        path: 'stock',
        data: { breadcrumb: LAYOUT_STRINGS.breadcrumbs.stock },
        loadChildren: () => import('./features/stock/stock.routes').then((m) => m.STOCK_ROUTES),
      },
      {
        path: 'sales',
        data: { breadcrumb: LAYOUT_STRINGS.breadcrumbs.sales },
        loadChildren: () => import('./features/sales/sales.routes').then((m) => m.SALES_ROUTES),
      },
      {
        path: 'finance',
        data: { breadcrumb: LAYOUT_STRINGS.breadcrumbs.finance },
        loadChildren: () =>
          import('./features/finance/finance.routes').then((m) => m.FINANCE_ROUTES),
      },
      {
        path: 'profile',
        data: { breadcrumb: LAYOUT_STRINGS.breadcrumbs.profile },
        loadChildren: () => import('./features/access/access.routes').then((m) => m.PROFILE_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
