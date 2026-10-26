import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { getFinancialSummary } from '../../../api/fn/financial-entries/get-financial-summary';
import {
  listFinancialEntries,
  ListFinancialEntries$Params,
} from '../../../api/fn/financial-entries/list-financial-entries';
import { listFinancialCategories } from '../../../api/fn/financial-categories/list-financial-categories';
import { FinancialEntryDto } from '../../../api/models/financial-entry-dto';
import { Role } from '../../../core/auth/auth.service';
import { signInAs, signOut } from '../../../core/auth/testing';
import { FakeApi, page } from '../../../shared/testing/fake-api';
import { provideLocalePtBr } from '../../../shared/testing/locale';
import { FINANCE_STRINGS } from '../finance.strings';
import { EntryList } from './entry-list';

function entry(id: number, overrides: Partial<FinancialEntryDto> = {}): FinancialEntryDto {
  return {
    id,
    kind: 'Payable',
    description: `Conta ${id}`,
    categoryId: 1,
    categoryName: 'Aluguel',
    partnerId: null,
    partnerName: null,
    partnerDocument: null,
    amount: 100,
    dueDate: '2026-11-10',
    status: 'Open',
    overdue: false,
    daysOverdue: 0,
    paidAt: null,
    paidAmount: null,
    installmentNo: 1,
    installmentTotal: 1,
    saleId: null,
    reversedAt: null,
    cancelledAt: null,
    reversalReason: null,
    version: 1,
    createdAt: '2026-10-01T12:00:00Z',
    updatedAt: '2026-10-01T12:00:00Z',
    ...overrides,
  };
}

describe('EntryList', () => {
  let api: FakeApi;
  let fixture: ComponentFixture<EntryList>;
  let element: HTMLElement;

  async function render(role: Role = 'Operator'): Promise<void> {
    TestBed.configureTestingModule({
      providers: [api.provide(), provideRouter([]), provideLocalePtBr()],
    });
    signInAs(role);
    fixture = TestBed.createComponent(EntryList);
    fixture.componentRef.setInput('kind', 'Payable');
    element = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
    fixture.detectChanges();
  }

  beforeEach(() => {
    api = new FakeApi()
      .on(listFinancialEntries, () =>
        page([
          entry(1, { overdue: true, daysOverdue: 3, installmentNo: 2, installmentTotal: 3 }),
          entry(2, { status: 'Paid', paidAt: '2026-10-05', paidAmount: 101 }),
          entry(3, { status: 'Cancelled' }),
        ]),
      )
      .on(getFinancialSummary, () => ({
        kind: 'Payable',
        openAmount: 1500,
        openCount: 4,
        overdueAmount: 300,
        overdueCount: 1,
        dueNext7DaysAmount: 200,
        dueNext7DaysCount: 2,
        paidThisMonthAmount: 900,
        paidThisMonthCount: 6,
      }))
      .on(listFinancialCategories, () => page([]));
  });

  afterEach(() => signOut());

  it('asks for the payables of the route, sorted by due date', async () => {
    await render();

    expect(api.callsTo(listFinancialEntries)[0]).toEqual(
      expect.objectContaining({
        kind: 'Payable',
        sort: 'dueDate',
        page: 1,
      } satisfies Partial<ListFinancialEntries$Params>),
    );
    expect(element.textContent).toContain(FINANCE_STRINGS.kinds.Payable.title);
  });

  it('shows the summary cards in reais', async () => {
    await render();

    const cards = Array.from(element.querySelectorAll('erp-kpi-strip .cell')).map((card) =>
      card.textContent?.replace(/\s/g, ' '),
    );
    expect(cards[0]).toContain('R$ 1.500,00');
    expect(cards[1]).toContain('R$ 300,00');
  });

  it('labels each status and offers only the actions that fit it', async () => {
    await render();
    const rows = element.querySelectorAll('tr.row');

    expect(rows[0].textContent).toContain(FINANCE_STRINGS.status.overdue(3));
    expect(rows[0].textContent).toContain('2/3');
    expect(
      rows[0].querySelector(`button[aria-label^="${FINANCE_STRINGS.kinds.Payable.pay}"]`),
    ).not.toBeNull();
    expect(rows[1].textContent).toContain(FINANCE_STRINGS.kinds.Payable.paidChip);
    expect(
      rows[1].querySelector(`button[aria-label^="${FINANCE_STRINGS.list.reverse}"]`),
    ).not.toBeNull();
    expect(rows[2].textContent).toContain(FINANCE_STRINGS.status.Cancelled);
    expect(rows[2].querySelectorAll('button').length).toBe(0);
  });

  it('is read-only for the Viewer profile', async () => {
    await render('Viewer');

    expect(element.querySelectorAll('tr.row button').length).toBe(0);
    expect(element.textContent).not.toContain(FINANCE_STRINGS.kinds.Payable.newEntry);
  });
});
