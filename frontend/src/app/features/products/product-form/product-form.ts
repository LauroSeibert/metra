import { CurrencyPipe, DatePipe, PercentPipe } from '@angular/common';
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
import { apply, disabled, form, FormField, submit } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { Api } from '../../../api/api';
import { createProduct } from '../../../api/fn/products/create-product';
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
import { Card } from '../../../shared/ui/card/card';
import { MoneyInput } from '../../../shared/ui/money-input/money-input';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { StatusChip } from '../../../shared/ui/status-chip/status-chip';
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
    PercentPipe,
    RouterLink,
    FormField,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    Card,
    MoneyInput,
    PageHeader,
    StatusChip,
  ],
  templateUrl: './product-form.html',
  styleUrl: './product-form.scss',
})
export class ProductForm {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly notifier = inject(Notifier);

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

  /** Largura da barra de margem (0–100), limitada para margens negativas ou acima de 100%. */
  protected readonly marginBar = computed(() => {
    const margin = this.margin();
    return margin === null ? 0 : Math.min(100, Math.max(0, Math.abs(margin) * 100));
  });
  protected readonly profit = computed(() => {
    const { costPrice, salePrice } = this.model();
    return costPrice !== null && salePrice !== null ? salePrice - costPrice : null;
  });
  protected readonly selectedCategory = computed(
    () => this.categoryOptions().find((category) => category.id === this.model().categoryId)?.name,
  );
  protected readonly selectedUnit = computed(() => {
    const unit = this.unitOptions().find((option) => option.id === this.model().unitId);
    return unit ? `${unit.code} · ${unit.name}` : undefined;
  });

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    this.conflict.set(false);

    await submit(this.productForm, async () => {
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
  }

  protected reload(): void {
    this.conflict.set(false);
    this.product.reload();
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
