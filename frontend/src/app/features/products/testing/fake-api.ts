import { PagedResultOfProductCategoryDto } from '../../../api/models/paged-result-of-product-category-dto';
import { PagedResultOfProductListItemDto } from '../../../api/models/paged-result-of-product-list-item-dto';
import { PagedResultOfUnitDto } from '../../../api/models/paged-result-of-unit-dto';
import { ProductListItemDto } from '../../../api/models/product-list-item-dto';

export { FakeApi, page } from '../../../shared/testing/fake-api';
import { page } from '../../../shared/testing/fake-api';

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
