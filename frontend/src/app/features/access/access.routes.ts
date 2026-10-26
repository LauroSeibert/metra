import { Routes } from '@angular/router';

/** /login, fora do layout principal. */
export const LOGIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./login/login').then((m) => m.Login),
  },
];

/** /profile ("Meu perfil"), dentro do layout. */
export const PROFILE_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./profile/profile').then((m) => m.Profile),
  },
];
