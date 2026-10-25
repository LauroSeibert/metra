import { DatePickerToggle } from '../../../shared/ui/date-picker-toggle/date-picker-toggle';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
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
import { getProduct } from '../../../api/fn/products/get-product';
import {
  listStockMovements,
  ListStockMovements$Params,
} from '../../../api/fn/stock/list-stock-movements';
import { reverseStockMovement } from '../../../api/fn/stock/reverse-stock-movement';
import { StockMovementDto } from '../../../api/models/stock-movement-dto';
import { StockMovementType } from '../../../api/models/stock-movement-type';
import { AuthService } from '../../../core/auth/auth.service';
import { toApiProblem } from '../../../core/http/api-problem';
import { Notifier } from '../../../core/notifications/notifier';
import { compactParams, readOptionalInt, readText } from '../../../shared/data/list-url-state';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/ui/confirm-dialog/confirm-dialog';
import { DataTable, DataTableColumn } from '../../../shared/ui/data-table/data-table';
import { DataTableCell } from '../../../shared/ui/data-table/data-table-templates';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { StatusChip, StatusTone } from '../../../shared/ui/status-chip/status-chip';
import { PickedProduct, ProductPicker } from '../../products/product-picker/product-picker';
import { STOCK_STRINGS } from '../stock.strings';

const TYPES: readonly StockMovementType[] = ['Entry', 'Exit', 'Adjustment', 'Reversal'];
const TONES: Record<StockMovementType, StatusTone> = {
  Entry: 'success',
  Exit: 'danger',
  Adjustment: 'info',
  Reversal: 'neutral',
  Sale: 'primary',
  SaleCancellation: 'warning',
};
const PAGE_SIZE = 50;

/**
 * Histórico de movimentações, do mais recente para o mais antigo. Paginação por chave: "Carregar
 * mais" pede a página seguinte a partir do cursor da última, com o mesmo custo em qualquer ponto
 * de 1 milhão de linhas. Sem total, de propósito (contar tudo é o que fica caro).
 */
@Component({
  selector: 'erp-movement-history',
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
    PageHeader,
    ProductPicker,
    StatusChip,
  ],
  templateUrl: './movement-history.html',
  styleUrl: './movement-history.scss',
})
export class MovementHistory {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly initial = this.route.snapshot.queryParamMap;

  protected readonly strings = STOCK_STRINGS;
  protected readonly canWrite = inject(AuthService).canWrite;
  protected readonly types = TYPES;
  protected readonly tones = TONES;
  protected readonly columns: readonly DataTableColumn[] = [
    { key: 'occurredAt', header: STOCK_STRINGS.columns.occurredAt, role: 'subtitle' },
    { key: 'product', header: STOCK_STRINGS.columns.product, role: 'title' },
    { key: 'type', header: STOCK_STRINGS.columns.type, align: 'center' },
    { key: 'quantity', header: STOCK_STRINGS.columns.movementQuantity, align: 'end' },
    { key: 'unitCost', header: STOCK_STRINGS.columns.unitCost, align: 'end', optional: true },
    { key: 'user', header: STOCK_STRINGS.columns.user, optional: true },
    { key: 'note', header: STOCK_STRINGS.columns.note, optional: true },
    { key: 'actions', header: STOCK_STRINGS.columns.actions, align: 'end', role: 'actions' },
  ];

  protected readonly product = signal<PickedProduct | null>(null);
  protected readonly type = signal<StockMovementType | null>(readType(this.initial.get('type')));
  protected readonly from = signal(readText(this.initial, 'from'));
  protected readonly to = signal(readText(this.initial, 'to'));
  private readonly initialProductId = readOptionalInt(this.initial, 'productId');

  /** Produto vindo da URL (link "Ver histórico" do saldo): carrega SKU e nome para o seletor. */
  private readonly urlProduct = resource({
    params: () => this.initialProductId ?? undefined,
    loader: ({ params }) => this.api.invoke(getProduct, { id: params }),
  });
  private readonly productId = computed(
    () => this.product()?.id ?? (this.urlProduct.isLoading() ? this.initialProductId : null),
  );

  private readonly query = computed<ListStockMovements$Params>(() => ({
    productId: this.productId() ?? undefined,
    type: this.type() ?? undefined,
    from: this.from() ? `${this.from()}T00:00:00Z` : undefined,
    to: this.to() ? nextDay(this.to()) : undefined,
    pageSize: PAGE_SIZE,
  }));

  protected readonly firstPage = resource({
    params: this.query,
    loader: ({ params }) => this.api.invoke(listStockMovements, params),
  });
  /** Páginas seguintes, acrescentadas pelo "Carregar mais"; zera quando os filtros mudam. */
  private readonly morePages = linkedSignal<unknown, readonly StockMovementDto[]>({
    source: this.query,
    computation: () => [],
  });
  private readonly cursor = linkedSignal(() =>
    this.firstPage.hasValue() ? this.firstPage.value().nextCursor : null,
  );
  protected readonly loadingMore = signal(false);

  protected readonly rows = linkedSignal<
    readonly StockMovementDto[] | null,
    readonly StockMovementDto[]
  >({
    source: () =>
      this.firstPage.hasValue() ? [...this.firstPage.value().items, ...this.morePages()] : null,
    computation: (items, previous) => items ?? previous?.value ?? [],
  });
  protected readonly hasMore = computed(() => this.cursor() !== null);
  protected readonly loadError = computed(() =>
    this.firstPage.status() === 'error' ? STOCK_STRINGS.history.loadError : null,
  );
  protected readonly hasFilters = computed(
    () => this.product() !== null || this.type() !== null || this.from() !== '' || this.to() !== '',
  );
  protected readonly rowKey = (movement: StockMovementDto) => movement.id;

  constructor() {
    effect(() => {
      if (this.urlProduct.hasValue() && this.product() === null) {
        const product = this.urlProduct.value();
        this.product.set({
          id: product.id,
          sku: product.sku,
          name: product.name,
          unitCode: product.unitCode,
        });
      }
    });

    effect(() => {
      void this.router.navigate([], {
        relativeTo: this.route,
        replaceUrl: true,
        queryParams: compactParams({
          productId: this.productId(),
          type: this.type(),
          from: this.from(),
          to: this.to(),
        }),
      });
    });
  }

  protected async loadMore(): Promise<void> {
    const cursor = this.cursor();
    if (!cursor || this.loadingMore()) {
      return;
    }
    this.loadingMore.set(true);
    try {
      const page = await this.api.invoke(listStockMovements, { ...this.query(), cursor });
      this.morePages.update((rows) => [...rows, ...page.items]);
      this.cursor.set(page.nextCursor);
    } catch (error) {
      this.notifier.error(toApiProblem(error).message);
    } finally {
      this.loadingMore.set(false);
    }
  }

  protected clearFilters(): void {
    this.product.set(null);
    this.type.set(null);
    this.from.set('');
    this.to.set('');
  }

  protected canReverse(movement: StockMovementDto): boolean {
    return this.canWrite() && movement.type !== 'Reversal' && movement.reversedById === null;
  }

  protected async reverse(movement: StockMovementDto): Promise<void> {
    const description = `${movement.quantity > 0 ? '−' : '+'}${Math.abs(movement.quantity)} ${movement.unitCode ?? ''}`;
    const ref = this.dialog.open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
      data: {
        title: STOCK_STRINGS.history.reverseTitle,
        message: STOCK_STRINGS.history.reverseMessage(description.trim()),
        confirmLabel: STOCK_STRINGS.history.reverse,
        cancelLabel: STOCK_STRINGS.dialog.cancel,
      },
    });
    if (!(await firstValueFrom(ref.afterClosed()))) {
      return;
    }
    try {
      await this.api.invoke(reverseStockMovement, { id: movement.id, body: { note: null } });
      this.notifier.success(STOCK_STRINGS.history.reversedNotice);
      this.firstPage.reload();
    } catch (error) {
      this.notifier.error(toApiProblem(error).message);
    }
  }

  protected onDate(target: 'from' | 'to', event: Event): void {
    this[target].set((event.target as HTMLInputElement).value);
  }
}

function readType(value: string | null): StockMovementType | null {
  return TYPES.find((type) => type === value) ?? null;
}

/** "Até" inclui o dia inteiro: a API recebe o início do dia seguinte (limite exclusivo). */
function nextDay(date: string): string {
  const next = new Date(`${date}T00:00:00Z`);
  next.setUTCDate(next.getUTCDate() + 1);
  return next.toISOString();
}
