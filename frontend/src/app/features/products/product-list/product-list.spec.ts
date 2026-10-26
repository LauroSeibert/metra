import { HttpErrorResponse } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { listProductCategories } from '../../../api/fn/product-categories/list-product-categories';
import { deactivateProduct } from '../../../api/fn/products/deactivate-product';
import { listProducts, ListProducts$Params } from '../../../api/fn/products/list-products';
import { Role } from '../../../core/auth/auth.service';
import { signInAs } from '../../../core/auth/testing';
import { PRODUCTS_STRINGS } from '../products.strings';
import { CATEGORIES, FakeApi, page, PRODUCTS, productItem } from '../testing/fake-api';
import { ProductList } from './product-list';

describe('ProductList', () => {
  let api: FakeApi;
  let fixture: ComponentFixture<ProductList>;
  let element: HTMLElement;

  async function render(role: Role = 'Operator'): Promise<void> {
    TestBed.configureTestingModule({ providers: [api.provide(), provideRouter([])] });
    signInAs(role);
    fixture = TestBed.createComponent(ProductList);
    element = fixture.nativeElement as HTMLElement;
    await settle();
  }

  async function settle(): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function lastListParams(): ListProducts$Params {
    return api
      .callsTo(listProducts)
      .filter((params) => (params as ListProducts$Params).pageSize !== 1)
      .at(-1) as ListProducts$Params;
  }

  beforeEach(() => {
    api = new FakeApi()
      .on(listProducts, () => PRODUCTS)
      .on(listProductCategories, () => CATEGORIES);
  });

  it('loads the first page sorted by code and shows the rows', async () => {
    await render();

    expect(lastListParams()).toEqual({
      search: undefined,
      categoryId: undefined,
      active: undefined,
      sort: 'code',
      page: 1,
      pageSize: 20,
    });
    expect(element.textContent).toContain('CAN-001');
    expect(element.textContent).toContain('Caneta azul');
    expect(element.textContent).toContain(PRODUCTS_STRINGS.status.activeChip);
  });

  it('hides create, edit and deactivate actions from the read-only profile', async () => {
    await render('Viewer');

    expect(element.textContent).not.toContain(PRODUCTS_STRINGS.list.newProduct);
    expect(element.querySelector(`a[aria-label^="${PRODUCTS_STRINGS.list.view}"]`)).not.toBeNull();
    expect(
      element.querySelector(`button[aria-label^="${PRODUCTS_STRINGS.list.deactivate}"]`),
    ).toBeNull();
  });

  it('starts with the page size chosen in the profile', async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({ providers: [api.provide(), provideRouter([])] });
    signInAs('Operator', { pageSize: 50 });
    fixture = TestBed.createComponent(ProductList);
    await settle();

    expect(lastListParams().pageSize).toBe(50);
  });

  it('filters inactive products through the KPI strip', async () => {
    await render();

    const inactive = Array.from(
      element.querySelectorAll<HTMLButtonElement>('erp-kpi-strip button.cell'),
    ).find(
      (button) =>
        button.querySelector('.label')?.textContent?.trim() === PRODUCTS_STRINGS.status.inactive,
    );
    inactive?.click();
    await settle();

    expect(lastListParams().active).toBe(false);
    expect(lastListParams().page).toBe(1);
  });

  it('searches after the debounce', async () => {
    await render();
    const input = element.querySelector<HTMLInputElement>('input[type="search"]')!;

    input.value = 'can';
    input.dispatchEvent(new Event('input'));
    await new Promise((resolve) => setTimeout(resolve, 350));
    await settle();

    expect(lastListParams().search).toBe('can');
  });

  it('shows the empty message when nothing matches', async () => {
    api.on(listProducts, () => page([]));
    await render();

    expect(element.textContent).toContain(PRODUCTS_STRINGS.list.empty);
  });

  it('shows the error state when the API fails', async () => {
    api.on(listProducts, () => Promise.reject(new HttpErrorResponse({ status: 500 })));
    await render();

    expect(element.textContent).toContain(PRODUCTS_STRINGS.list.loadError);
  });

  it('deactivates a product and reloads the list', async () => {
    api.on(deactivateProduct, () => productItem({ active: false }));
    await render();
    const callsBefore = api.callsTo(listProducts).length;

    element
      .querySelector<HTMLButtonElement>(`button[aria-label^="${PRODUCTS_STRINGS.list.deactivate}"]`)
      ?.click();
    await settle();
    await settle();

    expect(api.callsTo(deactivateProduct)).toEqual([{ id: 1 }]);
    expect(api.callsTo(listProducts).length).toBeGreaterThan(callsBefore);
  });
});
