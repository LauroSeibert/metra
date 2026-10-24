/**
 * Limites espelhados do servidor (Erp.Domain/Products e DecimalLimits). O servidor é a autoridade;
 * estes valores só antecipam o erro na tela. Mudou lá, mude aqui.
 */
export const PRODUCT_RULES = {
  skuMaxLength: 40,
  skuPattern: /^[A-Za-z0-9][A-Za-z0-9._-]*$/,
  nameMaxLength: 200,
  descriptionMaxLength: 1000,
  moneyScale: 2,
  quantityScale: 3,
} as const;

export const CATALOG_RULES = {
  categoryNameMaxLength: 100,
  unitCodeMaxLength: 10,
  unitNameMaxLength: 60,
} as const;

/** Conta as casas decimais de um número como digitado (1.5 → 1, 2.125 → 3). */
export function decimalPlaces(value: number): number {
  if (!Number.isFinite(value)) {
    return 0;
  }
  const [, fraction = ''] = String(value).split('.');
  return fraction.length;
}
