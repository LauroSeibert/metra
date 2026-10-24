import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * Moldura da aplicação: barra superior, menu lateral e área de conteúdo.
 * Conteúdo por projeção: [shellTopBar], [shellNav] e o conteúdo principal.
 */
@Component({
  selector: 'erp-app-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
})
export class AppShell {}
