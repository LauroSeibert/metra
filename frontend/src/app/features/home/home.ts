import { CurrencyPipe, DatePipe, DecimalPipe, formatCurrency } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  LOCALE_ID,
  resource,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Api } from '../../api/api';
import { getDashboard } from '../../api/fn/dashboard/get-dashboard';
import { SaleStatus } from '../../api/models/sale-status';
import { AuthService } from '../../core/auth/auth.service';
import { Card } from '../../shared/ui/card/card';
import { KpiItem, KpiStrip } from '../../shared/ui/kpi-strip/kpi-strip';
import { PageHeader } from '../../shared/ui/page-header/page-header';
import { StatusChip, StatusTone } from '../../shared/ui/status-chip/status-chip';
import { HOME_STRINGS } from './home.strings';
import { HlmButton } from '@spartan-ng/helm/button';
import { Icon } from '../../shared/ui/icon/icon';
import { LoadError } from '../../shared/ui/load-error/load-error';

const STATUS_TONE: Record<SaleStatus, StatusTone> = {
  Draft: 'warning',
  Confirmed: 'success',
  Cancelled: 'neutral',
};

/** Tela inicial: indicadores do mês, vendas por dia, mais vendidos e últimas vendas. */
@Component({
  selector: 'erp-home',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    HlmButton,
    Icon,
    LoadError,
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    RouterLink,
    Card,
    KpiStrip,
    PageHeader,
    StatusChip,
  ],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  private readonly api = inject(Api);
  private readonly locale = inject(LOCALE_ID);

  protected readonly strings = HOME_STRINGS;
  protected readonly canWrite = inject(AuthService).canWrite;
  protected readonly dashboard = resource({ loader: () => this.api.invoke(getDashboard, {}) });
  protected readonly data = computed(() =>
    this.dashboard.hasValue() ? this.dashboard.value() : null,
  );

  protected readonly kpiItems = computed<readonly KpiItem[]>(() => {
    const data = this.data();
    const strings = HOME_STRINGS;
    const money = (value: number) => formatCurrency(value, this.locale, 'R$');
    const change =
      data && data.salesLastMonth > 0
        ? Math.round((data.salesThisMonth / data.salesLastMonth - 1) * 100)
        : null;
    return [
      {
        key: 'sales',
        label: strings.salesMonth,
        value: data?.salesThisMonth ?? null,
        format: 'currency',
        hint: data
          ? strings.salesMonthHint(
              data.salesThisMonthCount,
              change === null ? null : (change > 0 ? '+' : '') + change + '%',
            )
          : undefined,
      },
      {
        key: 'receivable',
        label: strings.receivable,
        value: data?.receivableOpen ?? null,
        format: 'currency',
        hint: data ? strings.overdueHint(money(data.receivableOverdue)) : undefined,
      },
      {
        key: 'payable',
        label: strings.payable,
        value: data?.payableOpen ?? null,
        format: 'currency',
        hint: data ? strings.overdueHint(money(data.payableOverdue)) : undefined,
      },
      {
        key: 'lowStock',
        label: strings.lowStock,
        value: data?.lowStockCount ?? null,
        hint: strings.lowStockHint,
      },
      {
        key: 'drafts',
        label: strings.drafts,
        value: data?.draftCount ?? null,
        hint: strings.draftsHint,
      },
    ];
  });

  /** Barras do gráfico diário, com altura relativa ao maior dia. */
  protected readonly daily = computed(() => {
    const days = this.data()?.dailySales ?? [];
    const max = Math.max(1, ...days.map((day) => day.total));
    return days.map((day) => ({ ...day, height: (day.total / max) * 100 }));
  });
  protected readonly top = computed(() => {
    const products = this.data()?.topProducts ?? [];
    const max = Math.max(1, ...products.map((product) => product.total));
    return products.map((product) => ({ ...product, width: (product.total / max) * 100 }));
  });

  protected tone(status: SaleStatus): StatusTone {
    return STATUS_TONE[status];
  }

  protected barLabel(date: string, total: number, count: number): string {
    const [year, month, day] = date.split('-');
    return HOME_STRINGS.dailyBar(
      `${day}/${month}/${year}`,
      formatCurrency(total, this.locale, 'R$'),
      count,
    );
  }
}
