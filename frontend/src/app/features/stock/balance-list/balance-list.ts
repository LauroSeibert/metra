import { DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  linkedSignal,
  resource,
  signal,
} from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Api } from '../../../api/api';
import { listProductCategories } from '../../../api/fn/product-categories/list-product-categories';
import {
  listStockBalances,
  ListStockBalances$Params,
} from '../../../api/fn/stock/list-stock-balances';
import { StockBalanceDto } from '../../../api/models/stock-balance-dto';
import { AuthService } from '../../../core/auth/auth.service';
import { debounced } from '../../../shared/data/debounced';
import {
  compactParams,
  readInt,
  readOptionalInt,
  readSort,
  readText,
  toSortParam,
} from '../../../shared/data/list-url-state';
import {
  DataTable,
  DataTableColumn,
  DataTablePage,
  DataTableSort,
} from '../../../shared/ui/data-table/data-table';
import { DataTableCell } from '../../../shared/ui/data-table/data-table-templates';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import {
  SegmentedControl,
  SegmentedOption,
} from '../../../shared/ui/segmented-control/segmented-control';
import { StatusChip } from '../../../shared/ui/status-chip/status-chip';
import { MovementDialog, MovementDialogData } from '../movement-dialog/movement-dialog';
import { STOCK_STRINGS } from '../stock.strings';

type LevelFilter = 'all' | 'low' | 'ok';

const LOW_BY_LEVEL: Record<LevelFilter, boolean | undefined> = {
  all: undefined,
  low: true,
  ok: false,
};
const SORTABLE_FIELDS = ['name', 'sku', 'quantity'] as const;
const DEFAULT_SORT: DataTableSort = { field: 'name', direction: 'asc' };

/** Saldo por produto, comparado ao estoque mínimo, com atalhos para movimentar e ver o histórico. */
@Component({
  selector: 'erp-balance-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatTooltipModule,
    DataTable,
    DataTableCell,
    PageHeader,
    SegmentedControl,
    StatusChip,
  ],
  templateUrl: './balance-list.html',
  styleUrl: './balance-list.scss',
})
export class BalanceList {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly auth = inject(AuthService);
  private readonly initial = this.route.snapshot.queryParamMap;
  private readonly defaultPageSize = this.auth.pageSize();

  protected readonly strings = STOCK_STRINGS;
  protected readonly canWrite = this.auth.canWrite;
  protected readonly levelOptions: readonly SegmentedOption<LevelFilter>[] = [
    { value: 'all', label: STOCK_STRINGS.balances.all },
    { value: 'low', label: STOCK_STRINGS.balances.low },
    { value: 'ok', label: STOCK_STRINGS.balances.ok },
  ];
  protected readonly columns: readonly DataTableColumn[] = [
    { key: 'name', header: STOCK_STRINGS.columns.product, sortable: true, role: 'title' },
    { key: 'sku', header: STOCK_STRINGS.columns.sku, sortable: true, role: 'subtitle' },
    { key: 'unitCode', header: STOCK_STRINGS.columns.unit, align: 'center', optional: true },
    { key: 'quantity', header: STOCK_STRINGS.columns.quantity, align: 'end', sortable: true },
    { key: 'minStock', header: STOCK_STRINGS.columns.minStock, align: 'end', optional: true },
    { key: 'lowStock', header: STOCK_STRINGS.columns.level, align: 'center' },
    { key: 'actions', header: STOCK_STRINGS.columns.actions, align: 'end', role: 'actions' },
  ];

  protected readonly search = signal(readText(this.initial, 'search'));
  protected readonly categoryId = signal<number | null>(
    readOptionalInt(this.initial, 'categoryId'),
  );
  protected readonly level = signal<LevelFilter>(readLevel(this.initial.get('level')));
  protected readonly sort = signal<DataTableSort>(
    readSort(this.initial, SORTABLE_FIELDS, DEFAULT_SORT),
  );
  protected readonly pageSize = signal(readInt(this.initial, 'pageSize', this.defaultPageSize));
  private readonly searchTerm = debounced(computed(() => this.search().trim()));

  protected readonly page = linkedSignal<unknown, number>({
    source: () => [
      this.searchTerm(),
      this.categoryId(),
      this.level(),
      this.sort(),
      this.pageSize(),
    ],
    computation: (_source, previous) => (previous ? 1 : readInt(this.initial, 'page', 1)),
  });

  private readonly query = computed<ListStockBalances$Params>(() => ({
    search: this.searchTerm() || undefined,
    categoryId: this.categoryId() ?? undefined,
    lowStock: LOW_BY_LEVEL[this.level()],
    sort: toSortParam(this.sort()),
    page: this.page(),
    pageSize: this.pageSize(),
  }));

  protected readonly balances = resource({
    params: this.query,
    loader: ({ params }) => this.api.invoke(listStockBalances, params),
  });
  protected readonly categories = resource({
    loader: () => this.api.invoke(listProductCategories, { pageSize: 100 }),
  });

  protected readonly rows = linkedSignal<
    readonly StockBalanceDto[] | null,
    readonly StockBalanceDto[]
  >({
    source: () => (this.balances.hasValue() ? this.balances.value().items : null),
    computation: (items, previous) => items ?? previous?.value ?? [],
  });
  protected readonly total = linkedSignal<number | null, number>({
    source: () => (this.balances.hasValue() ? this.balances.value().totalCount : null),
    computation: (total, previous) => total ?? previous?.value ?? 0,
  });
  protected readonly loadError = computed(() =>
    this.balances.status() === 'error' ? STOCK_STRINGS.balances.loadError : null,
  );
  protected readonly categoryOptions = computed(() =>
    this.categories.hasValue() ? this.categories.value().items : [],
  );
  protected readonly hasFilters = computed(
    () => this.search().trim() !== '' || this.categoryId() !== null || this.level() !== 'all',
  );
  protected readonly rowKey = (balance: StockBalanceDto) => balance.productId;

  constructor() {
    effect(() => {
      void this.router.navigate([], {
        relativeTo: this.route,
        replaceUrl: true,
        queryParams: compactParams(
          {
            search: this.searchTerm(),
            categoryId: this.categoryId(),
            level: this.level(),
            sort: toSortParam(this.sort()),
            page: this.page(),
            pageSize: this.pageSize(),
          },
          {
            level: 'all',
            sort: toSortParam(DEFAULT_SORT),
            page: 1,
            pageSize: this.defaultPageSize,
          },
        ),
      });
    });
  }

  protected onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected clearFilters(): void {
    this.search.set('');
    this.categoryId.set(null);
    this.level.set('all');
  }

  protected onPage(change: DataTablePage): void {
    this.pageSize.set(change.pageSize);
    this.page.set(change.page);
  }

  /** Abre o lançamento; com produto, já vem escolhido. Lançou, recarrega o saldo. */
  protected async move(balance: StockBalanceDto | null): Promise<void> {
    const data: MovementDialogData = {
      product: balance
        ? {
            id: balance.productId,
            sku: balance.sku,
            name: balance.name,
            unitCode: balance.unitCode,
          }
        : null,
      currentQuantity: balance?.quantity ?? null,
    };
    const saved = await firstValueFrom(
      this.dialog
        .open(MovementDialog, { data, width: '560px', autoFocus: 'first-tabbable' })
        .afterClosed(),
    );
    if (saved) {
      this.balances.reload();
    }
  }

  protected openHistory(balance: StockBalanceDto): void {
    void this.router.navigate(['movements'], {
      relativeTo: this.route,
      queryParams: { productId: balance.productId },
    });
  }
}

function readLevel(value: string | null): LevelFilter {
  return value === 'low' || value === 'ok' ? value : 'all';
}
