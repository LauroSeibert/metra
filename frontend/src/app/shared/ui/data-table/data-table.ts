import { DecimalPipe, NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  contentChild,
  contentChildren,
  input,
  model,
  output,
  signal,
} from '@angular/core';
import { HlmButton } from '@spartan-ng/helm/button';
import {
  HlmEmpty,
  HlmEmptyContent,
  HlmEmptyDescription,
  HlmEmptyHeader,
  HlmEmptyMedia,
  HlmEmptyTitle,
} from '@spartan-ng/helm/empty';
import { HlmSkeleton } from '@spartan-ng/helm/skeleton';
import { delayedFlag } from '../../data/delayed-flag';
import { Icon } from '../icon/icon';
import { UI_STRINGS } from '../ui.strings';
import { DataTableCell, DataTableExpandedRow } from './data-table-templates';

export interface DataTableColumn {
  readonly key: string;
  readonly header: string;
  readonly align?: 'start' | 'end' | 'center';
  readonly sortable?: boolean;
  /**
   * Papel da coluna quando a tabela vira lista de cartões (largura estreita):
   * `title` vira o título do cartão, `subtitle` a linha abaixo, `actions` o rodapé.
   * Sem papel, aparece como "rótulo: valor".
   */
  readonly role?: 'title' | 'subtitle' | 'actions';
  /** Coluna secundária: some quando a tabela tem menos de 1000 px (volta no modo cartão). */
  readonly optional?: boolean;
}

export interface DataTableSort {
  readonly field: string;
  readonly direction: 'asc' | 'desc';
}

export interface DataTablePage {
  /** Página a partir de 1, como na API. */
  readonly page: number;
  readonly pageSize: number;
}

type RowKey = string | number;

const MAX_SKELETON_ROWS = 10;
/** Mesma duração do --erp-duration-base (200 ms), usada na animação de fechar o detalhe. */
const CLOSE_ANIMATION_MS = 200;
/** Larguras das barras do skeleton, alternadas por coluna para não parecer um bloco só. */
const SKELETON_WIDTHS = [70, 45, 55, 35] as const;
/** Duplo clique nesses elementos é deles, não da linha. */
const INTERACTIVE = 'a, button, input, select, textarea, [role="button"]';

/**
 * Tabela dentro de cartão, com ordenação e paginação feitas no servidor, linha expansível e os
 * três estados obrigatórios de toda tela: carregando, vazio e erro.
 */
@Component({
  selector: 'erp-data-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe,
    NgTemplateOutlet,
    HlmButton,
    HlmEmpty,
    HlmEmptyContent,
    HlmEmptyDescription,
    HlmEmptyHeader,
    HlmEmptyMedia,
    HlmEmptyTitle,
    HlmSkeleton,
    Icon,
  ],
  templateUrl: './data-table.html',
  styleUrl: './data-table.scss',
})
export class DataTable<T> {
  readonly columns = input.required<readonly DataTableColumn[]>();
  readonly rows = input<readonly T[]>([]);
  readonly rowKey = input.required<(row: T) => RowKey>();
  readonly loading = input(false);
  readonly error = input<string | null>(null);
  readonly emptyMessage = input<string>(UI_STRINGS.empty);
  /** Uma frase sob o título do estado vazio (opcional). */
  readonly emptyDescription = input<string | null>(null);
  /** Rótulo da ação do estado vazio (ex.: "Limpar filtros"); sem ele, não há botão. */
  readonly emptyActionLabel = input<string | null>(null);
  readonly sort = model<DataTableSort | null>(null);
  readonly total = input(0);
  readonly page = input(1);
  readonly pageSize = input(20);
  /** Linha esmaecida (ex.: registro inativo). */
  readonly rowMuted = input<(row: T) => boolean>(() => false);

  readonly pageChange = output<DataTablePage>();
  readonly retry = output<void>();
  readonly emptyAction = output<void>();
  /** Duplo clique numa linha (fora de botões e links): a tela decide, em geral abrir a edição. */
  readonly rowActivate = output<T>();

  protected readonly strings = UI_STRINGS;
  protected readonly cells = contentChildren(DataTableCell<T>);
  protected readonly expandedRow = contentChild(DataTableExpandedRow<T>);
  protected readonly expandedKeys = signal<ReadonlySet<RowKey>>(new Set());
  /** Linhas recolhendo: ficam na tela até a animação de fechar terminar. */
  protected readonly closingKeys = signal<ReadonlySet<RowKey>>(new Set());

  protected readonly cellTemplates = computed(
    () => new Map(this.cells().map((cell) => [cell.column(), cell.template])),
  );
  protected readonly columnCount = computed(
    () => this.columns().length + (this.expandedRow() ? 1 : 0),
  );
  /** Carregamento só aparece se demorar (ver LOADING_DELAY_MS); antes disso a tela não pisca. */
  protected readonly showLoading = delayedFlag(this.loading);
  protected readonly showSkeleton = computed(
    () => this.loading() && !this.error() && this.rows().length === 0,
  );
  /** Linhas fantasma no formato da página: a altura não pula quando os dados chegam. */
  protected readonly skeletonRows = computed(() =>
    Array.from({ length: Math.min(this.pageSize(), MAX_SKELETON_ROWS) }, (_, index) => index),
  );
  protected readonly showEmpty = computed(
    () => !this.loading() && !this.error() && this.rows().length === 0,
  );

  protected cellClass(column: DataTableColumn): string {
    return [
      'align-' + (column.align ?? 'start'),
      column.role ? 'role-' + column.role : '',
      column.optional ? 'optional' : '',
    ]
      .filter(Boolean)
      .join(' ');
  }

  protected value(row: T, key: string): unknown {
    return (row as Record<string, unknown>)[key];
  }

  protected sortDirection(column: DataTableColumn): 'ascending' | 'descending' | 'none' {
    const sort = this.sort();
    if (!sort || sort.field !== column.key) {
      return 'none';
    }
    return sort.direction === 'asc' ? 'ascending' : 'descending';
  }

  protected toggleSort(column: DataTableColumn): void {
    const current = this.sort();
    const direction = current?.field === column.key && current.direction === 'asc' ? 'desc' : 'asc';
    this.sort.set({ field: column.key, direction });
  }

  protected isExpanded(row: T): boolean {
    return this.expandedKeys().has(this.rowKey()(row));
  }

  protected isClosing(row: T): boolean {
    return this.closingKeys().has(this.rowKey()(row));
  }

  protected toggleExpanded(row: T): void {
    const key = this.rowKey()(row);
    if (this.isClosing(row)) {
      return;
    }
    if (!this.expandedKeys().has(key)) {
      this.expandedKeys.set(new Set(this.expandedKeys()).add(key));
      return;
    }
    // Recolher: anima o fechamento e só então tira a linha de detalhe.
    this.closingKeys.set(new Set(this.closingKeys()).add(key));
    setTimeout(() => {
      const expanded = new Set(this.expandedKeys());
      expanded.delete(key);
      this.expandedKeys.set(expanded);
      const closing = new Set(this.closingKeys());
      closing.delete(key);
      this.closingKeys.set(closing);
    }, CLOSE_ANIMATION_MS);
  }

  protected skeletonWidth(index: number): number {
    return SKELETON_WIDTHS[index % SKELETON_WIDTHS.length];
  }

  protected onRowDblClick(event: MouseEvent, row: T): void {
    if (event.target instanceof Element && event.target.closest(INTERACTIVE)) {
      return;
    }
    // O duplo clique seleciona a palavra sob o cursor; a seleção não faz sentido aqui.
    globalThis.getSelection?.()?.removeAllRanges();
    this.rowActivate.emit(row);
  }

  protected readonly pageCount = computed(() =>
    Math.max(1, Math.ceil(this.total() / this.pageSize())),
  );
  protected readonly firstShown = computed(() =>
    this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1,
  );
  protected readonly lastShown = computed(() =>
    Math.min(this.total(), this.page() * this.pageSize()),
  );

  protected isMuted(row: T): boolean {
    return this.rowMuted()(row);
  }

  protected goTo(page: number): void {
    this.pageChange.emit({ page, pageSize: this.pageSize() });
  }
}
