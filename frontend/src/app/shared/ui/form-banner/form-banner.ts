import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { HlmButton } from '@spartan-ng/helm/button';
import { Icon } from '../icon/icon';

/** Erros do formulário que não pertencem a um campo (regra do servidor, conflito de versão). */
@Component({
  selector: 'erp-form-banner',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmButton, Icon],
  host: {
    class: 'banner flex items-start gap-3 rounded-md bg-warning-soft px-4 py-3 text-warning-text',
    role: 'alert',
  },
  template: `
    <erp-icon name="triangle-alert" />
    <div class="flex-1">
      @for (error of errors(); track $index) {
        <p class="m-0 text-sm">{{ error.message }}</p>
      }
    </div>
    @if (reloadLabel(); as label) {
      <button hlmBtn variant="outline" size="sm" type="button" (click)="reload.emit()">
        {{ label }}
      </button>
    }
  `,
})
export class FormBanner {
  readonly errors = input.required<readonly { readonly message?: string }[]>();
  /** Texto do botão "Recarregar"; sem ele, o botão não aparece (só em conflito de versão). */
  readonly reloadLabel = input<string | null>(null);
  readonly reload = output<void>();
}
