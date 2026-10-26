import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { HlmCard } from '@spartan-ng/helm/card';
import { Icon } from '../icon/icon';
import type { IconName } from '../icon/icons';

/**
 * Cartão de seção: bloco de ícone, título e descrição no cabeçalho, corpo com padding e espaço entre
 * os campos. Uma ação opcional vai à direita do cabeçalho (`<button sectionAction>`).
 */
@Component({
  selector: 'erp-section-card',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmCard, Icon],
  host: { class: 'block h-full min-w-0' },
  template: `
    <section hlmCard class="h-full">
      <header class="flex items-center gap-3 border-b border-border px-5 py-4">
        <div class="icon-block flex shrink-0 items-center justify-center rounded-md bg-accent">
          <erp-icon [name]="icon()" [size]="18" />
        </div>
        <div class="min-w-0 flex-1">
          <h2 class="m-0 text-base leading-snug font-semibold">{{ title() }}</h2>
          @if (description(); as text) {
            <p class="m-0 text-label text-muted-foreground">{{ text }}</p>
          }
        </div>
        <ng-content select="[sectionAction]" />
      </header>
      <div class="flex flex-col gap-4 p-5">
        <ng-content />
      </div>
    </section>
  `,
})
export class SectionCard {
  readonly icon = input.required<IconName>();
  readonly title = input.required<string>();
  readonly description = input<string | null>(null);
}
