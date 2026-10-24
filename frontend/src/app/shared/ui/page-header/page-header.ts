import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/** Título, subtítulo e ações da página, alinhadas à direita (padrão do protótipo). */
@Component({
  selector: 'erp-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="text">
      <h1>{{ title() }}</h1>
      @if (subtitle(); as subtitle) {
        <p>{{ subtitle }}</p>
      }
    </div>
    <div class="actions"><ng-content /></div>
  `,
  styleUrl: './page-header.scss',
})
export class PageHeader {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
}
