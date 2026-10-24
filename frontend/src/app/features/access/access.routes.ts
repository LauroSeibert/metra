import { Routes } from '@angular/router';
import { provideMaterialFormDefaults } from '../../shared/ui/material-defaults';

// Rotas de acesso carregadas sob demanda: os padrões do Material de formulário ficam fora do
// bundle inicial (ver frontend/CLAUDE.md, "Armadilhas").

/** /login, fora do layout principal. */
export const LOGIN_ROUTES: Routes = [
  {
    path: '',
    providers: [provideMaterialFormDefaults()],
    loadComponent: () => import('./login/login').then((m) => m.Login),
  },
];

/** /profile ("Meu perfil"), dentro do layout. */
export const PROFILE_ROUTES: Routes = [
  {
    path: '',
    providers: [provideMaterialFormDefaults()],
    loadComponent: () => import('./profile/profile').then((m) => m.Profile),
  },
];
