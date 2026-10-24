import { TestBed } from '@angular/core/testing';
import { UserProfileDto } from '../../api/models/user-profile-dto';
import { AuthService, Role } from './auth.service';

/**
 * Para testes: abre uma sessão falsa com o perfil pedido (padrão Operador, que grava). Chame depois
 * de configurar o TestBed e antes de criar o componente.
 */
export function signInAs(
  role: Role = 'Operator',
  profile: Partial<UserProfileDto> = {},
): AuthService {
  localStorage.setItem('erp.session', JSON.stringify({ accessToken: 'a', refreshToken: 'r' }));
  const auth = TestBed.inject(AuthService);
  auth.profile.set({
    id: 1,
    email: 'teste@erp.local',
    fullName: 'Usuária Teste',
    role,
    theme: 'Light',
    pageSize: 20,
    version: 1,
    ...profile,
  });
  return auth;
}

/** Limpa a sessão falsa entre testes. */
export function signOut(): void {
  localStorage.removeItem('erp.session');
}
