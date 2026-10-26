import { PercentPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Margem sobre o preço de venda: valor grande e barra. Vermelho quando negativa. */
@Component({
  selector: 'erp-margin-meter',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PercentPipe],
  host: { class: 'block rounded-md border border-border bg-muted p-4' },
  template: `
    <div class="text-label text-muted-foreground">{{ label() }}</div>
    <div
      class="mt-1 text-3xl leading-none font-semibold tracking-title tabular-nums"
      [class.text-success]="!negative()"
      [class.text-destructive]="negative()"
    >
      {{ margin() === null ? '—' : (margin() | percent: '1.0-1') }}
    </div>
    <div class="mt-3 h-1.5 overflow-hidden rounded-full bg-border" aria-hidden="true">
      <div
        class="h-full rounded-full transition-[width] duration-200"
        [class.bg-success]="!negative()"
        [class.bg-destructive]="negative()"
        [style.width.%]="barWidth()"
      ></div>
    </div>
    @if (negative() && warning(); as text) {
      <p class="m-0 mt-2 text-caption text-destructive">{{ text }}</p>
    }
  `,
})
export class MarginMeter {
  /** Fração (0,46 = 46%) ou null quando ainda não dá para calcular. */
  readonly margin = input<number | null>(null);
  readonly label = input.required<string>();
  readonly warning = input<string | null>(null);

  protected readonly negative = computed(() => (this.margin() ?? 0) < 0);
  /** Largura da barra (0–100), limitada para margens negativas ou acima de 100%. */
  protected readonly barWidth = computed(() => {
    const margin = this.margin();
    return margin === null ? 0 : Math.min(100, Math.abs(margin) * 100);
  });
}
