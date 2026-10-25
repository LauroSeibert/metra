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
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmComboboxImports } from '@spartan-ng/helm/combobox';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSelectImports } from '@spartan-ng/helm/select';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Api } from '../../../api/api';
import { activateProduct } from '../../../api/fn/products/activate-product';
import { deactivateProduct } from '../../../api/fn/products/deactivate-product';
import { listProducts, ListProducts$Params } from '../../../api/fn/products/list-products';
import { listProductCategories } from '../../../api/fn/product-categories/list-product-categories';
import { ProductListItemDto } from '../../../api/models/product-list-item-dto';
import { AuthService } from '../../../core/auth/auth.service';
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
import { Icon } from '../../../shared/ui/icon/icon';
import { KpiItem, KpiStrip } from '../../../shared/ui/kpi-strip/kpi-strip';
import { SlashFocus } from '../../../shared/ui/slash-focus';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { PRODUCTS_STRINGS } from '../products.strings';

type StatusFilter = 'all' | 'active' | 'inactive';

const ACTIVE_BY_STATUS: Record<StatusFilter, boolean | undefined> = {
  all: undefined,
  active: true,
  inactive: false,
};

const SORTABLE_FIELDS = ['sku', 'name', 'salePrice'] as const;
const DEFAULT_SORT: DataTableSort = { field: 'name', direction: 'asc' };

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
    HlmBadge,
    HlmButton,
    HlmComboboxImports,
    HlmInput,
    HlmSelectImports,
    Icon,
    DataTable,
    DataTableCell,
    DataTableExpandedRow,
    KpiStrip,
    SlashFocus,
    PageHeader,
  ],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
})
export class ProductList {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly notifier = inject(Notifier);
  private readonly auth = inject(AuthService);
  private readonly initial = this.route.snapshot.queryParamMap;
  /** Itens por página escolhidos em "Meu perfil" (vale quando a URL não traz pageSize). */
  private readonly defaultPageSize = this.auth.pageSize();

  protected readonly strings = PRODUCTS_STRINGS;
  protected readonly canWrite = this.auth.canWrite;
  protected readonly columns: readonly DataTableColumn[] = [
    { key: 'name', header: PRODUCTS_STRINGS.columns.product, sortable: true, role: 'title' },
    { key: 'sku', header: PRODUCTS_STRINGS.columns.sku, sortable: true, role: 'subtitle' },
    { key: 'unitCode', header: PRODUCTS_STRINGS.columns.unitShort, optional: true },
    { key: 'salePrice', header: PRODUCTS_STRINGS.columns.salePrice, align: 'end', sortable: true },
    { key: 'stockQuantity', header: PRODUCTS_STRINGS.columns.stock, align: 'end' },
    { key: 'active', header: PRODUCTS_STRINGS.columns.status },
    { key: 'actions', header: PRODUCTS_STRINGS.columns.actions, align: 'end', role: 'actions' },
  ];

  // Filtros, iniciados a partir da URL.
  protected readonly search = signal(readText(this.initial, 'search'));
  protected readonly status = signal<StatusFilter | 'categories'>(
    readStatus(this.initial.get('status')),
  );
  /** A célula de categorias não é filtro: vale como "todos". */
  private readonly statusFilter = computed<StatusFilter>(() => {
    const status = this.status();
    return status === 'categories' ? 'all' : status;
  });
  protected readonly categoryId = signal<number | null>(
    readOptionalInt(this.initial, 'categoryId'),
  );
  /** Só produtos com saldo abaixo do estoque mínimo. */
  protected readonly lowStock = signal(this.initial.get('stock') === 'low');
  protected readonly sort = signal<DataTableSort>(
    readSort(this.initial, SORTABLE_FIELDS, DEFAULT_SORT),
  );
  protected readonly pageSize = signal(readInt(this.initial, 'pageSize', this.defaultPageSize));
  private readonly searchTerm = debounced(computed(() => this.search().trim()));

  /** Volta para a página 1 sempre que um filtro muda; na abertura respeita a página da URL. */
  protected readonly page = linkedSignal<unknown, number>({
    source: () => [
      this.searchTerm(),
      this.statusFilter(),
      this.categoryId(),
      this.lowStock(),
      this.sort(),
      this.pageSize(),
    ],
    computation: (_source, previous) => (previous ? 1 : readInt(this.initial, 'page', 1)),
  });

  private readonly query = computed<ListProducts$Params>(() => ({
    search: this.searchTerm() || undefined,
    categoryId: this.categoryId() ?? undefined,
    active: ACTIVE_BY_STATUS[this.statusFilter()],
    lowStock: this.lowStock() || undefined,
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

  /** Faixa de indicadores: Todos, Ativos e Inativos filtram pela situação. */
  protected readonly kpiItems = computed<readonly KpiItem<StatusFilter | 'categories'>[]>(() => {
    const kpis = this.kpis();
    const share = this.activeShare();
    return [
      {
        key: 'all',
        label: PRODUCTS_STRINGS.status.all,
        value: kpis?.total ?? null,
        hint: PRODUCTS_STRINGS.kpis.totalHint,
        selectable: true,
      },
      {
        key: 'active',
        label: PRODUCTS_STRINGS.status.active,
        value: kpis?.active ?? null,
        hint:
          share === null
            ? undefined
            : PRODUCTS_STRINGS.kpis.activeShare(Math.round(share * 100) + '%'),
        selectable: true,
      },
      {
        key: 'inactive',
        label: PRODUCTS_STRINGS.status.inactive,
        value: kpis?.inactive ?? null,
        hint: PRODUCTS_STRINGS.kpis.inactiveHint,
        selectable: true,
      },
      {
        key: 'categories',
        label: PRODUCTS_STRINGS.kpis.categories,
        value: kpis?.categories ?? null,
        hint: PRODUCTS_STRINGS.kpis.categoriesHint,
      },
    ];
  });
  protected readonly inactive = (product: ProductListItemDto) => !product.active;

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

  /** Texto dos filtros em lista (combobox de categoria e seleção de estoque). */
  protected readonly categoryLabel = (id: number): string =>
    id === 0
      ? PRODUCTS_STRINGS.list.allCategories
      : (this.categoryOptions().find((category) => category.id === id)?.name ?? '');
  protected readonly stockLabel = (lowOnly: boolean): string =>
    lowOnly ? PRODUCTS_STRINGS.list.lowStock : PRODUCTS_STRINGS.list.allStock;

  constructor() {
    effect(() => {
      void this.router.navigate([], {
        relativeTo: this.route,
        replaceUrl: true,
        queryParams: compactParams(
          {
            search: this.searchTerm(),
            status: this.statusFilter(),
            categoryId: this.categoryId(),
            stock: this.lowStock() ? 'low' : null,
            sort: toSortParam(this.sort()),
            page: this.page(),
            pageSize: this.pageSize(),
          },
          {
            status: 'all',
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
    this.status.set('all');
    this.categoryId.set(null);
    this.lowStock.set(false);
  }

  protected readonly hasFilters = computed(
    () =>
      this.search().trim() !== '' ||
      this.statusFilter() !== 'all' ||
      this.categoryId() !== null ||
      this.lowStock(),
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
