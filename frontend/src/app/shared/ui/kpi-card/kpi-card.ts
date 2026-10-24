import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { StatusTone } from '../status-chip/status-chip';

/** Cartão de indicador: ícone em tom pastel, rótulo em caixa alta e número grande. */
@Component({
  selector: 'erp-kpi-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, DecimalPipe, MatIconModule],
  template: `
    <span class="icon" aria-hidden="true"
      ><mat-icon>{{ icon() }}</mat-icon></span
    >
    <div class="body">
      <span class="label">{{ label() }}</span>
      @if (value() === null) {
        <span class="value erp-skeleton" aria-busy="true"></span>
      } @else {
        <strong class="value">
          @if (format() === 'currency') {
            {{ value() | currency }}
          } @else {
            {{ value() | number }}
          }
        </strong>
      }
      @if (hint(); as hint) {
        <span class="hint">{{ hint }}</span>
      }
    </div>
  `,
  styleUrl: './kpi-card.scss',
  host: { '[class]': '"tone-" + tone()' },
})
export class KpiCard {
  readonly label = input.required<string>();
  readonly icon = input.required<string>();
  /** `null` enquanto carrega. */
  readonly value = input<number | null>(null);
  readonly hint = input<string>();
  readonly tone = input<StatusTone>('primary');
  /** `currency` mostra em reais (R$ 1.234,56). */
  readonly format = input<'number' | 'currency'>('number');
}
