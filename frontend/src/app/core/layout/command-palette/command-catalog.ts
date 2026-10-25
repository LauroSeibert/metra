import { Injectable, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import type { IconName } from '../../../shared/ui/icon/icons';
import { AuthService } from '../../auth/auth.service';
import { ThemeService } from '../../auth/theme.service';
import { LAYOUT_STRINGS } from '../layout.strings';
import { NAV_ITEMS } from '../nav-items';

export interface PaletteCommand {
  readonly label: string;
  readonly icon: IconName;
  /** Rótulo do grupo ("Ir para", "Criar", "Ações"), mostrado à direita na busca. */
  readonly hint: string;
  readonly run: () => void;
}

export interface PaletteGroup {
  readonly label: string;
  readonly commands: readonly PaletteCommand[];
}

const STRINGS = LAYOUT_STRINGS.palette;

/** Sem acento e em minúsculas: "estoque" acha "Histórico de estoque". */
export function normalizeSearch(text: string): string {
  return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

/** Telas, criações e ações que a busca do topo e a paleta (celular) oferecem. */
@Injectable({ providedIn: 'root' })
export class CommandCatalog {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly theme = inject(ThemeService);

  readonly groups = computed<readonly PaletteGroup[]>(() => {
    const go = (label: string, icon: IconName, path: string, hint: string): PaletteCommand => ({
      label,
      icon,
      hint,
      run: () => void this.router.navigateByUrl(path),
    });
    const pages: PaletteCommand[] = [
      ...NAV_ITEMS.filter((item) => item.available).map((item) =>
        go(item.label, item.icon, item.path, STRINGS.goTo),
      ),
      go('Contas a receber', 'arrow-down-left', '/finance/receivables', STRINGS.goTo),
      go('Contas a pagar', 'arrow-up-right', '/finance/payables', STRINGS.goTo),
      go('Histórico de estoque', 'history', '/stock/movements', STRINGS.goTo),
      go(LAYOUT_STRINGS.myProfile, 'user', '/profile', STRINGS.goTo),
    ];
    const groups: PaletteGroup[] = [{ label: STRINGS.goTo, commands: pages }];
    if (this.auth.canWrite()) {
      groups.push({
        label: STRINGS.create,
        commands: [
          go('Nova venda', 'plus', '/sales/edit', STRINGS.create),
          go('Novo produto', 'plus', '/products/edit', STRINGS.create),
          go('Novo parceiro', 'plus', '/partners/edit', STRINGS.create),
          go('Nova conta a receber', 'plus', '/finance/receivables/edit', STRINGS.create),
          go('Nova conta a pagar', 'plus', '/finance/payables/edit', STRINGS.create),
        ],
      });
    }
    groups.push({
      label: STRINGS.actions,
      commands: [
        {
          label: this.theme.isDark() ? LAYOUT_STRINGS.toLightTheme : LAYOUT_STRINGS.toDarkTheme,
          icon: this.theme.isDark() ? 'sun' : 'moon',
          hint: STRINGS.actions,
          run: () => void this.theme.toggle(),
        },
      ],
    });
    return groups;
  });

  /** Todos os comandos numa lista só, na ordem dos grupos. */
  readonly commands = computed(() => this.groups().flatMap((group) => group.commands));
}
