import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { HlmBadge } from '@spartan-ng/helm/badge';

export type StatusTone = 'primary' | 'success' | 'danger' | 'info' | 'warning' | 'neutral';

const VARIANT = {
  primary: 'outline',
  info: 'outline',
  success: 'ok',
  danger: 'danger',
  warning: 'warn',
  neutral: 'off',
} as const;

/** Etiqueta em pílula com ponto, para status e tipos de registro (badge do Spartan). */
@Component({
  selector: 'erp-status-chip',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmBadge],
  host: { class: 'inline-flex' },
  template: `<span hlmBadge [variant]="variant()">{{ label() }}</span>`,
})
export class StatusChip {
  readonly label = input.required<string>();
  readonly tone = input<StatusTone>('neutral');
  /** Sem o ponto, em mono (para códigos como UN, KG). */
  readonly plain = input(false);

  protected readonly variant = computed(() => (this.plain() ? 'code' : VARIANT[this.tone()]));
}
