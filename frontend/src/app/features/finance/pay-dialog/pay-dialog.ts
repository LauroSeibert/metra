import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, required, submit, validate } from '@angular/forms/signals';
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmInput } from '@spartan-ng/helm/input';
import { Api } from '../../../api/api';
import { payFinancialEntry } from '../../../api/fn/financial-entries/pay-financial-entry';
import { FinancialEntryDto } from '../../../api/models/financial-entry-dto';
import { toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { FieldShell } from '../../../shared/ui/field-shell/field-shell';
import { MoneyInput } from '../../../shared/ui/money-input/money-input';
import { FINANCE_STRINGS } from '../finance.strings';

export interface PayDialogData {
  readonly entry: FinancialEntryDto;
  readonly title: string;
}

interface PayModel {
  paidAt: string;
  paidAmount: number | null;
}

/** Data de hoje no fuso do navegador, no formato do input de data (aaaa-mm-dd). */
export function todayIso(): string {
  const now = new Date();
  const local = new Date(now.getTime() - now.getTimezoneOffset() * 60_000);
  return local.toISOString().slice(0, 10);
}

/** Baixa de um lançamento: data e valor pago (padrão: hoje e o valor da parcela). Fecha com `true`. */
@Component({
  selector: 'erp-pay-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CurrencyPipe, FormField, HlmButton, HlmDialogImports, HlmInput, FieldShell, MoneyInput],
  template: `
    <form class="grid gap-4" novalidate (submit)="save($event)">
      <hlm-dialog-header>
        <h3 hlmDialogTitle>{{ data.title }}</h3>
      </hlm-dialog-header>

      <p class="m-0 flex justify-between gap-4 rounded-md bg-muted px-4 py-3">
        <strong class="font-medium">{{ data.entry.description }}</strong>
        <span class="tabular-nums">{{ data.entry.amount | currency }}</span>
      </p>

      <erp-field-shell
        [label]="strings.paidAt"
        [required]="true"
        [error]="
          payForm.paidAt().touched() ? (payForm.paidAt().errors()[0]?.message ?? null) : null
        "
      >
        <input hlmInput type="date" [formField]="payForm.paidAt" />
      </erp-field-shell>
      <erp-field-shell
        [label]="strings.paidAmount"
        [required]="true"
        [hint]="strings.amountHint"
        [error]="
          payForm.paidAmount().touched()
            ? (payForm.paidAmount().errors()[0]?.message ?? null)
            : null
        "
      >
        <erp-money-input [formField]="payForm.paidAmount" />
      </erp-field-shell>
      @for (error of payForm().errors(); track $index) {
        <p class="m-0 text-sm text-destructive" role="alert">{{ error.message }}</p>
      }

      <hlm-dialog-footer>
        <button hlmBtn variant="outline" type="button" hlmDialogClose>{{ strings.cancel }}</button>
        <button hlmBtn type="submit" [disabled]="payForm().submitting()">
          {{ strings.confirm }}
        </button>
      </hlm-dialog-footer>
    </form>
  `,
})
export class PayDialog {
  private readonly api = inject(Api);
  private readonly notifier = inject(Notifier);
  private readonly dialogRef = inject<BrnDialogRef<boolean>>(BrnDialogRef);

  protected readonly data = injectBrnDialogContext<PayDialogData>();
  protected readonly strings = FINANCE_STRINGS.pay;
  protected readonly today = todayIso();
  protected readonly model = signal<PayModel>({
    paidAt: this.today,
    paidAmount: this.data.entry.amount,
  });
  protected readonly payForm = form(this.model, (path) => {
    required(path.paidAt, { message: FINANCE_STRINGS.validation.required });
    required(path.paidAmount, { message: FINANCE_STRINGS.validation.required });
    validate(path.paidAmount, ({ value }) =>
      (value() ?? 0) <= 0
        ? { kind: 'min', message: FINANCE_STRINGS.validation.positive }
        : undefined,
    );
  });

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    await submit(this.payForm, async () => {
      const { paidAt, paidAmount } = this.model();
      try {
        await this.api.invoke(payFinancialEntry, {
          id: this.data.entry.id,
          body: { paidAt, paidAmount, version: this.data.entry.version },
        });
        this.notifier.success(FINANCE_STRINGS.list.paidNotice);
        this.dialogRef.close(true);
        return undefined;
      } catch (error) {
        return toServerErrors(this.payForm, toApiProblem(error));
      }
    });
  }
}
