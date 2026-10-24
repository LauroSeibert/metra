import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, required, submit, validate } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Api } from '../../../api/api';
import { payFinancialEntry } from '../../../api/fn/financial-entries/pay-financial-entry';
import { FinancialEntryDto } from '../../../api/models/financial-entry-dto';
import { toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
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
  imports: [
    CurrencyPipe,
    FormField,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MoneyInput,
  ],
  template: `
    <form novalidate (submit)="save($event)">
      <h2 mat-dialog-title>{{ data.title }}</h2>
      <mat-dialog-content>
        <p class="entry">
          <strong>{{ data.entry.description }}</strong>
          <span>{{ data.entry.amount | currency }}</span>
        </p>
        <mat-form-field>
          <mat-label>{{ strings.paidAt }}</mat-label>
          <input matInput type="date" [formField]="payForm.paidAt" />
          @if (payForm.paidAt().errors()[0]; as error) {
            <mat-error>{{ error.message }}</mat-error>
          }
        </mat-form-field>
        <mat-form-field>
          <mat-label>{{ strings.paidAmount }}</mat-label>
          <erp-money-input [formField]="payForm.paidAmount" />
          <mat-hint>{{ strings.amountHint }}</mat-hint>
          @if (payForm.paidAmount().errors()[0]; as error) {
            <mat-error>{{ error.message }}</mat-error>
          }
        </mat-form-field>
        @for (error of payForm().errors(); track $index) {
          <p class="error" role="alert">{{ error.message }}</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button matButton type="button" mat-dialog-close>{{ strings.cancel }}</button>
        <button
          matButton="filled"
          class="erp-primary-action"
          type="submit"
          [disabled]="payForm().submitting()"
        >
          {{ strings.confirm }}
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    mat-dialog-content {
      display: flex;
      flex-direction: column;
      gap: var(--erp-space-2);
    }

    .entry {
      display: flex;
      justify-content: space-between;
      gap: var(--erp-space-4);
      margin: 0 0 var(--erp-space-2);
      padding: var(--erp-space-3) var(--erp-space-4);
      border-radius: var(--erp-radius-md);
      background: var(--erp-surface-muted);
    }

    .error {
      margin: 0;
      color: var(--erp-danger);
    }
  `,
})
export class PayDialog {
  private readonly api = inject(Api);
  private readonly notifier = inject(Notifier);
  private readonly dialogRef = inject<MatDialogRef<PayDialog, boolean>>(MatDialogRef);

  protected readonly data = inject<PayDialogData>(MAT_DIALOG_DATA);
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
