import { DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  resource,
  signal,
} from '@angular/core';
import {
  form,
  FormField,
  hidden,
  maxLength,
  required,
  schema,
  submit,
  validate,
} from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Api } from '../../../api/api';
import { createStockMovement } from '../../../api/fn/stock/create-stock-movement';
import { listStockBalances } from '../../../api/fn/stock/list-stock-balances';
import { toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { decimalPlaces } from '../../products/product-rules';
import { PickedProduct, ProductPicker } from '../../products/product-picker/product-picker';
import { MoneyInput } from '../../../shared/ui/money-input/money-input';
import {
  SegmentedControl,
  SegmentedOption,
} from '../../../shared/ui/segmented-control/segmented-control';
import { STOCK_STRINGS } from '../stock.strings';

/** Tipos que o usuário lança à mão; estorno sai do histórico. */
export type ManualMovementType = 'Entry' | 'Exit' | 'Adjustment';

export interface MovementDialogData {
  /** Produto já escolhido (vindo da linha do saldo); nulo para escolher no diálogo. */
  readonly product: PickedProduct | null;
  readonly currentQuantity: number | null;
}

interface MovementModel {
  type: ManualMovementType;
  quantity: number | null;
  unitCost: number | null;
  note: string;
}

/** Mesmos limites do servidor (StockMovement). */
export const STOCK_RULES = { quantityScale: 3, noteMaxLength: 500 } as const;

const messages = STOCK_STRINGS.validation;
const movementSchema = schema<MovementModel>((path) => {
  required(path.quantity, { message: messages.required });
  validate(path.quantity, ({ value, valueOf }) => {
    const quantity = value();
    if (quantity === null) {
      return undefined;
    }
    if (decimalPlaces(quantity) > STOCK_RULES.quantityScale) {
      return { kind: 'scale', message: messages.decimals(STOCK_RULES.quantityScale) };
    }
    if (valueOf(path.type) === 'Adjustment') {
      return quantity < 0 ? { kind: 'min', message: messages.nonNegative } : undefined;
    }
    return quantity <= 0 ? { kind: 'min', message: messages.positive } : undefined;
  });
  hidden(path.unitCost, ({ valueOf }) => valueOf(path.type) !== 'Entry');
  validate(path.note, ({ value, valueOf }) =>
    valueOf(path.type) === 'Adjustment' && value().trim() === ''
      ? { kind: 'required', message: messages.required }
      : undefined,
  );
  maxLength(path.note, STOCK_RULES.noteMaxLength, {
    message: messages.maxLength(STOCK_RULES.noteMaxLength),
  });
});

/** Lançamento manual de estoque. Fecha com `true` quando lança. */
@Component({
  selector: 'erp-movement-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe,
    FormField,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MoneyInput,
    ProductPicker,
    SegmentedControl,
  ],
  templateUrl: './movement-dialog.html',
  styleUrl: './movement-dialog.scss',
})
export class MovementDialog {
  private readonly api = inject(Api);
  private readonly notifier = inject(Notifier);
  private readonly dialogRef = inject<MatDialogRef<MovementDialog, boolean>>(MatDialogRef);
  private readonly data = inject<MovementDialogData>(MAT_DIALOG_DATA);

  protected readonly strings = STOCK_STRINGS;
  protected readonly fixedProduct = this.data.product !== null;
  protected readonly product = signal<PickedProduct | null>(this.data.product);
  protected readonly productError = signal<string | null>(null);
  protected readonly typeOptions: readonly SegmentedOption<ManualMovementType>[] = [
    { value: 'Entry', label: STOCK_STRINGS.types.Entry },
    { value: 'Exit', label: STOCK_STRINGS.types.Exit },
    { value: 'Adjustment', label: STOCK_STRINGS.types.Adjustment },
  ];

  protected readonly model = signal<MovementModel>({
    type: 'Entry',
    quantity: null,
    unitCost: null,
    note: '',
  });
  protected readonly movementForm = form(this.model, movementSchema);
  protected readonly formErrors = computed(() => this.movementForm().errors());

  /** Saldo atual do produto escolhido (para o ajuste por contagem). */
  private readonly balance = resource({
    params: () => {
      const product = this.product();
      return product && !this.fixedProduct ? product : undefined;
    },
    loader: async ({ params }) => {
      const page = await this.api.invoke(listStockBalances, { search: params.sku, pageSize: 20 });
      return page.items.find((item) => item.productId === params.id)?.quantity ?? 0;
    },
  });
  protected readonly currentQuantity = computed(() => {
    if (this.fixedProduct) {
      return this.data.currentQuantity;
    }
    return this.product() && this.balance.hasValue() ? this.balance.value() : null;
  });

  protected setType(type: ManualMovementType): void {
    this.model.update((value) => ({ ...value, type }));
  }

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    const product = this.product();
    this.productError.set(product ? null : STOCK_STRINGS.validation.product);

    await submit(this.movementForm, async () => {
      if (!product) {
        return undefined;
      }
      const value = this.model();
      try {
        await this.api.invoke(createStockMovement, {
          body: {
            productId: product.id,
            type: value.type,
            quantity: value.quantity ?? 0,
            unitCost: value.type === 'Entry' ? value.unitCost : null,
            occurredAt: null,
            note: value.note.trim() || null,
          },
        });
        this.notifier.success(STOCK_STRINGS.dialog.saved);
        this.dialogRef.close(true);
        return undefined;
      } catch (error) {
        const problem = toApiProblem(error);
        this.productError.set(problem.fieldErrors['productId']?.[0] ?? null);
        return toServerErrors(this.movementForm, problem);
      }
    });
  }
}
