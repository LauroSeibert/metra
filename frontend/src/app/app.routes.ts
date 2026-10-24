import { Routes } from '@angular/router';
import { LAYOUT_STRINGS } from './core/layout/layout.strings';
import { MainLayout } from './core/layout/main-layout';

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
    ],
  },
  { path: '**', redirectTo: '' },
];
