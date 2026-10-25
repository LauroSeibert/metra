import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HlmButton } from '@spartan-ng/helm/button';
import { Icon } from '../icon/icon';

/**
 * Cabeçalho das telas de cadastro: link de volta, rótulo da ação ("Editar produto"), título com
 * selos (`[badges]`) e, à direita, as ações (Salvar, menu). Ver a tela de produto.
 */
@Component({
  selector: 'erp-form-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmButton, Icon, RouterLink],
  host: { class: 'flex flex-col gap-3' },
  template: `
    <a
      hlmBtn
      variant="ghost"
      size="sm"
      class="-ml-2.5 w-fit text-muted-foreground hover:text-foreground"
      [routerLink]="backLink()"
    >
      <erp-icon name="arrow-left" [size]="16" />
      {{ backLabel() }}
    </a>

    <div class="flex flex-wrap items-end justify-between gap-x-6 gap-y-4">
      <div class="min-w-0">
        <p class="m-0 text-sm text-muted-foreground">{{ eyebrow() }}</p>
        <div class="flex flex-wrap items-center gap-x-3 gap-y-2">
          <h1
            class="m-0 text-title leading-tight font-semibold tracking-title wrap-anywhere text-foreground max-sm:text-xl"
          >
            {{ title() }}
          </h1>
          <ng-content select="[badges]" />
        </div>
        @if (subtitle(); as text) {
          <p class="m-0 mt-1 text-sm text-muted-foreground">{{ text }}</p>
        }
      </div>
      <div class="flex items-center gap-2"><ng-content /></div>
    </div>
  `,
})
export class FormHeader {
  readonly backLink = input.required<string>();
  readonly backLabel = input.required<string>();
  readonly eyebrow = input.required<string>();
  readonly title = input.required<string>();
  readonly subtitle = input<string | null>(null);
}
