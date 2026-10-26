import { HlmTooltip } from '@spartan-ng/helm/tooltip';
import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
  resource,
  signal,
} from '@angular/core';
import {
  apply,
  disabled,
  form,
  FormField,
  maxLength,
  schema,
  submit,
  validate,
} from '@angular/forms/signals';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Api } from '../../../api/api';
import { cancelSale } from '../../../api/fn/sales/cancel-sale';
import { confirmSale } from '../../../api/fn/sales/confirm-sale';
import { createSale } from '../../../api/fn/sales/create-sale';
import { getSale } from '../../../api/fn/sales/get-sale';
import { getSaleSettings } from '../../../api/fn/sales/get-sale-settings';
import { updateSale } from '../../../api/fn/sales/update-sale';
import { PaymentMethod } from '../../../api/models/payment-method';
import { SaleDto } from '../../../api/models/sale-dto';
import { SaveSaleRequest } from '../../../api/models/save-sale-request';
import { AuthService } from '../../../core/auth/auth.service';
import { API_ERROR_CODES, toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { delayedFlag } from '../../../shared/data/delayed-flag';
import { ConfirmService } from '../../../shared/ui/confirm-dialog/confirm-dialog';
import { MoneyInput } from '../../../shared/ui/money-input/money-input';
import { StatusChip, StatusTone } from '../../../shared/ui/status-chip/status-chip';
import { PartnerPicker, PickedPartner } from '../../partners/partner-picker/partner-picker';
import { PickedProduct, ProductPicker } from '../../products/product-picker/product-picker';
import { SALES_STRINGS } from '../sales.strings';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCard } from '@spartan-ng/helm/card';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSelectImports } from '@spartan-ng/helm/select';
import { HlmSkeleton } from '@spartan-ng/helm/skeleton';
import { HlmTextarea } from '@spartan-ng/helm/textarea';
import { fieldError } from '../../../shared/ui/field-shell/field-error';
import { FieldShell } from '../../../shared/ui/field-shell/field-shell';
import { FormBanner } from '../../../shared/ui/form-banner/form-banner';
import { FormHeader } from '../../../shared/ui/form-header/form-header';
import { Icon } from '../../../shared/ui/icon/icon';
import { LoadError } from '../../../shared/ui/load-error/load-error';
import { SectionCard } from '../../../shared/ui/section-card/section-card';

/** Mesmos limites do servidor (Sale). */
export const SALE_RULES = { noteMaxLength: 500 } as const;
const UPFRONT: readonly PaymentMethod[] = ['Cash', 'Pix', 'DebitCard'];

interface SaleModel {
  issuedAt: string;
  paymentMethod: PaymentMethod;
  installments: number;
  discount: number | null;
  note: string;
  maxInstallments: number;
}

interface SaleLine {
  readonly productId: number;
  readonly sku: string;
  readonly name: string;
  readonly unitCode: string | null;
  readonly unitPrice: number;
  readonly quantity: number;
  readonly discount: number;
}

const messages = SALES_STRINGS.validation;
const saleSchema = schema<SaleModel>((path) => {
  validate(path.installments, ({ value, valueOf }) => {
    const max = valueOf(path.maxInstallments);
    if (!Number.isInteger(value()) || value() < 1 || value() > max) {
      return { kind: 'range', message: messages.installments(max) };
    }
    return UPFRONT.includes(valueOf(path.paymentMethod)) && value() !== 1
      ? { kind: 'upfront', message: messages.upfront }
      : undefined;
  });
  validate(path.discount, ({ value }) =>
    (value() ?? 0) < 0 ? { kind: 'min', message: messages.discount } : undefined,
  );
  maxLength(path.note, SALE_RULES.noteMaxLength, {
    message: messages.noteMaxLength(SALE_RULES.noteMaxLength),
  });
});

const STATUS_TONE: Record<SaleDto['status'], StatusTone> = {
  Draft: 'warning',
  Confirmed: 'success',
  Cancelled: 'neutral',
};

function todayIso(): string {
  const now = new Date();
  return new Date(now.getTime() - now.getTimezoneOffset() * 60_000).toISOString().slice(0, 10);
}

function lineTotal(line: SaleLine): number {
  return Math.round((line.quantity * line.unitPrice - line.discount) * 100) / 100;
}

/** Venda: rascunho editável (cliente, pagamento e itens), confirmação e cancelamento. */
@Component({
  selector: 'erp-sale-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    HlmButton,
    HlmCard,
    HlmInput,
    HlmSelectImports,
    HlmSkeleton,
    HlmTextarea,
    FieldShell,
    FormBanner,
    FormHeader,
    Icon,
    LoadError,
    SectionCard,
    HlmTooltip,
    CurrencyPipe,
    DatePipe,
    FormField,
    MoneyInput,
    PartnerPicker,
    ProductPicker,
    StatusChip,
  ],
  templateUrl: './sale-form.html',
  styleUrl: './sale-form.scss',
})
export class SaleForm {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly confirm = inject(ConfirmService);
  private readonly notifier = inject(Notifier);

  readonly id = input<string>();

  protected readonly strings = SALES_STRINGS;
  protected readonly paymentMethods = Object.keys(SALES_STRINGS.paymentMethods) as PaymentMethod[];
  protected readonly canWrite = inject(AuthService).canWrite;
  protected readonly isEdit = computed(() => this.id() !== undefined);
  private readonly saleId = computed(() => {
    const id = Number(this.id());
    return Number.isInteger(id) && id > 0 ? id : null;
  });

  protected readonly sale = resource({
    params: () => this.saleId() ?? undefined,
    loader: ({ params }) => this.api.invoke(getSale, { id: params }),
  });
  protected readonly settings = resource({
    loader: () => this.api.invoke(getSaleSettings, {}),
  });
  protected readonly loaded = computed<SaleDto | null>(() =>
    this.sale.hasValue() ? this.sale.value() : null,
  );
  private readonly maxInstallments = computed(() =>
    this.settings.hasValue() ? this.settings.value().maxInstallments : 12,
  );
  protected readonly maxDiscountPercent = computed(() =>
    this.settings.hasValue() ? this.settings.value().maxDiscountPercent : null,
  );

  protected readonly partner = linkedSignal<PickedPartner | null>(() => {
    const sale = this.loaded();
    return sale
      ? { id: sale.partnerId, name: sale.partnerName, document: sale.partnerDocument }
      : null;
  });
  protected readonly lines = linkedSignal<readonly SaleLine[]>(() =>
    (this.loaded()?.items ?? []).map((item) => ({
      productId: item.productId,
      sku: item.sku,
      name: item.productName,
      unitCode: item.unitCode ?? null,
      unitPrice: item.unitPrice,
      quantity: item.quantity,
      discount: item.discount,
    })),
  );
  protected readonly model = linkedSignal<SaleModel>(() => {
    const sale = this.loaded();
    return {
      issuedAt: sale?.issuedAt ?? todayIso(),
      paymentMethod: sale?.paymentMethod ?? 'Pix',
      installments: sale?.installments ?? 1,
      discount: sale?.discount ?? null,
      note: sale?.note ?? '',
      maxInstallments: this.maxInstallments(),
    };
  });
  /** Só rascunho (ou venda nova) é editável, e só por quem grava. */
  protected readonly editable = computed(
    () => this.canWrite() && (!this.isEdit() || this.loaded()?.status === 'Draft'),
  );
  protected readonly saleForm = form(this.model, (path) => {
    apply(path, saleSchema);
    disabled(path, () => !this.editable());
  });

  protected readonly productToAdd = signal<PickedProduct | null>(null);
  protected readonly attempted = signal(false);
  protected readonly busy = signal(false);
  protected readonly conflict = signal(false);

  protected readonly loading = computed(
    () => this.isEdit() && this.sale.isLoading() && !this.sale.hasValue(),
  );
  protected readonly showSkeleton = delayedFlag(this.loading);
  protected readonly loadError = computed(
    () => this.isEdit() && (this.saleId() === null || this.sale.status() === 'error'),
  );

  protected readonly totals = computed(() => {
    const lines = this.lines();
    const gross = lines.reduce((sum, line) => sum + line.quantity * line.unitPrice, 0);
    const itemDiscounts = lines.reduce((sum, line) => sum + line.discount, 0);
    const discount = this.model().discount ?? 0;
    return { gross, itemDiscounts, discount, total: gross - itemDiscounts - discount };
  });
  /** Erros que não cabem num campo do formulário: cliente e itens. */
  protected readonly localErrors = computed(() => {
    const errors: string[] = [];
    if (!this.partner()) {
      errors.push(messages.partner);
    }
    const lines = this.lines();
    if (lines.length === 0) {
      errors.push(messages.items);
    }
    lines.forEach((line, index) => {
      if (!(line.quantity > 0)) {
        errors.push(`${index + 1}. ${line.name}: ${messages.quantity}`);
      } else if (line.discount < 0) {
        errors.push(`${index + 1}. ${line.name}: ${messages.discount}`);
      } else if (line.discount > line.quantity * line.unitPrice) {
        errors.push(`${index + 1}. ${line.name}: ${messages.itemDiscount}`);
      }
    });
    return errors;
  });
  protected readonly bannerErrors = computed(() => [
    ...(this.attempted() ? this.localErrors() : []),
    ...this.saleForm()
      .errors()
      .map((error) => error.message ?? ''),
  ]);

  protected readonly bannerMessages = computed(() =>
    this.bannerErrors().map((message) => ({ message })),
  );
  protected readonly lineTotal = lineTotal;
  protected readonly fieldError = fieldError;
  protected readonly paymentLabel = (method: PaymentMethod): string =>
    SALES_STRINGS.paymentMethods[method];

  protected setPaymentMethod(method: PaymentMethod | null | undefined): void {
    if (method) {
      this.saleForm.paymentMethod().value.set(method);
    }
  }

  protected statusTone(sale: SaleDto): StatusTone {
    return STATUS_TONE[sale.status];
  }

  protected addProduct(): void {
    const product = this.productToAdd();
    if (!product) {
      return;
    }
    const existing = this.lines().findIndex((line) => line.productId === product.id);
    if (existing >= 0) {
      this.updateLine(existing, 'quantity', this.lines()[existing].quantity + 1);
    } else {
      this.lines.update((lines) => [
        ...lines,
        {
          productId: product.id,
          sku: product.sku,
          name: product.name,
          unitCode: product.unitCode ?? null,
          unitPrice: product.salePrice ?? 0,
          quantity: 1,
          discount: 0,
        },
      ]);
    }
    this.productToAdd.set(null);
  }

  protected onLineInput(index: number, field: 'quantity' | 'discount', event: Event): void {
    const value = (event.target as HTMLInputElement).valueAsNumber;
    this.updateLine(index, field, Number.isNaN(value) ? 0 : value);
  }

  protected removeLine(index: number): void {
    this.lines.update((lines) => lines.filter((_, position) => position !== index));
  }

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    await this.persist();
  }

  protected async confirmSale(): Promise<void> {
    const actions = SALES_STRINGS.actions;
    const number = this.loaded()?.number ?? 0;
    if (!(await this.ask(actions.confirmTitle, actions.confirmMessage(number)))) {
      return;
    }
    const saved = await this.persist({ quiet: true });
    if (!saved) {
      return;
    }
    await this.run(
      () => this.api.invoke(confirmSale, { id: saved.id, body: { version: saved.version } }),
      actions.confirmed,
    );
  }

  protected async cancelSale(): Promise<void> {
    const sale = this.loaded();
    const actions = SALES_STRINGS.actions;
    if (!sale || !(await this.ask(actions.cancelTitle, actions.cancelMessage(sale.number)))) {
      return;
    }
    await this.run(
      () =>
        this.api.invoke(cancelSale, {
          id: sale.id,
          body: { reason: null, version: sale.version },
        }),
      actions.cancelled,
    );
  }

  protected reload(): void {
    this.conflict.set(false);
    this.sale.reload();
  }

  /** Grava o rascunho (cria ou atualiza). Devolve a venda gravada, ou null se não passou. */
  private async persist(options: { quiet?: boolean } = {}): Promise<SaleDto | null> {
    this.attempted.set(true);
    this.conflict.set(false);
    if (this.localErrors().length > 0) {
      return null;
    }
    let saved: SaleDto | null = null;
    this.busy.set(true);
    await submit(this.saleForm, async () => {
      const current = this.loaded();
      const body = this.toRequest(current?.version ?? null);
      try {
        if (current) {
          saved = await this.api.invoke(updateSale, { id: current.id, body });
          this.sale.set(saved);
          if (!options.quiet) {
            this.notifier.success(SALES_STRINGS.form.updated);
          }
        } else {
          saved = await this.api.invoke(createSale, { body });
          this.notifier.success(SALES_STRINGS.form.created(saved.number));
          await this.router.navigate(['..', 'edit', saved.id], {
            relativeTo: this.route,
            replaceUrl: true,
          });
        }
        return undefined;
      } catch (error) {
        const problem = toApiProblem(error);
        this.conflict.set(problem.code === API_ERROR_CODES.concurrencyConflict);
        return toServerErrors(this.saleForm, problem);
      }
    });
    this.busy.set(false);
    return saved;
  }

  private toRequest(version: number | null): SaveSaleRequest {
    const value = this.model();
    return {
      partnerId: this.partner()?.id ?? 0,
      paymentMethod: value.paymentMethod,
      installments: value.installments,
      discount: value.discount ?? 0,
      issuedAt: value.issuedAt || null,
      note: value.note.trim() || null,
      items: this.lines().map((line) => ({
        productId: line.productId,
        quantity: line.quantity,
        discount: line.discount,
      })),
      version,
    };
  }

  private updateLine(index: number, field: 'quantity' | 'discount', value: number): void {
    this.lines.update((lines) =>
      lines.map((line, position) => (position === index ? { ...line, [field]: value } : line)),
    );
  }

  private async ask(title: string, message: string): Promise<boolean> {
    return this.confirm.ask({
      title,
      message,
      confirmLabel: title,
      cancelLabel: SALES_STRINGS.actions.keep,
    });
  }

  private async run(action: () => Promise<SaleDto>, success: string): Promise<void> {
    this.busy.set(true);
    try {
      this.sale.set(await action());
      this.notifier.success(success);
    } catch (error) {
      this.notifier.error(toApiProblem(error).message);
    } finally {
      this.busy.set(false);
    }
  }
}
