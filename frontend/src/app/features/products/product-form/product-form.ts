import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  afterNextRender,
  computed,
  ElementRef,
  inject,
  Injector,
  input,
  linkedSignal,
  resource,
  signal,
} from '@angular/core';
import { apply, disabled, form, FormField, submit } from '@angular/forms/signals';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCard } from '@spartan-ng/helm/card';
import { HlmComboboxImports } from '@spartan-ng/helm/combobox';
import {
  HlmEmpty,
  HlmEmptyContent,
  HlmEmptyHeader,
  HlmEmptyMedia,
  HlmEmptyTitle,
} from '@spartan-ng/helm/empty';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSkeleton } from '@spartan-ng/helm/skeleton';
import { HlmSwitch } from '@spartan-ng/helm/switch';
import { HlmTextarea } from '@spartan-ng/helm/textarea';
import { Router, RouterLink } from '@angular/router';
import { Api } from '../../../api/api';
import { activateProduct } from '../../../api/fn/products/activate-product';
import { createProduct } from '../../../api/fn/products/create-product';
import { deactivateProduct } from '../../../api/fn/products/deactivate-product';
import { getProduct } from '../../../api/fn/products/get-product';
import { updateProduct } from '../../../api/fn/products/update-product';
import { listProductCategories } from '../../../api/fn/product-categories/list-product-categories';
import { listUnits } from '../../../api/fn/units/list-units';
import { CreateProductRequest } from '../../../api/models/create-product-request';
import { AuthService } from '../../../core/auth/auth.service';
import { API_ERROR_CODES, toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { delayedFlag } from '../../../shared/data/delayed-flag';
import { ActionBar } from '../../../shared/ui/action-bar/action-bar';
import { FieldShell } from '../../../shared/ui/field-shell/field-shell';
import { Icon } from '../../../shared/ui/icon/icon';
import { MarginMeter } from '../../../shared/ui/margin-meter/margin-meter';
import { MoneyInput } from '../../../shared/ui/money-input/money-input';
import { SectionCard } from '../../../shared/ui/section-card/section-card';
import { StatusChip } from '../../../shared/ui/status-chip/status-chip';
import { PRODUCT_RULES } from '../product-rules';
import { PRODUCTS_STRINGS } from '../products.strings';
import {
  EMPTY_PRODUCT,
  ProductFormModel,
  productSchema,
  toProductFormModel,
} from './product-form.schema';

/** Cadastro e edição de produto. Sem `id` na rota, cria; com `id`, edita com versão otimista. */
@Component({
  selector: 'erp-product-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe,
    DatePipe,
    RouterLink,
    FormField,
    HlmButton,
    HlmCard,
    HlmComboboxImports,
    HlmDropdownMenuImports,
    HlmEmpty,
    HlmEmptyContent,
    HlmEmptyHeader,
    HlmEmptyMedia,
    HlmEmptyTitle,
    HlmInput,
    HlmSkeleton,
    HlmSwitch,
    HlmTextarea,
    ActionBar,
    FieldShell,
    Icon,
    MarginMeter,
    MoneyInput,
    SectionCard,
    StatusChip,
  ],
  templateUrl: './product-form.html',
  styleUrl: './product-form.scss',
})
export class ProductForm {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly notifier = inject(Notifier);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  /** Parâmetro `:id` da rota (withComponentInputBinding). */
  readonly id = input<string>();

  protected readonly strings = PRODUCTS_STRINGS;
  protected readonly isEdit = computed(() => this.id() !== undefined);
  protected readonly productId = computed(() => {
    const id = Number(this.id());
    return Number.isInteger(id) && id > 0 ? id : null;
  });

  protected readonly product = resource({
    params: () => this.productId() ?? undefined,
    loader: ({ params }) => this.api.invoke(getProduct, { id: params }),
  });
  protected readonly categories = resource({
    loader: () => this.api.invoke(listProductCategories, { pageSize: 100 }),
  });
  protected readonly units = resource({
    loader: () => this.api.invoke(listUnits, { pageSize: 100 }),
  });

  /** Recomeça do produto carregado sempre que ele (re)carrega. */
  protected readonly model = linkedSignal<ProductFormModel>(() =>
    this.product.hasValue() ? toProductFormModel(this.product.value()) : { ...EMPTY_PRODUCT },
  );
  protected readonly canWrite = inject(AuthService).canWrite;
  // Perfil Leitura vê o cadastro, mas não edita.
  protected readonly productForm = form(this.model, (path) => {
    apply(path, productSchema);
    disabled(path, () => !this.canWrite());
  });

  protected readonly conflict = signal(false);
  protected readonly categoryOptions = computed(() =>
    this.categories.hasValue() ? this.categories.value().items : [],
  );
  protected readonly unitOptions = computed(() =>
    this.units.hasValue() ? this.units.value().items : [],
  );
  protected readonly loadingProduct = computed(
    () => this.isEdit() && this.product.isLoading() && !this.product.hasValue(),
  );
  protected readonly showSkeleton = delayedFlag(this.loadingProduct);
  protected readonly loadError = computed(
    () => this.isEdit() && (this.productId() === null || this.product.status() === 'error'),
  );
  protected readonly formErrors = computed(() => this.productForm().errors());
  protected readonly margin = computed(() => {
    const { costPrice, salePrice } = this.model();
    return costPrice !== null && salePrice !== null && salePrice > 0
      ? (salePrice - costPrice) / salePrice
      : null;
  });

  protected readonly profit = computed(() => {
    const { costPrice, salePrice } = this.model();
    return costPrice !== null && salePrice !== null ? salePrice - costPrice : null;
  });
  protected readonly maxDescription = PRODUCT_RULES.descriptionMaxLength;

  /** Muda em relação ao que está salvo (ou ao formulário vazio, no cadastro). */
  protected readonly dirty = computed(() => {
    const saved = this.product.hasValue()
      ? toProductFormModel(this.product.value())
      : EMPTY_PRODUCT;
    return JSON.stringify(this.model()) !== JSON.stringify(saved);
  });
  protected readonly togglingActive = signal(false);

  // Categoria e unidade usam 0 como "nenhuma" (o combobox não aceita null como valor).
  protected readonly categoryLabel = (id: number): string =>
    id === 0
      ? this.strings.form.none
      : (this.categoryOptions().find((c) => c.id === id)?.name ?? '');
  protected readonly unitLabel = (id: number): string => {
    const unit = this.unitOptions().find((option) => option.id === id);
    return id === 0 ? this.strings.form.none : unit ? `${unit.code} · ${unit.name}` : '';
  };

  protected setCategory(id: number): void {
    this.productForm.categoryId().value.set(id || null);
  }

  protected setUnit(id: number): void {
    this.productForm.unitId().value.set(id || null);
  }

  /** Mensagem do primeiro erro do campo, depois que ele foi tocado ou o envio foi tentado. */
  protected errorOf(field: {
    touched(): boolean;
    errors(): readonly { message?: string }[];
  }): string | null {
    return field.touched() ? (field.errors()[0]?.message ?? null) : null;
  }

  /** Ativa ou inativa na hora (endpoints próprios), mantendo o que foi digitado e ainda não salvo. */
  protected async toggleActive(): Promise<void> {
    const current = this.product.hasValue() ? this.product.value() : null;
    if (!current || this.togglingActive()) {
      return;
    }
    this.togglingActive.set(true);
    try {
      const updated = await this.api.invoke(current.active ? deactivateProduct : activateProduct, {
        id: current.id,
      });
      const typed = this.model();
      this.product.set(updated);
      this.model.set(typed);
      this.notifier.success(
        updated.active ? PRODUCTS_STRINGS.list.activated : PRODUCTS_STRINGS.list.deactivated,
      );
    } catch (error) {
      this.notifier.error(toApiProblem(error).message);
    } finally {
      this.togglingActive.set(false);
    }
  }

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    this.conflict.set(false);

    const submitted = await submit(this.productForm, async () => {
      const request = this.toRequest();
      try {
        // Depois de salvar, continua na tela com os dados como o servidor gravou (SKU gerado,
        // versão nova). Cadastro novo passa para a URL de edição dele.
        const current = this.product.hasValue() ? this.product.value() : null;
        if (current) {
          const updated = await this.api.invoke(updateProduct, {
            id: current.id,
            body: { ...request, version: current.version },
          });
          this.product.set(updated);
          this.notifier.success(PRODUCTS_STRINGS.form.updated);
        } else {
          const created = await this.api.invoke(createProduct, { body: request });
          this.notifier.success(PRODUCTS_STRINGS.form.created);
          await this.router.navigate(['/products', 'edit', created.id], { replaceUrl: true });
        }
        return undefined;
      } catch (error) {
        const problem = toApiProblem(error);
        this.conflict.set(problem.code === API_ERROR_CODES.concurrencyConflict);
        return toServerErrors(this.productForm, problem, { 'product.sku_duplicate': 'sku' });
      }
    });
    if (!submitted) {
      this.focusFirstInvalid();
    }
  }

  protected reload(): void {
    this.conflict.set(false);
    this.product.reload();
  }

  private focusFirstInvalid(): void {
    afterNextRender(
      () => this.host.nativeElement.querySelector<HTMLElement>('[aria-invalid="true"]')?.focus(),
      { injector: this.injector },
    );
  }

  private toRequest(): CreateProductRequest {
    const value = this.model();
    return {
      sku: value.sku.trim() || null,
      name: value.name.trim(),
      description: value.description.trim() || null,
      categoryId: value.categoryId,
      unitId: value.unitId,
      costPrice: value.costPrice ?? 0,
      salePrice: value.salePrice ?? 0,
      minStock: value.minStock ?? 0,
    };
  }
}
