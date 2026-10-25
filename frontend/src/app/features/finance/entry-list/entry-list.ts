import { DatePickerToggle } from '../../../shared/ui/date-picker-toggle/date-picker-toggle';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
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
import { cancelFinancialEntry } from '../../../api/fn/financial-entries/cancel-financial-entry';
import { getFinancialSummary } from '../../../api/fn/financial-entries/get-financial-summary';
import {
  listFinancialEntries,
  ListFinancialEntries$Params,
} from '../../../api/fn/financial-entries/list-financial-entries';
import { reverseFinancialPayment } from '../../../api/fn/financial-entries/reverse-financial-payment';
import { listFinancialCategories } from '../../../api/fn/financial-categories/list-financial-categories';
import { FinancialEntryDto } from '../../../api/models/financial-entry-dto';
import { FinancialEntryKind } from '../../../api/models/financial-entry-kind';
import { FinancialEntryStatusFilter } from '../../../api/models/financial-entry-status-filter';
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
import { ConfirmService } from '../../../shared/ui/confirm-dialog/confirm-dialog';
import {
  DataTable,
  DataTableColumn,
  DataTablePage,
  DataTableSort,
} from '../../../shared/ui/data-table/data-table';
import { DataTableCell } from '../../../shared/ui/data-table/data-table-templates';
import { KpiItem, KpiStrip } from '../../../shared/ui/kpi-strip/kpi-strip';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { SlashFocus } from '../../../shared/ui/slash-focus';
import {
  SegmentedControl,
  SegmentedOption,
} from '../../../shared/ui/segmented-control/segmented-control';
import { StatusChip, StatusTone } from '../../../shared/ui/status-chip/status-chip';
import { FINANCE_STRINGS } from '../finance.strings';
import { PayDialog, PayDialogData } from '../pay-dialog/pay-dialog';

type StatusFilter = 'all' | 'open' | 'overdue' | 'paid' | 'cancelled';

const STATUS_PARAM: Record<StatusFilter, FinancialEntryStatusFilter | undefined> = {
  all: undefined,
  open: 'Open',
  overdue: 'Overdue',
  paid: 'Paid',
  cancelled: 'Cancelled',
};
const STATUSES: readonly StatusFilter[] = ['all', 'open', 'overdue', 'paid', 'cancelled'];
const SORTABLE_FIELDS = ['dueDate', 'amount', 'description'] as const;
const DEFAULT_SORT: DataTableSort = { field: 'dueDate', direction: 'asc' };

/**
 * Contas a receber ou a pagar (o tipo vem da rota). Padrão "cartão de resumo + tabela": totais em
 * aberto, vencido, a vencer e baixado no mês, e a lista com baixa, estorno e cancelamento.
 */
@Component({
  selector: 'erp-entry-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePickerToggle,
    CurrencyPipe,
    DatePipe,
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
    SegmentedControl,
    StatusChip,
  ],
  templateUrl: './entry-list.html',
  styleUrl: './entry-list.scss',
})
export class EntryList {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly confirmService = inject(ConfirmService);
  private readonly notifier = inject(Notifier);
  private readonly auth = inject(AuthService);
  private readonly initial = this.route.snapshot.queryParamMap;
  private readonly defaultPageSize = this.auth.pageSize();

  /** Dado da rota: a receber ou a pagar. */
  readonly kind = input.required<FinancialEntryKind>();

  protected readonly strings = FINANCE_STRINGS;
  protected readonly texts = computed(() => FINANCE_STRINGS.kinds[this.kind()]);
  protected readonly canWrite = this.auth.canWrite;
  protected readonly statusOptions: readonly SegmentedOption<StatusFilter>[] = [
    { value: 'all', label: FINANCE_STRINGS.list.all },
    { value: 'open', label: FINANCE_STRINGS.list.open },
    { value: 'overdue', label: FINANCE_STRINGS.list.overdue },
    { value: 'paid', label: FINANCE_STRINGS.list.paid },
    { value: 'cancelled', label: FINANCE_STRINGS.list.cancelled },
  ];
  protected readonly columns: readonly DataTableColumn[] = [
    { key: 'dueDate', header: FINANCE_STRINGS.columns.dueDate, sortable: true, role: 'subtitle' },
    {
      key: 'description',
      header: FINANCE_STRINGS.columns.description,
      sortable: true,
      role: 'title',
    },
    { key: 'partner', header: FINANCE_STRINGS.columns.partner, optional: true },
    { key: 'category', header: FINANCE_STRINGS.columns.category, optional: true },
    { key: 'amount', header: FINANCE_STRINGS.columns.amount, align: 'end', sortable: true },
    { key: 'status', header: FINANCE_STRINGS.columns.status, align: 'center' },
    { key: 'actions', header: FINANCE_STRINGS.columns.actions, align: 'end', role: 'actions' },
  ];

  protected readonly search = signal(readText(this.initial, 'search'));
  protected readonly status = signal<StatusFilter>(readStatus(this.initial.get('status')));
  protected readonly dueFrom = signal(readText(this.initial, 'dueFrom'));
  protected readonly dueTo = signal(readText(this.initial, 'dueTo'));
  protected readonly categoryId = signal<number | null>(
    readOptionalInt(this.initial, 'categoryId'),
  );
  protected readonly sort = signal<DataTableSort>(
    readSort(this.initial, SORTABLE_FIELDS, DEFAULT_SORT),
  );
  protected readonly pageSize = signal(readInt(this.initial, 'pageSize', this.defaultPageSize));
  private readonly searchTerm = debounced(computed(() => this.search().trim()));

  protected readonly page = linkedSignal<unknown, number>({
    source: () => [
      this.searchTerm(),
      this.status(),
      this.dueFrom(),
      this.dueTo(),
      this.categoryId(),
      this.sort(),
      this.pageSize(),
    ],
    computation: (_source, previous) => (previous ? 1 : readInt(this.initial, 'page', 1)),
  });

  private readonly query = computed<ListFinancialEntries$Params>(() => ({
    kind: this.kind(),
    search: this.searchTerm() || undefined,
    status: STATUS_PARAM[this.status()],
    dueFrom: this.dueFrom() || undefined,
    dueTo: this.dueTo() || undefined,
    categoryId: this.categoryId() ?? undefined,
    sort: toSortParam(this.sort()),
    page: this.page(),
    pageSize: this.pageSize(),
  }));

  protected readonly entries = resource({
    params: this.query,
    loader: ({ params }) => this.api.invoke(listFinancialEntries, params),
  });
  protected readonly summary = resource({
    params: () => this.kind(),
    loader: ({ params }) => this.api.invoke(getFinancialSummary, { kind: params }),
  });
  protected readonly categories = resource({
    params: () => (this.kind() === 'Receivable' ? 'Revenue' : 'Expense'),
    loader: ({ params }) =>
      this.api.invoke(listFinancialCategories, { kind: params, pageSize: 100 }),
  });

  protected readonly totals = computed(() =>
    this.summary.hasValue() ? this.summary.value() : null,
  );
  /** Resumo em reais: em aberto, vencido, a vencer em 7 dias e baixado no mês. */
  protected readonly kpiItems = computed<readonly KpiItem[]>(() => {
    const totals = this.totals();
    const summary = FINANCE_STRINGS.summary;
    const count = (value: number | undefined) =>
      value === undefined ? undefined : summary.count(value);
    return [
      {
        key: 'open',
        label: summary.open,
        value: totals?.openAmount ?? null,
        hint: count(totals?.openCount),
        format: 'currency',
      },
      {
        key: 'overdue',
        label: summary.overdue,
        value: totals?.overdueAmount ?? null,
        hint: count(totals?.overdueCount),
        format: 'currency',
      },
      {
        key: 'soon',
        label: summary.dueSoon,
        value: totals?.dueNext7DaysAmount ?? null,
        hint: count(totals?.dueNext7DaysCount),
        format: 'currency',
      },
      {
        key: 'paid',
        label: this.texts().paidThisMonth,
        value: totals?.paidThisMonthAmount ?? null,
        hint: count(totals?.paidThisMonthCount),
        format: 'currency',
      },
    ];
  });
  protected readonly cancelled = (entry: FinancialEntryDto) => entry.status === 'Cancelled';
  protected readonly rows = linkedSignal<
    readonly FinancialEntryDto[] | null,
    readonly FinancialEntryDto[]
  >({
    source: () => (this.entries.hasValue() ? this.entries.value().items : null),
    computation: (items, previous) => items ?? previous?.value ?? [],
  });
  protected readonly total = linkedSignal<number | null, number>({
    source: () => (this.entries.hasValue() ? this.entries.value().totalCount : null),
    computation: (total, previous) => total ?? previous?.value ?? 0,
  });
  protected readonly loadError = computed(() =>
    this.entries.status() === 'error' ? FINANCE_STRINGS.list.loadError : null,
  );
  protected readonly categoryOptions = computed(() =>
    this.categories.hasValue() ? this.categories.value().items : [],
  );
  protected readonly hasFilters = computed(
    () =>
      this.search().trim() !== '' ||
      this.status() !== 'all' ||
      this.dueFrom() !== '' ||
      this.dueTo() !== '' ||
      this.categoryId() !== null,
  );
  protected readonly rowKey = (entry: FinancialEntryDto) => entry.id;

  constructor() {
    effect(() => {
      void this.router.navigate([], {
        relativeTo: this.route,
        replaceUrl: true,
        queryParams: compactParams(
          {
            search: this.searchTerm(),
            status: this.status(),
            dueFrom: this.dueFrom(),
            dueTo: this.dueTo(),
            categoryId: this.categoryId(),
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

  protected statusLabel(entry: FinancialEntryDto): string {
    if (entry.overdue) {
      return FINANCE_STRINGS.status.overdue(entry.daysOverdue);
    }
    return entry.status === 'Paid' ? this.texts().paidChip : FINANCE_STRINGS.status[entry.status];
  }

  protected statusTone(entry: FinancialEntryDto): StatusTone {
    if (entry.overdue) {
      return 'danger';
    }
    return entry.status === 'Paid' ? 'success' : entry.status === 'Open' ? 'primary' : 'neutral';
  }

  protected onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected onDate(target: 'dueFrom' | 'dueTo', event: Event): void {
    this[target].set((event.target as HTMLInputElement).value);
  }

  protected clearFilters(): void {
    this.search.set('');
    this.status.set('all');
    this.dueFrom.set('');
    this.dueTo.set('');
    this.categoryId.set(null);
  }

  protected onPage(change: DataTablePage): void {
    this.pageSize.set(change.pageSize);
    this.page.set(change.page);
  }

  protected open(entry: FinancialEntryDto): void {
    void this.router.navigate(['edit', entry.id], { relativeTo: this.route });
  }

  protected async pay(entry: FinancialEntryDto): Promise<void> {
    const data: PayDialogData = { entry, title: this.texts().payTitle };
    const paid = await firstValueFrom(
      this.dialog.open(PayDialog, { data, width: '440px' }).afterClosed(),
    );
    if (paid) {
      this.refresh();
    }
  }

  protected async reversePayment(entry: FinancialEntryDto): Promise<void> {
    if (
      !(await this.confirm(
        FINANCE_STRINGS.list.reverseTitle,
        FINANCE_STRINGS.list.reverseMessage(entry.description),
        FINANCE_STRINGS.list.reverse,
      ))
    ) {
      return;
    }
    await this.run(
      () =>
        this.api.invoke(reverseFinancialPayment, {
          id: entry.id,
          body: { reason: null, version: entry.version },
        }),
      FINANCE_STRINGS.list.reversed,
    );
  }

  protected async cancel(entry: FinancialEntryDto): Promise<void> {
    if (
      !(await this.confirm(
        FINANCE_STRINGS.list.cancelTitle,
        FINANCE_STRINGS.list.cancelMessage(entry.description),
        FINANCE_STRINGS.list.confirmCancel,
      ))
    ) {
      return;
    }
    await this.run(
      () =>
        this.api.invoke(cancelFinancialEntry, { id: entry.id, body: { version: entry.version } }),
      FINANCE_STRINGS.list.cancelledNotice,
    );
  }

  private async confirm(title: string, message: string, confirmLabel: string): Promise<boolean> {
    return this.confirmService.ask({
      title,
      message,
      confirmLabel,
      cancelLabel: FINANCE_STRINGS.list.keep,
    });
  }

  private async run(action: () => Promise<unknown>, success: string): Promise<void> {
    try {
      await action();
      this.notifier.success(success);
      this.refresh();
    } catch (error) {
      this.notifier.error(toApiProblem(error).message);
    }
  }

  private refresh(): void {
    this.entries.reload();
    this.summary.reload();
  }
}

function readStatus(value: string | null): StatusFilter {
  return STATUSES.find((status) => status === value) ?? 'all';
}
