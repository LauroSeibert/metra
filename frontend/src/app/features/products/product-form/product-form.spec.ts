import { HttpErrorResponse } from '@angular/common/http';
import { WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { createProduct } from '../../../api/fn/products/create-product';
import { getProduct } from '../../../api/fn/products/get-product';
import { updateProduct } from '../../../api/fn/products/update-product';
import { listProductCategories } from '../../../api/fn/product-categories/list-product-categories';
import { listUnits } from '../../../api/fn/units/list-units';
import { ProductDto } from '../../../api/models/product-dto';
import { signInAs } from '../../../core/auth/testing';
import { PRODUCTS_STRINGS } from '../products.strings';
import { CATEGORIES, FakeApi, UNITS } from '../testing/fake-api';
import { ProductForm } from './product-form';
import { ProductFormModel } from './product-form.schema';

const VALID: ProductFormModel = {
  sku: 'can-002',
  name: 'Caneta preta',
  description: '',
  categoryId: 10,
  unitId: 20,
  costPrice: 1.5,
  salePrice: 3.9,
  minStock: 0,
};

const EXISTING: ProductDto = {
  id: 7,
  sku: 'CAN-007',
  name: 'Caneta verde',
  description: 'Tinta gel',
  categoryId: 10,
  categoryName: 'Papelaria',
  unitId: 20,
  unitCode: 'UN',
  unitName: 'Unidade',
  costPrice: 2,
  salePrice: 5,
  minStock: 3,
  active: true,
  version: 4,
  createdAt: '2026-10-23T12:00:00Z',
  updatedAt: '2026-10-23T12:00:00Z',
};

describe('ProductForm', () => {
  let api: FakeApi;
  let fixture: ComponentFixture<ProductForm>;
  let element: HTMLElement;

  async function render(id?: string): Promise<void> {
    TestBed.configureTestingModule({ providers: [api.provide(), provideRouter([])] });
    signInAs();
    vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(ProductForm);
    if (id) {
      fixture.componentRef.setInput('id', id);
    }
    element = fixture.nativeElement as HTMLElement;
    await settle();
  }

  async function settle(): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function model(): WritableSignal<ProductFormModel> {
    return (fixture.componentInstance as unknown as { model: WritableSignal<ProductFormModel> })
      .model;
  }

  async function submit(): Promise<void> {
    element.querySelector<HTMLFormElement>('form')!.dispatchEvent(new Event('submit'));
    await settle();
    await settle();
  }

  beforeEach(() => {
    api = new FakeApi().on(listProductCategories, () => CATEGORIES).on(listUnits, () => UNITS);
  });

  it('blocks submission and shows required errors when empty', async () => {
    await render();

    await submit();

    expect(api.callsTo(createProduct)).toEqual([]);
    expect(element.textContent).toContain(PRODUCTS_STRINGS.validation.required);
  });

  it('rejects negative prices and extra decimals before calling the API', async () => {
    await render();
    model().set({ ...VALID, salePrice: -1, costPrice: 1.005 });

    await submit();

    expect(api.callsTo(createProduct)).toEqual([]);
    expect(element.textContent).toContain(PRODUCTS_STRINGS.validation.nonNegative);
    expect(element.textContent).toContain(PRODUCTS_STRINGS.validation.decimals(2));
  });

  it('creates the product with trimmed values', async () => {
    api.on(createProduct, () => ({ ...EXISTING, id: 8 }));
    await render();
    model().set({ ...VALID, name: '  Caneta preta  ' });

    await submit();

    expect(api.callsTo(createProduct)).toEqual([
      {
        body: {
          sku: 'can-002',
          name: 'Caneta preta',
          description: null,
          categoryId: 10,
          unitId: 20,
          costPrice: 1.5,
          salePrice: 3.9,
          minStock: 0,
        },
      },
    ]);
  });

  it('sends a blank SKU as null so the server generates it', async () => {
    api.on(createProduct, () => ({ ...EXISTING, id: 9 }));
    await render();
    model().set({ ...VALID, sku: '' });

    await submit();

    expect(api.callsTo(createProduct)).toEqual([
      { body: expect.objectContaining({ sku: null, name: VALID.name }) },
    ]);
  });

  it('shows a duplicate SKU from the server under the SKU field', async () => {
    api.on(createProduct, () =>
      Promise.reject(
        new HttpErrorResponse({
          status: 409,
          error: {
            code: 'product.sku_duplicate',
            detail: 'Já existe um produto com o SKU CAN-002.',
          },
        }),
      ),
    );
    await render();
    model().set(VALID);

    await submit();

    const skuField = Array.from(element.querySelectorAll('erp-field-shell')).find((shell) =>
      shell.textContent?.includes(PRODUCTS_STRINGS.form.sku),
    );
    expect(skuField?.textContent).toContain('Já existe um produto com o SKU CAN-002.');
  });

  it('loads the product for editing and sends its version on update', async () => {
    api.on(getProduct, () => EXISTING).on(updateProduct, () => ({ ...EXISTING, version: 5 }));
    await render('7');

    expect(model()().sku).toBe('CAN-007');
    model().update((value) => ({ ...value, name: 'Caneta verde clara' }));
    await submit();

    expect(api.callsTo(updateProduct)).toEqual([
      expect.objectContaining({
        id: 7,
        body: expect.objectContaining({ name: 'Caneta verde clara', version: 4 }),
      }),
    ]);
  });

  it('stays on the screen after an update, with the saved data and the new version', async () => {
    api
      .on(getProduct, () => EXISTING)
      .on(updateProduct, () => ({ ...EXISTING, sku: 'PAP-000007', version: 5 }));
    await render('7');

    model().update((value) => ({ ...value, sku: '' }));
    await submit();
    await submit();

    expect(TestBed.inject(Router).navigate).not.toHaveBeenCalled();
    expect(model()().sku).toBe('PAP-000007');
    expect(api.callsTo(updateProduct)[1]).toEqual(
      expect.objectContaining({ body: expect.objectContaining({ version: 5 }) }),
    );
  });

  it('opens the edit URL of a newly created product instead of going back to the list', async () => {
    api.on(createProduct, () => ({ ...EXISTING, id: 8 }));
    await render();
    model().set(VALID);

    await submit();

    expect(TestBed.inject(Router).navigate).toHaveBeenCalledWith(['/products', 'edit', 8], {
      replaceUrl: true,
    });
  });

  it('accepts a product without category and unit', async () => {
    api.on(createProduct, () => ({ ...EXISTING, id: 9 }));
    await render();
    model().set({ ...VALID, categoryId: null, unitId: null });

    await submit();

    expect(api.callsTo(createProduct)).toEqual([
      { body: expect.objectContaining({ categoryId: null, unitId: null }) },
    ]);
  });

  it('fills money fields from the cents as the user types', async () => {
    await render();
    const costInput = element.querySelectorAll<HTMLInputElement>('erp-money-input input')[0];

    for (const typed of ['1', '10', '100']) {
      costInput.value = typed === '1' ? '1' : `${costInput.value}0`;
      costInput.dispatchEvent(new Event('input'));
    }
    await settle();

    expect(model()().costPrice).toBe(1);
    expect(costInput.value.replace(/\s/g, ' ')).toBe('R$ 1,00');
  });

  it('offers to reload when the server reports a concurrency conflict', async () => {
    api
      .on(getProduct, () => EXISTING)
      .on(updateProduct, () =>
        Promise.reject(
          new HttpErrorResponse({
            status: 409,
            error: {
              code: 'concurrency_conflict',
              detail: 'O registro foi alterado por outra pessoa.',
            },
          }),
        ),
      );
    await render('7');

    await submit();

    expect(element.querySelector('.banner')?.textContent).toContain('alterado por outra pessoa');
    expect(element.querySelector('.banner button')?.textContent).toContain(
      PRODUCTS_STRINGS.form.reload,
    );
  });
});
