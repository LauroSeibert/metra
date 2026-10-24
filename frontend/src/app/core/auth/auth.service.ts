import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Api } from '../../api/api';
import { login } from '../../api/fn/auth/login';
import { logout } from '../../api/fn/auth/logout';
import { refreshSession } from '../../api/fn/auth/refresh-session';
import { getMyProfile } from '../../api/fn/profile/get-my-profile';
import { SessionTokensDto } from '../../api/models/session-tokens-dto';
import { UserProfileDto } from '../../api/models/user-profile-dto';
import { applyTheme } from './theme';

/** Perfis de acesso, com os mesmos nomes da API (Erp.Domain.Access.Roles). */
export const ROLES = { admin: 'Admin', operator: 'Operator', viewer: 'Viewer' } as const;
export type Role = (typeof ROLES)[keyof typeof ROLES];

const SESSION_KEY = 'erp.session';

interface StoredSession {
  readonly accessToken: string;
  readonly refreshToken: string;
}

/**
 * Sessão do usuário: tokens, perfil e permissões. O token de acesso vai no cabeçalho de cada
 * chamada (auth.interceptor.ts); quando expira, o refresh renova sem o usuário perceber. Os tokens
 * ficam no localStorage para a sessão sobreviver a um F5 (decisão e riscos no ADR 0003).
 * A interface só esconde o que o perfil não pode fazer; quem decide é a API.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly session = signal<StoredSession | null>(readSession());
  private refreshing: Promise<boolean> | null = null;

  readonly profile = signal<UserProfileDto | null>(null);
  readonly isAuthenticated = computed(() => this.session() !== null);
  readonly role = computed(() => (this.profile()?.role ?? null) as Role | null);
  readonly isAdmin = computed(() => this.role() === ROLES.admin);
  /** Admin e Operador gravam cadastros e movimentos; Leitura só consulta. */
  readonly canWrite = computed(() => this.role() === ROLES.admin || this.role() === ROLES.operator);
  /** Itens por página escolhidos em "Meu perfil"; padrão das listagens sem `pageSize` na URL. */
  readonly pageSize = computed(() => this.profile()?.pageSize ?? 20);

  constructor() {
    effect(() => {
      const profile = this.profile();
      if (profile) {
        applyTheme(profile.theme);
      }
    });
  }

  accessToken(): string | null {
    return this.session()?.accessToken ?? null;
  }

  /** Na abertura do app: com sessão guardada, carrega o perfil (renovando o token se preciso). */
  async restore(): Promise<void> {
    if (!this.session()) {
      return;
    }
    try {
      await this.loadProfile();
    } catch {
      this.clear();
    }
  }

  async login(email: string, password: string): Promise<void> {
    const tokens = await this.api.invoke(login, { body: { email, password } });
    this.store(tokens);
    await this.loadProfile();
  }

  async loadProfile(): Promise<void> {
    this.profile.set(await this.api.invoke(getMyProfile));
  }

  /** Renova os tokens uma vez, mesmo com várias chamadas recebendo 401 ao mesmo tempo. */
  refresh(): Promise<boolean> {
    this.refreshing ??= this.renew().finally(() => (this.refreshing = null));
    return this.refreshing;
  }

  async logout(): Promise<void> {
    try {
      await this.api.invoke(logout);
    } catch {
      // Sair funciona mesmo sem servidor: os tokens locais são descartados de qualquer jeito.
    }
    this.clear();
    await this.router.navigate(['/login']);
  }

  /** Sessão perdida (refresh recusado): volta ao login e depois para onde o usuário estava. */
  async expire(): Promise<void> {
    const returnUrl = this.router.url;
    this.clear();
    await this.router.navigate(['/login'], {
      queryParams: returnUrl && returnUrl !== '/' ? { returnUrl, expired: 1 } : { expired: 1 },
    });
  }

  private async renew(): Promise<boolean> {
    const refreshToken = this.session()?.refreshToken;
    if (!refreshToken) {
      return false;
    }
    try {
      this.store(await this.api.invoke(refreshSession, { body: { refreshToken } }));
      return true;
    } catch {
      return false;
    }
  }

  private store(tokens: SessionTokensDto): void {
    const session: StoredSession = {
      accessToken: tokens.accessToken,
      refreshToken: tokens.refreshToken,
    };
    this.session.set(session);
    try {
      localStorage.setItem(SESSION_KEY, JSON.stringify(session));
    } catch {
      // Sem armazenamento (modo privado): a sessão vale só enquanto a aba estiver aberta.
    }
  }

  private clear(): void {
    this.session.set(null);
    this.profile.set(null);
    try {
      localStorage.removeItem(SESSION_KEY);
    } catch {
      // Nada a limpar.
    }
  }
}

function readSession(): StoredSession | null {
  try {
    const raw = localStorage.getItem(SESSION_KEY);
    const value: unknown = raw ? JSON.parse(raw) : null;
    return isSession(value) ? value : null;
  } catch {
    return null;
  }
}

function isSession(value: unknown): value is StoredSession {
  return (
    typeof value === 'object' &&
    value !== null &&
    typeof (value as Record<string, unknown>)['accessToken'] === 'string' &&
    typeof (value as Record<string, unknown>)['refreshToken'] === 'string'
  );
}
