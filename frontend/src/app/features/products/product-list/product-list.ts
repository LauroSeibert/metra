import { CurrencyPipe, DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
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
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Api } from '../../../api/api';
import { activateProduct } from '../../../api/fn/products/activate-product';
import { deactivateProduct } from '../../../api/fn/products/deactivate-product';
import { listProducts, ListProducts$Params } from '../../../api/fn/products/list-products';
import { listProductCategories } from '../../../api/fn/product-categories/list-product-categories';
import { ProductListItemDto } from '../../../api/models/product-list-item-dto';
import { toApiProblem } from '../../../core/http/api-problem';
import { Notifier } from '../../../core/notifications/notifier';
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
import {
  DataTableCell,
  DataTableExpandedRow,
} from '../../../shared/ui/data-table/data-table-templates';
import { KpiCard } from '../../../shared/ui/kpi-card/kpi-card';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import {
  SegmentedControl,
  SegmentedOption,
} from '../../../shared/ui/segmented-control/segmented-control';
import { StatusChip } from '../../../shared/ui/status-chip/status-chip';
import { PRODUCTS_STRINGS } from '../products.strings';

type StatusFilter = 'all' | 'active' | 'inactive';

const ACTIVE_BY_STATUS: Record<StatusFilter, boolean | undefined> = {
  all: undefined,
  active: true,
  inactive: false,
};

const SORTABLE_FIELDS = ['sku', 'name', 'salePrice'] as const;
const DEFAULT_SORT: DataTableSort = { field: 'name', direction: 'asc' };
const DEFAULT_PAGE_SIZE = 20;

/**
 * Listagem de produtos: busca com debounce, filtros, ordenação e paginação no servidor, estado na
 * URL. Tela de referência para as próximas listagens (ver .claude/skills/new-feature-screen).
 */
@Component({
  selector: 'erp-product-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    PercentPipe,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatTooltipModule,
    DataTable,
    DataTableCell,
    DataTableExpandedRow,
    KpiCard,
    PageHeader,
    SegmentedControl,
    StatusChip,
  ],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
})
export class ProductList {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly notifier = inject(Notifier);
  private readonly initial = this.route.snapshot.queryParamMap;

  protected readonly strings = PRODUCTS_STRINGS;
  protected readonly statusOptions: readonly SegmentedOption<StatusFilter>[] = [
    { value: 'all', label: PRODUCTS_STRINGS.status.all },
    { value: 'active', label: PRODUCTS_STRINGS.status.active },
    { value: 'inactive', label: PRODUCTS_STRINGS.status.inactive },
  ];
  protected readonly columns: readonly DataTableColumn[] = [
    { key: 'name', header: PRODUCTS_STRINGS.columns.product, sortable: true, role: 'title' },
    { key: 'sku', header: PRODUCTS_STRINGS.columns.sku, sortable: true, role: 'subtitle' },
    { key: 'categoryName', header: PRODUCTS_STRINGS.columns.category },
    { key: 'unitCode', header: PRODUCTS_STRINGS.columns.unit, align: 'center', optional: true },
    { key: 'salePrice', header: PRODUCTS_STRINGS.columns.salePrice, align: 'end', sortable: true },
    { key: 'active', header: PRODUCTS_STRINGS.columns.status, align: 'center' },
    { key: 'actions', header: PRODUCTS_STRINGS.columns.actions, align: 'end', role: 'actions' },
  ];

  // Filtros, iniciados a partir da URL.
  protected readonly search = signal(readText(this.initial, 'search'));
  protected readonly status = signal<StatusFilter>(readStatus(this.initial.get('status')));
  protected readonly categoryId = signal<number | null>(
    readOptionalInt(this.initial, 'categoryId'),
  );
  protected readonly sort = signal<DataTableSort>(
    readSort(this.initial, SORTABLE_FIELDS, DEFAULT_SORT),
  );
  protected readonly pageSize = signal(readInt(this.initial, 'pageSize', DEFAULT_PAGE_SIZE));
  private readonly searchTerm = debounced(computed(() => this.search().trim()));

  /** Volta para a página 1 sempre que um filtro muda; na abertura respeita a página da URL. */
  protected readonly page = linkedSignal<unknown, number>({
    source: () => [
      this.searchTerm(),
      this.status(),
      this.categoryId(),
      this.sort(),
      this.pageSize(),
    ],
    computation: (_source, previous) => (previous ? 1 : readInt(this.initial, 'page', 1)),
  });

  private readonly query = computed<ListProducts$Params>(() => ({
    search: this.searchTerm() || undefined,
    categoryId: this.categoryId() ?? undefined,
    active: ACTIVE_BY_STATUS[this.status()],
    sort: toSortParam(this.sort()),
    page: this.page(),
    pageSize: this.pageSize(),
  }));

  protected readonly products = resource({
    params: this.query,
    loader: ({ params }) => this.api.invoke(listProducts, params),
  });

  protected readonly categories = resource({
    loader: () => this.api.invoke(listProductCategories, { pageSize: 100 }),
  });

  /** Indicadores do catálogo inteiro (independem dos filtros): só a contagem de cada consulta. */
  protected readonly stats = resource({
    loader: async () => {
      const [all, active, inactive, categories] = await Promise.all([
        this.api.invoke(listProducts, { pageSize: 1 }),
        this.api.invoke(listProducts, { active: true, pageSize: 1 }),
        this.api.invoke(listProducts, { active: false, pageSize: 1 }),
        this.api.invoke(listProductCategories, { pageSize: 1 }),
      ]);
      return {
        total: all.totalCount,
        active: active.totalCount,
        inactive: inactive.totalCount,
        categories: categories.totalCount,
      };
    },
  });
  protected readonly kpis = computed(() => (this.stats.hasValue() ? this.stats.value() : null));
  protected readonly activeShare = computed(() => {
    const kpis = this.kpis();
    return kpis && kpis.total > 0 ? kpis.active / kpis.total : null;
  });

  /** Mantém as linhas anteriores (esmaecidas) enquanto a próxima página carrega. */
  protected readonly rows = linkedSignal<
    readonly ProductListItemDto[] | null,
    readonly ProductListItemDto[]
  >({
    source: () => (this.products.hasValue() ? this.products.value().items : null),
    computation: (items, previous) => items ?? previous?.value ?? [],
  });
  protected readonly total = linkedSignal<number | null, number>({
    source: () => (this.products.hasValue() ? this.products.value().totalCount : null),
    computation: (total, previous) => total ?? previous?.value ?? 0,
  });
  protected readonly loadError = computed(() =>
    this.products.status() === 'error' ? PRODUCTS_STRINGS.list.loadError : null,
  );
  protected readonly categoryOptions = computed(() =>
    this.categories.hasValue() ? this.categories.value().items : [],
  );

  protected readonly rowKey = (product: ProductListItemDto) => product.id;

  constructor() {
    effect(() => {
      void this.router.navigate([], {
        relativeTo: this.route,
        replaceUrl: true,
        queryParams: compactParams(
          {
            search: this.searchTerm(),
            status: this.status(),
            categoryId: this.categoryId(),
            sort: toSortParam(this.sort()),
            page: this.page(),
            pageSize: this.pageSize(),
          },
          { status: 'all', sort: toSortParam(DEFAULT_SORT), page: 1, pageSize: DEFAULT_PAGE_SIZE },
        ),
      });
    });
  }

  protected onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected clearFilters(): void {
    this.search.set('');
    this.status.set('all');
    this.categoryId.set(null);
  }

  protected readonly hasFilters = computed(
    () => this.search().trim() !== '' || this.status() !== 'all' || this.categoryId() !== null,
  );

  protected onPage(change: DataTablePage): void {
    this.pageSize.set(change.pageSize);
    this.page.set(change.page);
  }

  protected margin(product: ProductListItemDto): number | null {
    return product.salePrice > 0
      ? (product.salePrice - product.costPrice) / product.salePrice
      : null;
  }

  /** Duplo clique na linha abre a edição, como o link do nome. */
  protected openProduct(product: ProductListItemDto): void {
    void this.router.navigate(['edit', product.id], { relativeTo: this.route });
  }

  protected async toggleActive(product: ProductListItemDto): Promise<void> {
    try {
      if (product.active) {
        await this.api.invoke(deactivateProduct, { id: product.id });
        this.notifier.success(PRODUCTS_STRINGS.list.deactivated);
      } else {
        await this.api.invoke(activateProduct, { id: product.id });
        this.notifier.success(PRODUCTS_STRINGS.list.activated);
      }
      this.products.reload();
      this.stats.reload();
    } catch (error) {
      this.notifier.error(toApiProblem(error).message);
    }
  }
}

function readStatus(value: string | null): StatusFilter {
  return value === 'active' || value === 'inactive' ? value : 'all';
}
