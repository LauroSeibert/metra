import { maxLength, min, pattern, required, schema, validate } from '@angular/forms/signals';
import { ProductDto } from '../../../api/models/product-dto';
import { decimalPlaces, PRODUCT_RULES } from '../product-rules';
import { PRODUCTS_STRINGS } from '../products.strings';

/** Modelo do formulário. Seleções e números começam vazios (null) até o usuário preencher. */
export interface ProductFormModel {
  sku: string;
  name: string;
  description: string;
  categoryId: number | null;
  unitId: number | null;
  costPrice: number | null;
  salePrice: number | null;
  minStock: number | null;
}

export const EMPTY_PRODUCT: ProductFormModel = {
  sku: '',
  name: '',
  description: '',
  categoryId: null,
  unitId: null,
  costPrice: null,
  salePrice: null,
  minStock: 0,
};

export function toProductFormModel(product: ProductDto): ProductFormModel {
  return {
    sku: product.sku,
    name: product.name,
    description: product.description ?? '',
    categoryId: product.categoryId,
    unitId: product.unitId,
    costPrice: product.costPrice,
    salePrice: product.salePrice,
    minStock: product.minStock,
  };
}

const messages = PRODUCTS_STRINGS.validation;

/** Mesmas regras do servidor (Product.Apply), para o erro aparecer antes do envio. */
export const productSchema = schema<ProductFormModel>((path) => {
  // SKU opcional: em branco, o servidor gera a partir da categoria e do id.
  maxLength(path.sku, PRODUCT_RULES.skuMaxLength, {
    message: messages.maxLength(PRODUCT_RULES.skuMaxLength),
  });
  pattern(path.sku, PRODUCT_RULES.skuPattern, { message: messages.skuPattern });

  required(path.name, { message: messages.required });
  maxLength(path.name, PRODUCT_RULES.nameMaxLength, {
    message: messages.maxLength(PRODUCT_RULES.nameMaxLength),
  });
  maxLength(path.description, PRODUCT_RULES.descriptionMaxLength, {
    message: messages.maxLength(PRODUCT_RULES.descriptionMaxLength),
  });

  for (const [field, scale] of [
    [path.costPrice, PRODUCT_RULES.moneyScale],
    [path.salePrice, PRODUCT_RULES.moneyScale],
    [path.minStock, PRODUCT_RULES.quantityScale],
  ] as const) {
    required(field, { message: messages.required });
    min(field, 0, { message: messages.nonNegative });
    validate(field, ({ value }) => {
      const current = value();
      return current !== null && decimalPlaces(current) > scale
        ? { kind: 'scale', message: messages.decimals(scale) }
        : undefined;
    });
  }
});
