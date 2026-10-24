import { CurrencyPipe, DecimalPipe, NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';

/** Uma célula da faixa: rótulo, número grande e uma linha de apoio. */
export interface KpiItem<K extends string = string> {
  readonly key: K;
  readonly label: string;
  /** `null` enquanto carrega. */
  readonly value: number | null;
  readonly hint?: string;
  /** `currency` mostra em reais (R$ 1.234,56). */
  readonly format?: 'number' | 'currency';
  /** Clicável: escolher a célula aplica o filtro correspondente na listagem. */
  readonly selectable?: boolean;
}

/**
 * Faixa de indicadores: um cartão dividido em células (referência: docs/design/listagem.png). Células
 * marcadas como <c>selectable</c> funcionam como filtro: a escolhida fica destacada e vale como
 * <c>selected</c>. As demais são só informação.
 */
@Component({
  selector: 'erp-kpi-strip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DecimalPipe, NgTemplateOutlet],
  template: `
    @for (item of items(); track item.key) {
      @if (item.selectable) {
        <button
          type="button"
          class="cell selectable"
          [class.selected]="item.key === selected()"
          [attr.aria-pressed]="item.key === selected()"
          (click)="selected.set(item.key)"
        >
          <ng-container
            [ngTemplateOutlet]="content"
            [ngTemplateOutletContext]="{ $implicit: item }"
          />
        </button>
      } @else {
        <div class="cell">
          <ng-container
            [ngTemplateOutlet]="content"
            [ngTemplateOutletContext]="{ $implicit: item }"
          />
        </div>
      }
    }

    <ng-template #content let-item>
      <span class="label">{{ item.label }}</span>
      @if (item.value === null) {
        <span class="value erp-skeleton" aria-busy="true"></span>
      } @else {
        <strong class="value">
          @if (item.format === 'currency') {
            {{ item.value | currency }}
          } @else {
            {{ item.value | number }}
          }
        </strong>
      }
      @if (item.hint) {
        <span class="hint">{{ item.hint }}</span>
      }
    </ng-template>
  `,
  styleUrl: './kpi-strip.scss',
  host: { role: 'group', '[attr.aria-label]': 'label()' },
})
export class KpiStrip<K extends string = string> {
  readonly items = input.required<readonly KpiItem<K>[]>();
  readonly selected = model<K | null>(null);
  readonly label = input<string>();
}
