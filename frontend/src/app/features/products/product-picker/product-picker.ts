import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  model,
  resource,
  signal,
} from '@angular/core';
import { Api } from '../../../api/api';
import { listProducts } from '../../../api/fn/products/list-products';
import { debounced } from '../../../shared/data/debounced';
import { SearchPicker } from '../../../shared/ui/search-picker/search-picker';
import { PRODUCTS_STRINGS } from '../products.strings';

/** Produto escolhido: o mínimo para mostrar e para gravar o id. */
export interface PickedProduct {
  readonly id: number;
  readonly sku: string;
  readonly name: string;
  readonly unitCode?: string | null;
  readonly salePrice?: number;
}

/**
 * Busca de produto por trecho de SKU ou nome, com sugestões enquanto digita. Usado no lançamento de
 * estoque e nos itens da venda. Só sugere produtos ativos.
 */
@Component({
  selector: 'erp-product-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SearchPicker],
  host: { class: 'block min-w-0' },
  template: `
    <erp-search-picker
      [label]="label()"
      [items]="options()"
      [key]="key"
      [itemToString]="display"
      [(selected)]="selected"
      [searchPlaceholder]="strings.placeholder"
      [noResults]="strings.noResults"
      [clearLabel]="strings.clear"
      [inline]="inline()"
      [required]="required()"
      [disabled]="disabled()"
      [error]="error()"
      (search)="typed.set($event)"
    >
      <ng-template let-product>
        <span class="flex w-full items-baseline gap-3">
          <span class="shrink-0 font-mono text-label text-muted-foreground">{{ product.sku }}</span>
          <span class="truncate">{{ product.name }}</span>
        </span>
      </ng-template>
    </erp-search-picker>
  `,
})
export class ProductPicker {
  private readonly api = inject(Api);

  /** Produto escolhido (ou já definido por quem abriu a tela). */
  readonly selected = model<PickedProduct | null>(null);
  readonly label = input<string>(PRODUCTS_STRINGS.picker.label);
  readonly inline = input(false);
  readonly required = input(false);
  readonly disabled = input(false);
  /** Mensagem de erro do campo (validação de quem usa o seletor). */
  readonly error = input<string | null>(null);

  protected readonly strings = PRODUCTS_STRINGS.picker;
  protected readonly typed = signal('');
  private readonly term = debounced(computed(() => this.typed().trim()));

  private readonly results = resource({
    params: () => this.term() || undefined,
    loader: ({ params }) =>
      this.api.invoke(listProducts, { search: params, active: true, pageSize: 10 }),
  });
  protected readonly options = computed<readonly PickedProduct[]>(() =>
    this.results.hasValue() && this.term()
      ? this.results.value().items.map((product) => ({
          id: product.id,
          sku: product.sku,
          name: product.name,
          unitCode: product.unitCode,
          salePrice: product.salePrice,
        }))
      : [],
  );

  protected readonly key = (product: PickedProduct): number => product.id;
  protected readonly display = (product: PickedProduct): string =>
    `${product.sku} · ${product.name}`;
}
