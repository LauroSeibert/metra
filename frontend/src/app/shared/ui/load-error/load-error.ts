import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCard } from '@spartan-ng/helm/card';
import {
  HlmEmpty,
  HlmEmptyContent,
  HlmEmptyHeader,
  HlmEmptyMedia,
  HlmEmptyTitle,
} from '@spartan-ng/helm/empty';
import { Icon } from '../icon/icon';

/** Cartão de erro ao carregar uma tela de cadastro, com botão para tentar de novo. */
@Component({
  selector: 'erp-load-error',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    HlmButton,
    HlmCard,
    HlmEmpty,
    HlmEmptyContent,
    HlmEmptyHeader,
    HlmEmptyMedia,
    HlmEmptyTitle,
    Icon,
  ],
  host: { class: 'block', role: 'alert' },
  template: `
    <section hlmCard>
      <div class="p-5">
        <div hlmEmpty>
          <div hlmEmptyHeader>
            <div hlmEmptyMedia variant="icon"><erp-icon name="cloud-off" [size]="20" /></div>
            <h3 hlmEmptyTitle>{{ message() }}</h3>
          </div>
          <div hlmEmptyContent>
            <button hlmBtn variant="outline" type="button" (click)="retry.emit()">
              <erp-icon name="refresh" />
              {{ retryLabel() }}
            </button>
          </div>
        </div>
      </div>
    </section>
  `,
})
export class LoadError {
  readonly message = input.required<string>();
  readonly retryLabel = input.required<string>();
  readonly retry = output<void>();
}
