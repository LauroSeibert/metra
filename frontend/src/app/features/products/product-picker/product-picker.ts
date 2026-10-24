import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
  model,
  resource,
} from '@angular/core';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Api } from '../../../api/api';
import { listProducts } from '../../../api/fn/products/list-products';
import { debounced } from '../../../shared/data/debounced';
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
 * Busca de produto por prefixo de SKU ou nome, com sugestões enquanto digita. Usado no lançamento de
 * estoque e nos itens da venda. Só sugere produtos ativos.
 */
@Component({
  selector: 'erp-product-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatAutocompleteModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  template: `
    <mat-form-field [subscriptSizing]="subscriptSizing()">
      <mat-label>{{ label() }}</mat-label>
      <mat-icon matPrefix>search</mat-icon>
      <input
        matInput
        autocomplete="off"
        [placeholder]="strings.placeholder"
        [matAutocomplete]="auto"
        [value]="text()"
        [disabled]="disabled()"
        (input)="onInput($event)"
      />
      @if (selected() && !disabled()) {
        <button
          matIconButton
          matSuffix
          type="button"
          [attr.aria-label]="strings.clear"
          (click)="clear()"
        >
          <mat-icon>close</mat-icon>
        </button>
      }
      <mat-autocomplete
        #auto="matAutocomplete"
        [displayWith]="display"
        (optionSelected)="pick($event.option.value)"
      >
        @for (product of options(); track product.id) {
          <mat-option [value]="product">
            <span class="sku">{{ product.sku }}</span> · {{ product.name }}
          </mat-option>
        } @empty {
          @if (term()) {
            <mat-option disabled>{{ strings.noResults }}</mat-option>
          }
        }
      </mat-autocomplete>
      @if (error()) {
        <mat-hint class="error">{{ error() }}</mat-hint>
      }
    </mat-form-field>
  `,
  styles: `
    :host {
      display: block;
    }

    mat-form-field {
      width: 100%;
    }

    .sku {
      color: var(--erp-text-subtle);
      font-family: ui-monospace, 'Cascadia Mono', monospace;
      font-size: var(--erp-font-size-sm);
    }

    .error {
      color: var(--erp-danger);
    }
  `,
})
export class ProductPicker {
  private readonly api = inject(Api);

  /** Produto escolhido (ou já definido por quem abriu a tela). */
  readonly selected = model<PickedProduct | null>(null);
  readonly label = input<string>(PRODUCTS_STRINGS.picker.label);
  readonly disabled = input(false);
  /** Mensagem de erro do campo (validação de quem usa o seletor). */
  readonly error = input<string | null>(null);
  readonly subscriptSizing = input<'fixed' | 'dynamic'>('fixed');

  protected readonly strings = PRODUCTS_STRINGS.picker;
  /** Texto digitado; volta a ser o produto escolhido quando ele muda. */
  protected readonly text = linkedSignal(() => this.display(this.selected()));
  private readonly typed = linkedSignal<PickedProduct | null, string>({
    source: this.selected,
    computation: () => '',
  });
  protected readonly term = debounced(computed(() => this.typed().trim()));

  private readonly results = resource({
    params: () => this.term() || undefined,
    loader: ({ params }) =>
      this.api.invoke(listProducts, { search: params, active: true, pageSize: 10 }),
  });
  protected readonly options = computed(() =>
    this.results.hasValue() && this.term() ? this.results.value().items : [],
  );

  protected readonly display = (product: PickedProduct | string | null): string =>
    typeof product === 'string' ? product : product ? `${product.sku} · ${product.name}` : '';

  protected onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.text.set(value);
    this.typed.set(value);
    if (this.selected()) {
      this.selected.set(null);
    }
  }

  protected pick(product: PickedProduct): void {
    this.selected.set({
      id: product.id,
      sku: product.sku,
      name: product.name,
      unitCode: product.unitCode,
      salePrice: product.salePrice,
    });
  }

  protected clear(): void {
    this.selected.set(null);
    this.text.set('');
    this.typed.set('');
  }
}
