import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import {
  listStockMovements,
  ListStockMovements$Params,
} from '../../../api/fn/stock/list-stock-movements';
import { StockMovementDto } from '../../../api/models/stock-movement-dto';
import { Role } from '../../../core/auth/auth.service';
import { signInAs, signOut } from '../../../core/auth/testing';
import { FakeApi } from '../../../shared/testing/fake-api';
import { STOCK_STRINGS } from '../stock.strings';
import { MovementHistory } from './movement-history';

function movement(id: number, overrides: Partial<StockMovementDto> = {}): StockMovementDto {
  return {
    id,
    productId: 4,
    sku: 'CAN-004',
    productName: 'Caneta',
    unitCode: 'UN',
    type: 'Entry',
    quantity: 5,
    unitCost: 2,
    occurredAt: '2026-10-24T12:00:00Z',
    referenceType: null,
    referenceId: null,
    userId: 1,
    userName: 'Operador',
    note: null,
    reversedById: null,
    createdAt: '2026-10-24T12:00:00Z',
    ...overrides,
  };
}

describe('MovementHistory', () => {
  let api: FakeApi;
  let fixture: ComponentFixture<MovementHistory>;
  let element: HTMLElement;

  async function render(role: Role = 'Operator'): Promise<void> {
    TestBed.configureTestingModule({ providers: [api.provide(), provideRouter([])] });
    signInAs(role);
    fixture = TestBed.createComponent(MovementHistory);
    element = fixture.nativeElement as HTMLElement;
    await settle();
  }

  async function settle(): Promise<void> {
    await fixture.whenStable();
    fixture.detectChanges();
  }

  afterEach(() => signOut());

  it('appends the next page using the cursor, and stops at the end', async () => {
    api = new FakeApi().on(listStockMovements, (params) =>
      (params as ListStockMovements$Params).cursor === 'c1'
        ? { items: [movement(1)], nextCursor: null }
        : { items: [movement(3), movement(2)], nextCursor: 'c1' },
    );
    await render();
    expect(element.querySelectorAll('tr.row').length).toBe(2);

    const more = Array.from(element.querySelectorAll<HTMLButtonElement>('.more button')).find(
      (button) => button.textContent?.includes(STOCK_STRINGS.history.loadMore),
    )!;
    more.click();
    await settle();
    await settle();

    expect(element.querySelectorAll('tr.row').length).toBe(3);
    expect(api.callsTo(listStockMovements).at(-1)).toEqual(
      expect.objectContaining({ cursor: 'c1' }),
    );
    expect(element.querySelector('.more')?.textContent).toContain(STOCK_STRINGS.history.end);
  });

  it('offers reversal only for writers and only for movements not yet reversed', async () => {
    api = new FakeApi().on(listStockMovements, () => ({
      items: [
        movement(3),
        movement(2, { reversedById: 9 }),
        movement(9, { type: 'Reversal', quantity: -5, referenceId: 2 }),
      ],
      nextCursor: null,
    }));
    await render();

    expect(element.querySelectorAll('.erp-row-action.danger').length).toBe(1);
    expect(element.textContent).toContain(STOCK_STRINGS.history.reversed);
    expect(element.textContent).toContain(STOCK_STRINGS.history.reversalOf(2));
  });

  it('hides reversal from the read-only profile', async () => {
    api = new FakeApi().on(listStockMovements, () => ({ items: [movement(3)], nextCursor: null }));
    await render('Viewer');

    expect(element.querySelector('.erp-row-action.danger')).toBeNull();
  });
});
