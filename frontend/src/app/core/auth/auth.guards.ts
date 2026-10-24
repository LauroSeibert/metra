import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService, Role } from './auth.service';

/** Área logada: sem sessão, vai para o login e volta depois para a página pedida. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  if (auth.isAuthenticated()) {
    return true;
  }
  return inject(Router).createUrlTree(['/login'], {
    queryParams: state.url && state.url !== '/' ? { returnUrl: state.url } : {},
  });
};

/** Tela de login: quem já está logado vai direto para o início. */
export const guestGuard: CanActivateFn = () =>
  inject(AuthService).isAuthenticated() ? inject(Router).createUrlTree(['/']) : true;

/**
 * Rota restrita a perfis (ex.: configurações só para Admin). É conveniência de navegação: a API
 * recusa com 403 de qualquer jeito.
 */
export function roleGuard(...roles: Role[]): CanActivateFn {
  return () => {
    const role = inject(AuthService).role();
    return role !== null && roles.includes(role) ? true : inject(Router).createUrlTree(['/']);
  };
}
