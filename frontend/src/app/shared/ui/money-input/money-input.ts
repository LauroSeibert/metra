import {
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  inject,
  input,
  model,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { FormValueControl, ValidationError } from '@angular/forms/signals';
import { MatFormFieldControl } from '@angular/material/form-field';
import { Subject } from 'rxjs';

/** Até 9.999.999.999.999,99: cabe no DECIMAL(18,2) do servidor e em inteiro seguro do JS. */
const MAX_DIGITS = 15;

const currency = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' });

let nextId = 0;

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
 * R$ 0,01 → R$ 0,10 → R$ 1,00. Funciona com `[formField]` (Signal Forms) e dentro de
 * `mat-form-field`. O valor do modelo é em reais (`number | null`).
 */
@Component({
  selector: 'erp-money-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [{ provide: MatFormFieldControl, useFactory: () => inject(MoneyInput).control }],
  host: { class: 'erp-money-input' },
  template: `
    <input
      #native
      [id]="inputId"
      type="text"
      inputmode="numeric"
      autocomplete="off"
      [value]="text(value())"
      [disabled]="disabled()"
      [readOnly]="readonly()"
      [attr.aria-invalid]="control.errorState || null"
      [attr.aria-describedby]="describedBy()"
      (input)="onInput()"
      (focus)="onFocus()"
      (blur)="onBlur()"
    />
  `,
  styles: `
    :host {
      display: block;
    }

    input {
      width: 100%;
      padding: 0;
      border: 0;
      outline: 0;
      background: transparent;
      color: inherit;
      font: inherit;
      font-variant-numeric: tabular-nums;
    }
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

  readonly inputId = `erp-money-input-${nextId++}`;
  readonly control = new MoneyFieldControl(this);
  protected readonly describedBy = signal<string | null>(null);
  protected readonly text = formatMoney;
  private readonly native = viewChild.required<ElementRef<HTMLInputElement>>('native');

  constructor() {
    // O mat-form-field (OnPush) só se redesenha quando o controle avisa.
    effect(() => {
      this.value();
      this.touched();
      this.errors();
      this.disabled();
      this.required();
      this.control.stateChanges.next();
    });
  }

  focus(): void {
    this.native().nativeElement.focus();
  }

  setDescribedBy(ids: string): void {
    this.describedBy.set(ids || null);
  }

  protected onInput(): void {
    const element = this.native().nativeElement;
    const value = parseCents(element.value);
    this.value.set(value);
    // Reescreve mesmo quando o valor não muda (letra ou vírgula apagada), e mantém o cursor no fim.
    element.value = formatMoney(value);
    element.setSelectionRange(element.value.length, element.value.length);
  }

  protected onFocus(): void {
    this.control.focused = true;
    this.control.stateChanges.next();
  }

  protected onBlur(): void {
    this.control.focused = false;
    this.touch.emit();
    this.control.stateChanges.next();
  }
}

/**
 * Lado Material do campo. Fica separado do componente porque `MatFormFieldControl.value` é um
 * valor simples e, no Signal Forms, `value` precisa ser um `model()`.
 */
class MoneyFieldControl implements MatFormFieldControl<number> {
  readonly stateChanges = new Subject<void>();
  readonly controlType = 'erp-money-input';
  readonly ngControl = null;
  readonly placeholder = '';
  focused = false;

  constructor(private readonly owner: MoneyInput) {}

  get id(): string {
    return this.owner.inputId;
  }

  get value(): number | null {
    return this.owner.value();
  }

  get empty(): boolean {
    return this.owner.value() === null;
  }

  get shouldLabelFloat(): boolean {
    return this.focused || !this.empty;
  }

  get required(): boolean {
    return this.owner.required();
  }

  get disabled(): boolean {
    return this.owner.disabled();
  }

  get errorState(): boolean {
    return this.owner.touched() && this.owner.errors().length > 0;
  }

  setDescribedByIds(ids: string[]): void {
    this.owner.setDescribedBy(ids.join(' '));
  }

  onContainerClick(): void {
    this.owner.focus();
  }
}
