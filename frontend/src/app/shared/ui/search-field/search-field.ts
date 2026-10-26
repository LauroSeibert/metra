import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  model,
  viewChild,
} from '@angular/core';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmTooltip } from '@spartan-ng/helm/tooltip';
import { Icon } from '../icon/icon';
import { SlashFocus } from '../slash-focus';
import { UI_STRINGS } from '../ui.strings';

/**
 * Busca da listagem: lupa, atalho "/" para focar e botão de limpar quando há texto.
 * Use com `[(value)]` ligado a um signal; o debounce fica com quem lê o valor.
 */
@Component({
  selector: 'erp-search-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmButton, HlmInput, HlmTooltip, Icon, SlashFocus],
  host: { class: 'relative block min-w-56 flex-1' },
  template: `
    <erp-icon
      name="search"
      class="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-muted-foreground"
    />
    <input
      #field
      hlmInput
      erpSlashFocus
      type="search"
      class="pr-10 pl-9"
      [attr.inputmode]="numeric() ? 'numeric' : null"
      [attr.aria-label]="label()"
      [placeholder]="label()"
      [value]="value()"
      (input)="value.set(field.value)"
    />
    @if (value()) {
      <button
        hlmBtn
        variant="ghost"
        size="icon-xs"
        type="button"
        class="absolute top-1/2 right-2 -translate-y-1/2"
        [attr.aria-label]="strings.clearSearch"
        [hlmTooltip]="strings.clearSearch"
        (click)="clear()"
      >
        <erp-icon name="x" [size]="14" />
      </button>
    } @else {
      <span
        class="erp-kbd pointer-events-none absolute top-1/2 right-2.5 -translate-y-1/2"
        aria-hidden="true"
        >/</span
      >
    }
  `,
})
export class SearchField {
  readonly label = input.required<string>();
  readonly numeric = input(false);
  readonly value = model('');

  protected readonly strings = UI_STRINGS;
  private readonly field = viewChild.required<ElementRef<HTMLInputElement>>('field');

  protected clear(): void {
    this.value.set('');
    this.field().nativeElement.focus();
  }
}
