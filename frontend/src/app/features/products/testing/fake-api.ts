import { Provider } from '@angular/core';
import { Api } from '../../../api/api';
import { PagedResultOfProductCategoryDto } from '../../../api/models/paged-result-of-product-category-dto';
import { PagedResultOfProductListItemDto } from '../../../api/models/paged-result-of-product-list-item-dto';
import { PagedResultOfUnitDto } from '../../../api/models/paged-result-of-unit-dto';
import { ProductListItemDto } from '../../../api/models/product-list-item-dto';

type ApiFunction = (...args: never[]) => unknown;
type Handler = (params: unknown) => unknown;

/**
 * Api falsa para testes de tela: cada função gerada (listProducts, createProduct...) recebe uma
 * resposta. Guarda as chamadas para os testes conferirem os parâmetros enviados.
 */
export class FakeApi {
  readonly calls: { fn: ApiFunction; params: unknown }[] = [];
  private readonly handlers = new Map<ApiFunction, Handler>();

  on(fn: ApiFunction, handler: Handler): this {
    this.handlers.set(fn, handler);
    return this;
  }

  callsTo(fn: ApiFunction): unknown[] {
    return this.calls.filter((call) => call.fn === fn).map((call) => call.params);
  }

  readonly invoke = async (fn: ApiFunction, params?: unknown): Promise<unknown> => {
    this.calls.push({ fn, params });
    const handler = this.handlers.get(fn);
    if (!handler) {
      throw new Error(`FakeApi sem resposta para ${fn.name}`);
    }
    return handler(params);
  };

  provide(): Provider {
    return { provide: Api, useValue: this };
  }
}

export function page<T>(items: T[]): {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
} {
  return { items, page: 1, pageSize: 20, totalCount: items.length };
}

export function productItem(overrides: Partial<ProductListItemDto> = {}): ProductListItemDto {
  return {
    id: 1,
    sku: 'CAN-001',
    name: 'Caneta azul',
    description: null,
    categoryId: 10,
    categoryName: 'Papelaria',
    unitId: 20,
    unitCode: 'UN',
    costPrice: 1.5,
    salePrice: 3.9,
    minStock: 10,
    active: true,
    updatedAt: '2026-10-23T12:00:00Z',
    ...overrides,
  };
}

export const CATEGORIES: PagedResultOfProductCategoryDto = page([
  { id: 10, name: 'Papelaria', productCount: 1, version: 1 },
]);

export const UNITS: PagedResultOfUnitDto = page([
  { id: 20, code: 'UN', name: 'Unidade', productCount: 1, version: 1 },
]);

export const PRODUCTS: PagedResultOfProductListItemDto = page([productItem()]);
