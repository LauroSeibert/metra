import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * Moldura da aplicação: barra superior, menu lateral e conteúdo em largura total.
 * - desktop: menu com rótulos ou recolhido (`compact`);
 * - tablet: menu só com ícones (`compact`);
 * - mobile: menu em gaveta sobre o conteúdo (`mobile` + `navOpen`).
 * Conteúdo por projeção: [shellTopBar], [shellNav] e o conteúdo principal.
 */
@Component({
  selector: 'erp-app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
  host: {
    '[class.compact]': 'compact()',
    '[class.mobile]': 'mobile()',
    '[class.nav-open]': 'navOpen()',
    '(document:keydown.escape)': 'navOpen() && closeNav.emit()',
  },
})
export class AppShell {
  readonly compact = input(false);
  readonly mobile = input(false);
  readonly navOpen = input(false);
  readonly closeNav = output<void>();
}
