import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guards';
import { LAYOUT_STRINGS } from './core/layout/layout.strings';
import { MainLayout } from './core/layout/main-layout';
import { ACCESS_STRINGS } from './features/access/access.strings';
import { PARTNERS_STRINGS } from './features/partners/partners.strings';
import { PRODUCTS_STRINGS } from './features/products/products.strings';

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
        data: { breadcrumb: PRODUCTS_STRINGS.breadcrumb },
        loadChildren: () =>
          import('./features/products/products.routes').then((m) => m.PRODUCTS_ROUTES),
      },
      {
        path: 'partners',
        data: { breadcrumb: PARTNERS_STRINGS.breadcrumb },
        loadChildren: () =>
          import('./features/partners/partners.routes').then((m) => m.PARTNERS_ROUTES),
      },
      {
        path: 'profile',
        data: { breadcrumb: ACCESS_STRINGS.profile.breadcrumb },
        loadChildren: () => import('./features/access/access.routes').then((m) => m.PROFILE_ROUTES),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
