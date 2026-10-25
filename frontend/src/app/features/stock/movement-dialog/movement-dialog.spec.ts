import { WritableSignal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DIALOG_DATA } from '@angular/cdk/dialog';
import { BrnDialogRef } from '@spartan-ng/brain/dialog';
import { createStockMovement } from '../../../api/fn/stock/create-stock-movement';
import { FakeApi } from '../../../shared/testing/fake-api';
import { STOCK_STRINGS } from '../stock.strings';
import { MovementDialog, MovementDialogData } from './movement-dialog';

interface Model {
  type: 'Entry' | 'Exit' | 'Adjustment';
  quantity: number | null;
  unitCost: number | null;
  note: string;
}

describe('MovementDialog', () => {
  let api: FakeApi;
  let fixture: ComponentFixture<MovementDialog>;
  let element: HTMLElement;
  let close: ReturnType<typeof vi.fn>;

  const DATA: MovementDialogData = {
    product: { id: 4, sku: 'CAN-004', name: 'Caneta', unitCode: 'UN' },
    currentQuantity: 10,
  };

  async function render(data: MovementDialogData = DATA): Promise<void> {
    close = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        api.provide(),
        { provide: DIALOG_DATA, useValue: data },
        { provide: BrnDialogRef, useValue: { close } },
      ],
    });
    fixture = TestBed.createComponent(MovementDialog);
    element = fixture.nativeElement as HTMLElement;
    await settle();
  }

  async function settle(): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
  }

  function model(): WritableSignal<Model> {
    return (fixture.componentInstance as unknown as { model: WritableSignal<Model> }).model;
  }

  async function submit(): Promise<void> {
    element.querySelector('form')!.dispatchEvent(new Event('submit'));
    await settle();
    await settle();
  }

  beforeEach(() => {
    api = new FakeApi().on(createStockMovement, () => ({}));
  });

  it('records an entry with its unit cost and closes', async () => {
    await render();
    model().set({ type: 'Entry', quantity: 5, unitCost: 2.5, note: '' });

    await submit();

    expect(api.callsTo(createStockMovement)).toEqual([
      {
        body: {
          productId: 4,
          type: 'Entry',
          quantity: 5,
          unitCost: 2.5,
          occurredAt: null,
          note: null,
        },
      },
    ]);
    expect(close).toHaveBeenCalledWith(true);
  });

  it('shows the current balance and sends the count on an adjustment, without unit cost', async () => {
    await render();
    model().set({ type: 'Adjustment', quantity: 7, unitCost: 3, note: 'Contagem de outubro' });
    await settle();

    expect(element.textContent).toContain(STOCK_STRINGS.dialog.current('10'));
    expect(element.textContent).toContain(STOCK_STRINGS.dialog.counted);
    expect(element.querySelector('erp-money-input')).toBeNull();

    await submit();

    expect(api.callsTo(createStockMovement)[0]).toEqual({
      body: expect.objectContaining({ type: 'Adjustment', quantity: 7, unitCost: null }),
    });
  });

  it('requires a reason on adjustments and a positive quantity on exits', async () => {
    await render();
    model().set({ type: 'Adjustment', quantity: 7, unitCost: null, note: ' ' });
    await submit();
    model().set({ type: 'Exit', quantity: 0, unitCost: null, note: '' });
    await submit();

    expect(api.callsTo(createStockMovement)).toEqual([]);
    expect(element.textContent).toContain(STOCK_STRINGS.validation.positive);
  });

  it('asks for a product when none was given', async () => {
    await render({ product: null, currentQuantity: null });
    model().set({ type: 'Entry', quantity: 1, unitCost: null, note: '' });

    await submit();

    expect(api.callsTo(createStockMovement)).toEqual([]);
    expect(element.textContent).toContain(STOCK_STRINGS.validation.product);
  });
});
