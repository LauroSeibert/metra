import { Component, signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LOADING_DELAY_MS } from '../../data/delayed-flag';
import { DataTable, DataTableColumn, DataTableSort } from './data-table';
import { DataTableCell, DataTableExpandedRow } from './data-table-templates';

interface Row {
  id: number;
  name: string;
  price: number;
}

@Component({
  imports: [DataTable, DataTableCell, DataTableExpandedRow],
  template: `
    <erp-data-table
      [columns]="columns"
      [rows]="rows()"
      [rowKey]="rowKey"
      [loading]="loading()"
      [error]="error()"
      [(sort)]="sort"
      [total]="rows().length"
      (retry)="retries = retries + 1"
    >
      <ng-template erpCell="price" [erpCellOf]="rows()" let-row>R$ {{ row.price }}</ng-template>
      <ng-template erpExpandedRow [erpExpandedRowOf]="rows()" let-row>
        <p class="detail">Detalhe de {{ row.name }}</p>
      </ng-template>
    </erp-data-table>
  `,
})
class Host {
  readonly columns: DataTableColumn[] = [
    { key: 'name', header: 'Nome', sortable: true },
    { key: 'price', header: 'Preço', align: 'end' },
  ];
  readonly rows = signal<Row[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly sort = signal<DataTableSort | null>(null);
  readonly rowKey = (row: Row) => row.id;
  retries = 0;
}

describe('DataTable', () => {
  let fixture: ComponentFixture<Host>;
  let host: Host;
  let element: HTMLElement;

  beforeEach(async () => {
    fixture = TestBed.createComponent(Host);
    host = fixture.componentInstance;
    element = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  });

  async function update(change: () => void): Promise<void> {
    change();
    fixture.detectChanges();
    await fixture.whenStable();
  }

  it('reserves skeleton rows while loading and only reveals them if loading is slow', async () => {
    vi.useFakeTimers();
    try {
      await update(() => host.loading.set(true));

      const skeleton = () => element.querySelectorAll('tr.skeleton-row');
      expect(skeleton().length).toBeGreaterThan(0);
      expect(skeleton()[0].classList).toContain('pending');
      expect(skeleton()[0].querySelectorAll('td').length).toBe(host.columns.length + 1);
      expect(element.querySelector('mat-progress-bar')).toBeNull();
      expect(element.querySelector('table')?.getAttribute('aria-busy')).toBe('true');

      await update(() => vi.advanceTimersByTime(LOADING_DELAY_MS));

      expect(skeleton()[0].classList).not.toContain('pending');
      expect(element.querySelector('mat-progress-bar')).not.toBeNull();
    } finally {
      vi.useRealTimers();
    }
  });

  it('never shows loading indicators when data arrives before the delay', async () => {
    vi.useFakeTimers();
    try {
      await update(() => host.loading.set(true));
      await update(() => {
        host.rows.set([{ id: 1, name: 'Caneta', price: 3 }]);
        host.loading.set(false);
      });
      await update(() => vi.advanceTimersByTime(LOADING_DELAY_MS));

      expect(element.querySelector('tr.skeleton-row')).toBeNull();
      expect(element.querySelector('mat-progress-bar')).toBeNull();
    } finally {
      vi.useRealTimers();
    }
  });

  it('emits rowActivate on double click, except on buttons inside the row', async () => {
    const activated: Row[] = [];
    const table = fixture.debugElement.children[0].componentInstance as DataTable<Row>;
    table.rowActivate.subscribe((row) => activated.push(row));
    await update(() => host.rows.set([{ id: 1, name: 'Caneta', price: 3 }]));

    const row = element.querySelector('tr.row')!;
    row.querySelector('td.align-end')!.dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));
    row
      .querySelector('td.expander button')!
      .dispatchEvent(new MouseEvent('dblclick', { bubbles: true }));

    expect(activated).toEqual([{ id: 1, name: 'Caneta', price: 3 }]);
  });

  it('shows the empty state when there are no rows', () => {
    expect(element.querySelector('tr.state')?.textContent).toContain('Nenhum registro encontrado.');
  });

  it('shows the error with a retry button', async () => {
    await update(() => host.error.set('Falhou'));

    const button = element.querySelector<HTMLButtonElement>('tr.state button');
    expect(element.textContent).toContain('Falhou');
    button?.click();
    expect(host.retries).toBe(1);
  });

  it('renders custom cells and expands rows', async () => {
    await update(() => host.rows.set([{ id: 1, name: 'Caneta', price: 3 }]));

    expect(element.querySelector('tbody td.align-end')?.textContent).toContain('R$ 3');
    expect(element.querySelector('.detail')).toBeNull();

    await update(() => element.querySelector<HTMLButtonElement>('td.expander button')?.click());

    expect(element.querySelector('.detail')?.textContent).toContain('Detalhe de Caneta');
  });

  it('toggles sort direction on sortable headers', async () => {
    await update(() => host.rows.set([{ id: 1, name: 'Caneta', price: 3 }]));
    const sortButton = () => element.querySelector<HTMLButtonElement>('th button.sort');

    await update(() => sortButton()?.click());
    expect(host.sort()).toEqual({ field: 'name', direction: 'asc' });

    await update(() => sortButton()?.click());
    expect(host.sort()).toEqual({ field: 'name', direction: 'desc' });
    expect(element.querySelector('th[aria-sort]')?.getAttribute('aria-sort')).toBe('descending');
  });
});
