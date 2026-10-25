import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Título, subtítulo e ações da página, alinhadas à direita (padrão do protótipo). */
@Component({
  selector: 'erp-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class: 'mb-6 flex flex-wrap items-end justify-between gap-x-6 gap-y-4 max-sm:items-stretch',
  },
  template: `
    <div class="min-w-0">
      <h1
        class="m-0 text-title leading-tight font-semibold tracking-title text-foreground max-sm:text-xl"
      >
        {{ title() }}
      </h1>
      @if (subtitle(); as subtitle) {
        <p class="m-0 mt-1 text-sm text-muted-foreground">{{ subtitle }}</p>
      }
    </div>
    <div class="flex flex-wrap items-center gap-2 max-sm:w-full"><ng-content /></div>
  `,
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
}
