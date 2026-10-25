import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { LucideDynamicIcon } from '@lucide/angular';
import { ICONS, type IconName } from './icons';

/** Ícone Lucide: 16 px e traço 1,75 por padrão (18 px na navegação). Decorativo: `aria-hidden`. */
@Component({
  selector: 'erp-icon',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideDynamicIcon],
  host: { class: 'inline-flex shrink-0 items-center justify-center' },
  template: `<svg [lucideIcon]="data()" [size]="size()" [strokeWidth]="1.75"></svg>`,
})
export class Icon {
  readonly name = input.required<IconName>();
  readonly size = input(16);

  protected readonly data = computed(() => ICONS[this.name()]);
}
