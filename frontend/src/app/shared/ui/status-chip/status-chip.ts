import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export type StatusTone = 'primary' | 'success' | 'danger' | 'info' | 'warning' | 'neutral';

/** Etiqueta em pílula, em tom pastel, para status e tipos de registro. */
@Component({
  selector: 'erp-status-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: '{{ label() }}',
  styleUrl: './status-chip.scss',
  host: { '[class]': '"tone-" + tone() + (plain() ? " plain" : "")' },
})
export class StatusChip {
  readonly label = input.required<string>();
  readonly tone = input<StatusTone>('neutral');
  /** Sem o indicador redondo (para códigos como UN, KG). */
  readonly plain = input(false);
}
