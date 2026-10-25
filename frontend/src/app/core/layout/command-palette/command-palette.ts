import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { HlmCommandImports } from '@spartan-ng/helm/command';
import { Icon } from '../../../shared/ui/icon/icon';
import type { IconName } from '../../../shared/ui/icon/icons';
import { AuthService } from '../../auth/auth.service';
import { ThemeService } from '../../auth/theme.service';
import { LAYOUT_STRINGS } from '../layout.strings';
import { NAV_ITEMS } from '../nav-items';
import { CommandPaletteService } from './command-palette.service';

interface PaletteCommand {
  readonly label: string;
  readonly icon: IconName;
  readonly run: () => void;
}

interface PaletteGroup {
  readonly label: string;
  readonly commands: readonly PaletteCommand[];
}

const STRINGS = LAYOUT_STRINGS.palette;

/** Sem acento e em minúsculas: "estoque" acha "Histórico de estoque". */
function normalize(text: string): string {
  return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

/**
 * Paleta de comandos (Ctrl/Cmd K): ir para uma tela, criar um registro ou alternar o tema.
 * Setas escolhem, Enter executa, Esc fecha.
 */
@Component({
  selector: 'erp-command-palette',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmCommandImports, Icon],
  host: { '(document:keydown)': 'onGlobalKey($event)' },
  template: `
    <hlm-command-dialog
      dialogContentClass="w-full sm:max-w-xl"
      [title]="strings.title"
      [description]="strings.description"
      [state]="palette.open() ? 'open' : 'closed'"
      (stateChange)="onState($event)"
    >
      <hlm-command [filter]="filter">
        <hlm-command-input [placeholder]="strings.placeholder" />
        <hlm-command-list>
          <div *hlmCommandEmptyState hlmCommandEmpty>{{ strings.empty }}</div>
          @for (group of groups(); track group.label) {
            <hlm-command-group>
              <hlm-command-group-label>{{ group.label }}</hlm-command-group-label>
              @for (command of group.commands; track command.label) {
                <button hlmCommandItem [value]="command.label" (selected)="run(command)">
                  <erp-icon [name]="command.icon" />
                  {{ command.label }}
                </button>
              }
            </hlm-command-group>
          }
        </hlm-command-list>
      </hlm-command>
    </hlm-command-dialog>
  `,
})
export class CommandPalette {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly theme = inject(ThemeService);
  protected readonly palette = inject(CommandPaletteService);

  protected readonly strings = STRINGS;
  protected readonly filter = (value: string, search: string): boolean =>
    normalize(value).includes(normalize(search.trim()));

  protected readonly groups = computed<readonly PaletteGroup[]>(() => {
    const go = (label: string, icon: IconName, path: string): PaletteCommand => ({
      label,
      icon,
      run: () => void this.router.navigateByUrl(path),
    });
    const pages: PaletteCommand[] = [
      ...NAV_ITEMS.filter((item) => item.available).map((item) =>
        go(item.label, item.icon, item.path),
      ),
      go('Contas a receber', 'arrow-down-left', '/finance/receivables'),
      go('Contas a pagar', 'arrow-up-right', '/finance/payables'),
      go('Histórico de estoque', 'history', '/stock/movements'),
      go(LAYOUT_STRINGS.myProfile, 'user', '/profile'),
    ];
    const groups: PaletteGroup[] = [{ label: STRINGS.goTo, commands: pages }];
    if (this.auth.canWrite()) {
      groups.push({
        label: STRINGS.create,
        commands: [
          go('Nova venda', 'plus', '/sales/edit'),
          go('Novo produto', 'plus', '/products/edit'),
          go('Novo parceiro', 'plus', '/partners/edit'),
          go('Nova conta a receber', 'plus', '/finance/receivables/edit'),
          go('Nova conta a pagar', 'plus', '/finance/payables/edit'),
        ],
      });
    }
    groups.push({
      label: STRINGS.actions,
      commands: [
        {
          label: this.theme.isDark() ? LAYOUT_STRINGS.toLightTheme : LAYOUT_STRINGS.toDarkTheme,
          icon: this.theme.isDark() ? 'sun' : 'moon',
          run: () => void this.theme.toggle(),
        },
      ],
    });
    return groups;
  });

  protected onGlobalKey(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.palette.toggle();
    }
  }

  protected onState(state: string): void {
    this.palette.open.set(state === 'open');
  }

  protected run(command: PaletteCommand): void {
    this.palette.open.set(false);
    command.run();
  }
}
