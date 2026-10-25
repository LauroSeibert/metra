import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import { FormValueControl, ValidationError } from '@angular/forms/signals';

/** Até 9.999.999.999.999,99: cabe no DECIMAL(18,2) do servidor e em inteiro seguro do JS. */
const MAX_DIGITS = 15;

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

/** Valor em reais a partir dos dígitos digitados, lidos como centavos ("100" → 1). */
export function parseCents(text: string): number | null {
  const digits = text
    .replace(/\D/g, '')
    .replace(/^0+(?=\d)/, '')
    .slice(0, MAX_DIGITS);
  return digits === '' ? null : Number(digits) / 100;
}

export function formatMoney(value: number | null): string {
  return value === null ? '' : currency.format(value);
}

/**
 * Campo monetário que preenche a partir dos centavos, como em caixa: digitar 1, 0, 0 mostra
 * R$ 0,01 → R$ 0,10 → R$ 1,00. Funciona com `[formField]` (Signal Forms), dentro de `erp-field-shell`.
 * O valor do modelo é em reais (`number | null`).
 */
@Component({
  selector: 'erp-money-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <input
      #native
      type="text"
      inputmode="numeric"
      autocomplete="off"
      class="h-control w-full min-w-0 rounded-md border border-input bg-card px-3 text-sm tabular-nums transition-[color,box-shadow] outline-none placeholder:text-muted-foreground focus-visible:border-ring focus-visible:shadow-focus disabled:cursor-not-allowed disabled:opacity-50 aria-invalid:border-destructive"
      [value]="text(value())"
      [disabled]="disabled()"
      [readOnly]="readonly()"
      [attr.aria-invalid]="touched() && errors().length > 0 ? 'true' : null"
      (input)="onInput()"
      (blur)="touch.emit()"
    />
  `,
})
export class MoneyInput implements FormValueControl<number | null> {
  readonly value = model<number | null>(null);
  readonly touched = input(false);
  readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);
  readonly disabled = input(false);
  readonly readonly = input(false);
  readonly required = input(false);
  readonly touch = output<void>();

  protected readonly text = formatMoney;
  private readonly native = viewChild.required<ElementRef<HTMLInputElement>>('native');

  focus(): void {
    this.native().nativeElement.focus();
  }

  protected onInput(): void {
    const element = this.native().nativeElement;
    const value = parseCents(element.value);
    this.value.set(value);
    // Reescreve mesmo quando o valor não muda (letra ou vírgula apagada), e mantém o cursor no fim.
    element.value = formatMoney(value);
    element.setSelectionRange(element.value.length, element.value.length);
  }
}
