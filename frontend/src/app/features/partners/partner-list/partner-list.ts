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
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Api } from '../../../api/api';
import { activatePartner } from '../../../api/fn/partners/activate-partner';
import { deactivatePartner } from '../../../api/fn/partners/deactivate-partner';
import { listPartners, ListPartners$Params } from '../../../api/fn/partners/list-partners';
import { PartnerListItemDto } from '../../../api/models/partner-list-item-dto';
import { PartnerType } from '../../../api/models/partner-type';
import { AuthService } from '../../../core/auth/auth.service';
import { toApiProblem } from '../../../core/http/api-problem';
import { Notifier } from '../../../core/notifications/notifier';
import { debounced } from '../../../shared/data/debounced';
import {
  compactParams,
  readInt,
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
import { KpiItem, KpiStrip } from '../../../shared/ui/kpi-strip/kpi-strip';
import { SlashFocus } from '../../../shared/ui/slash-focus';
import { formatDocument } from '../partner-rules';
import { PARTNERS_STRINGS } from '../partners.strings';

type StatusFilter = 'all' | 'active' | 'inactive';

const ACTIVE_BY_STATUS: Record<StatusFilter, boolean | undefined> = {
  all: undefined,
  active: true,
  inactive: false,
};
const PARTNER_TYPES: readonly PartnerType[] = ['Customer', 'Supplier', 'Both'];
const SORTABLE_FIELDS = ['name', 'document'] as const;
const DEFAULT_SORT: DataTableSort = { field: 'name', direction: 'asc' };

/** Listagem de clientes e fornecedores. Mesmo padrão da listagem de produtos. */
@Component({
  selector: 'erp-partner-list',
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
    KpiStrip,
    PageHeader,
    SlashFocus,
  ],
  templateUrl: './partner-list.html',
  styleUrl: './partner-list.scss',
})
export class PartnerList {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly notifier = inject(Notifier);
  private readonly auth = inject(AuthService);
  private readonly initial = this.route.snapshot.queryParamMap;
  private readonly defaultPageSize = this.auth.pageSize();

  protected readonly strings = PARTNERS_STRINGS;
  protected readonly canWrite = this.auth.canWrite;
  protected readonly typeOptions = PARTNER_TYPES;
  protected readonly columns: readonly DataTableColumn[] = [
    { key: 'name', header: PARTNERS_STRINGS.columns.name, sortable: true, role: 'title' },
    {
      key: 'document',
      header: PARTNERS_STRINGS.columns.document,
      sortable: true,
      role: 'subtitle',
    },
    { key: 'contact', header: PARTNERS_STRINGS.columns.contact, optional: true },
    { key: 'active', header: PARTNERS_STRINGS.columns.status },
    { key: 'actions', header: PARTNERS_STRINGS.columns.actions, align: 'end', role: 'actions' },
  ];

  protected readonly search = signal(readText(this.initial, 'search'));
  protected readonly type = signal<PartnerType | null>(readType(this.initial.get('type')));
  protected readonly status = signal<StatusFilter | 'suppliers'>(
    readStatus(this.initial.get('status')),
  );
  /** A célula de fornecedores não é filtro: vale como "todos". */
  private readonly statusFilter = computed<StatusFilter>(() => {
    const status = this.status();
    return status === 'suppliers' ? 'all' : status;
  });
  protected readonly sort = signal<DataTableSort>(
    readSort(this.initial, SORTABLE_FIELDS, DEFAULT_SORT),
  );
  protected readonly pageSize = signal(readInt(this.initial, 'pageSize', this.defaultPageSize));
  private readonly searchTerm = debounced(computed(() => this.search().trim()));

  protected readonly page = linkedSignal<unknown, number>({
    source: () => [
      this.searchTerm(),
      this.type(),
      this.statusFilter(),
      this.sort(),
      this.pageSize(),
    ],
    computation: (_source, previous) => (previous ? 1 : readInt(this.initial, 'page', 1)),
  });

  private readonly query = computed<ListPartners$Params>(() => ({
    search: this.searchTerm() || undefined,
    type: this.type() ?? undefined,
    active: ACTIVE_BY_STATUS[this.statusFilter()],
    sort: toSortParam(this.sort()),
    page: this.page(),
    pageSize: this.pageSize(),
  }));

  protected readonly partners = resource({
    params: this.query,
    loader: ({ params }) => this.api.invoke(listPartners, params),
  });

  protected readonly rows = linkedSignal<
    readonly PartnerListItemDto[] | null,
    readonly PartnerListItemDto[]
  >({
    source: () => (this.partners.hasValue() ? this.partners.value().items : null),
    computation: (items, previous) => items ?? previous?.value ?? [],
  });
  protected readonly total = linkedSignal<number | null, number>({
    source: () => (this.partners.hasValue() ? this.partners.value().totalCount : null),
    computation: (total, previous) => total ?? previous?.value ?? 0,
  });
  protected readonly loadError = computed(() =>
    this.partners.status() === 'error' ? PARTNERS_STRINGS.list.loadError : null,
  );
  protected readonly hasFilters = computed(
    () => this.search().trim() !== '' || this.type() !== null || this.statusFilter() !== 'all',
  );
  protected readonly rowKey = (partner: PartnerListItemDto) => partner.id;
  protected readonly inactive = (partner: PartnerListItemDto) => !partner.active;

  /** Contagens da base inteira (independem dos filtros): só o total de cada consulta. */
  protected readonly stats = resource({
    loader: async () => {
      const [all, active, inactive, suppliers] = await Promise.all([
        this.api.invoke(listPartners, { pageSize: 1 }),
        this.api.invoke(listPartners, { active: true, pageSize: 1 }),
        this.api.invoke(listPartners, { active: false, pageSize: 1 }),
        this.api.invoke(listPartners, { type: 'Supplier', pageSize: 1 }),
      ]);
      return {
        all: all.totalCount,
        active: active.totalCount,
        inactive: inactive.totalCount,
        suppliers: suppliers.totalCount,
      };
    },
  });
  protected readonly kpiItems = computed<readonly KpiItem<StatusFilter | 'suppliers'>[]>(() => {
    const stats = this.stats.hasValue() ? this.stats.value() : null;
    const kpis = PARTNERS_STRINGS.kpis;
    return [
      {
        key: 'all',
        label: PARTNERS_STRINGS.status.all,
        value: stats?.all ?? null,
        hint: kpis.allHint,
        selectable: true,
      },
      {
        key: 'active',
        label: PARTNERS_STRINGS.status.active,
        value: stats?.active ?? null,
        hint: kpis.activeHint,
        selectable: true,
      },
      {
        key: 'inactive',
        label: PARTNERS_STRINGS.status.inactive,
        value: stats?.inactive ?? null,
        hint: kpis.inactiveHint,
        selectable: true,
      },
      {
        key: 'suppliers',
        label: kpis.suppliers,
        value: stats?.suppliers ?? null,
        hint: kpis.suppliersHint,
      },
    ];
  });
  protected readonly formatDocument = formatDocument;

  constructor() {
    effect(() => {
      void this.router.navigate([], {
        relativeTo: this.route,
        replaceUrl: true,
        queryParams: compactParams(
          {
            search: this.searchTerm(),
            type: this.type(),
            status: this.statusFilter(),
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
    this.type.set(null);
    this.status.set('all');
  }

  protected onPage(change: DataTablePage): void {
    this.pageSize.set(change.pageSize);
    this.page.set(change.page);
  }

  protected openPartner(partner: PartnerListItemDto): void {
    void this.router.navigate(['edit', partner.id], { relativeTo: this.route });
  }

  protected async toggleActive(partner: PartnerListItemDto): Promise<void> {
    try {
      if (partner.active) {
        await this.api.invoke(deactivatePartner, { id: partner.id });
        this.notifier.success(PARTNERS_STRINGS.list.deactivated);
      } else {
        await this.api.invoke(activatePartner, { id: partner.id });
        this.notifier.success(PARTNERS_STRINGS.list.activated);
      }
      this.partners.reload();
      this.stats.reload();
    } catch (error) {
      this.notifier.error(toApiProblem(error).message);
    }
  }
}

function readStatus(value: string | null): StatusFilter {
  return value === 'active' || value === 'inactive' ? value : 'all';
}

function readType(value: string | null): PartnerType | null {
  return PARTNER_TYPES.find((type) => type === value) ?? null;
}
