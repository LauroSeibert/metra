import { HlmTooltip } from '@spartan-ng/helm/tooltip';
import { DatePickerToggle } from '../../../shared/ui/date-picker-toggle/date-picker-toggle';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
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
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Api } from '../../../api/api';
import { cancelSale } from '../../../api/fn/sales/cancel-sale';
import { confirmSale } from '../../../api/fn/sales/confirm-sale';
import { getSale } from '../../../api/fn/sales/get-sale';
import { listSales, ListSales$Params } from '../../../api/fn/sales/list-sales';
import { SaleListItemDto } from '../../../api/models/sale-list-item-dto';
import { SaleStatus } from '../../../api/models/sale-status';
import { AuthService } from '../../../core/auth/auth.service';
import { toApiProblem } from '../../../core/http/api-problem';
import { Notifier } from '../../../core/notifications/notifier';
import { debounced } from '../../../shared/data/debounced';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/ui/confirm-dialog/confirm-dialog';
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
import { StatusChip, StatusTone } from '../../../shared/ui/status-chip/status-chip';
import { PartnerPicker, PickedPartner } from '../../partners/partner-picker/partner-picker';
import { SALES_STRINGS } from '../sales.strings';

type StatusFilter = 'all' | SaleStatus;

const STATUS_TONE: Record<SaleStatus, StatusTone> = {
  Draft: 'warning',
  Confirmed: 'success',
  Cancelled: 'neutral',
};

/** Lista de vendas: contagem por situação (filtra), busca pelo número, cliente e período. */
@Component({
  selector: 'erp-sale-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    HlmTooltip,
    DatePickerToggle,
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    DataTable,
    DataTableCell,
    KpiStrip,
    PageHeader,
    PartnerPicker,
    SlashFocus,
    StatusChip,
  ],
  templateUrl: './sale-list.html',
  styleUrl: './sale-list.scss',
})
export class SaleList {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly auth = inject(AuthService);

  protected readonly strings = SALES_STRINGS;
  protected readonly canWrite = this.auth.canWrite;
  protected readonly columns: readonly DataTableColumn[] = [
    { key: 'number', header: SALES_STRINGS.columns.number, sortable: true, role: 'subtitle' },
    { key: 'partner', header: SALES_STRINGS.columns.partner, role: 'title' },
    { key: 'issuedAt', header: SALES_STRINGS.columns.issuedAt, sortable: true },
    { key: 'payment', header: SALES_STRINGS.columns.payment, optional: true },
    { key: 'total', header: SALES_STRINGS.columns.total, align: 'end', sortable: true },
    { key: 'status', header: SALES_STRINGS.columns.status, align: 'center' },
    { key: 'actions', header: SALES_STRINGS.columns.actions, align: 'end', role: 'actions' },
  ];

  protected readonly search = signal('');
  protected readonly status = signal<StatusFilter | null>('all');
  protected readonly partner = signal<PickedPartner | null>(null);
  protected readonly issuedFrom = signal('');
  protected readonly issuedTo = signal('');
  protected readonly sort = signal<DataTableSort>({ field: 'number', direction: 'desc' });
  protected readonly pageSize = signal(this.auth.pageSize());
  private readonly searchTerm = debounced(computed(() => this.search().trim()));

  protected readonly page = linkedSignal<unknown, number>({
    source: () => [
      this.searchTerm(),
      this.status(),
      this.partner(),
      this.issuedFrom(),
      this.issuedTo(),
      this.sort(),
      this.pageSize(),
    ],
    computation: () => 1,
  });

  /** Filtros comuns à lista e às contagens (a situação fica de fora das contagens). */
  private readonly filters = computed(() => {
    const number = Number(this.searchTerm());
    return {
      number: Number.isInteger(number) && number > 0 ? number : undefined,
      partnerId: this.partner()?.id,
      issuedFrom: this.issuedFrom() || undefined,
      issuedTo: this.issuedTo() || undefined,
    };
  });
  private readonly query = computed<ListSales$Params>(() => {
    const status = this.status();
    const sort = this.sort();
    return {
      ...this.filters(),
      status: status && status !== 'all' ? status : undefined,
      sort: sort.direction === 'desc' ? `-${sort.field}` : sort.field,
      page: this.page(),
      pageSize: this.pageSize(),
    };
  });

  protected readonly sales = resource({
    params: this.query,
    loader: ({ params }) => this.api.invoke(listSales, params),
  });
  protected readonly stats = resource({
    params: this.filters,
    loader: async ({ params }) => {
      const count = (status?: SaleStatus) =>
        this.api
          .invoke(listSales, { ...params, status, pageSize: 1 })
          .then((result) => result.totalCount);
      const [all, draft, confirmed, cancelled] = await Promise.all([
        count(),
        count('Draft'),
        count('Confirmed'),
        count('Cancelled'),
      ]);
      return { all, draft, confirmed, cancelled };
    },
  });

  protected readonly kpiItems = computed<readonly KpiItem<StatusFilter>[]>(() => {
    const stats = this.stats.hasValue() ? this.stats.value() : null;
    const list = SALES_STRINGS.list;
    const status = SALES_STRINGS.status;
    return [
      {
        key: 'all',
        label: list.all,
        value: stats?.all ?? null,
        hint: list.allHint,
        selectable: true,
      },
      {
        key: 'Draft',
        label: status.Draft,
        value: stats?.draft ?? null,
        hint: list.draftHint,
        selectable: true,
      },
      {
        key: 'Confirmed',
        label: status.Confirmed,
        value: stats?.confirmed ?? null,
        hint: list.confirmedHint,
        selectable: true,
      },
      {
        key: 'Cancelled',
        label: status.Cancelled,
        value: stats?.cancelled ?? null,
        hint: list.cancelledHint,
        selectable: true,
      },
    ];
  });

  protected readonly rows = linkedSignal<
    readonly SaleListItemDto[] | null,
    readonly SaleListItemDto[]
  >({
    source: () => (this.sales.hasValue() ? this.sales.value().items : null),
    computation: (items, previous) => items ?? previous?.value ?? [],
  });
  protected readonly total = linkedSignal<number | null, number>({
    source: () => (this.sales.hasValue() ? this.sales.value().totalCount : null),
    computation: (total, previous) => total ?? previous?.value ?? 0,
  });
  protected readonly loadError = computed(() =>
    this.sales.status() === 'error' ? SALES_STRINGS.list.loadError : null,
  );
  protected readonly hasFilters = computed(
    () =>
      this.search().trim() !== '' ||
      this.status() !== 'all' ||
      this.partner() !== null ||
      this.issuedFrom() !== '' ||
      this.issuedTo() !== '',
  );
  protected readonly rowKey = (sale: SaleListItemDto) => sale.id;
  protected readonly cancelled = (sale: SaleListItemDto) => sale.status === 'Cancelled';

  protected tone(sale: SaleListItemDto): StatusTone {
    return STATUS_TONE[sale.status];
  }

  protected onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected onDate(target: 'issuedFrom' | 'issuedTo', event: Event): void {
    this[target].set((event.target as HTMLInputElement).value);
  }

  protected clearFilters(): void {
    this.search.set('');
    this.status.set('all');
    this.partner.set(null);
    this.issuedFrom.set('');
    this.issuedTo.set('');
  }

  protected onPage(change: DataTablePage): void {
    this.pageSize.set(change.pageSize);
    this.page.set(change.page);
  }

  protected open(sale: SaleListItemDto): void {
    void this.router.navigate(['edit', sale.id], { relativeTo: this.route });
  }

  protected async confirm(sale: SaleListItemDto): Promise<void> {
    const actions = SALES_STRINGS.actions;
    if (
      await this.ask(
        actions.confirmTitle,
        actions.confirmMessage(sale.number),
        actions.confirmTitle,
      )
    ) {
      await this.run(async () => {
        const { version: current } = await this.api.invoke(getSale, { id: sale.id });
        await this.api.invoke(confirmSale, { id: sale.id, body: { version: current } });
      }, actions.confirmed);
    }
  }

  protected async cancel(sale: SaleListItemDto): Promise<void> {
    const actions = SALES_STRINGS.actions;
    if (
      await this.ask(actions.cancelTitle, actions.cancelMessage(sale.number), actions.cancelTitle)
    ) {
      await this.run(async () => {
        const { version: current } = await this.api.invoke(getSale, { id: sale.id });
        await this.api.invoke(cancelSale, {
          id: sale.id,
          body: { reason: null, version: current },
        });
      }, actions.cancelled);
    }
  }

  private async ask(title: string, message: string, confirmLabel: string): Promise<boolean> {
    const ref = this.dialog.open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
      data: { title, message, confirmLabel, cancelLabel: SALES_STRINGS.actions.keep },
    });
    return (await firstValueFrom(ref.afterClosed())) === true;
  }

  private async run(action: () => Promise<unknown>, success: string): Promise<void> {
    try {
      await action();
      this.notifier.success(success);
      this.sales.reload();
      this.stats.reload();
    } catch (error) {
      this.notifier.error(toApiProblem(error).message);
    }
  }
}
