import { DecimalPipe } from '@angular/common';
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
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Api } from '../../../api/api';
import { deleteUnit } from '../../../api/fn/units/delete-unit';
import { listUnits } from '../../../api/fn/units/list-units';
import { UnitDto } from '../../../api/models/unit-dto';
import { AuthService } from '../../../core/auth/auth.service';
import { toApiProblem } from '../../../core/http/api-problem';
import { Notifier } from '../../../core/notifications/notifier';
import { debounced } from '../../../shared/data/debounced';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/ui/confirm-dialog/confirm-dialog';
import {
  DataTable,
  DataTableColumn,
  DataTablePage,
} from '../../../shared/ui/data-table/data-table';
import { DataTableCell } from '../../../shared/ui/data-table/data-table-templates';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { StatusChip } from '../../../shared/ui/status-chip/status-chip';
import { PRODUCTS_STRINGS } from '../products.strings';
import { UnitDialog } from './unit-dialog';

@Component({
  selector: 'erp-unit-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe,
    RouterLink,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatTooltipModule,
    DataTable,
    DataTableCell,
    PageHeader,
    StatusChip,
  ],
  templateUrl: './unit-list.html',
})
export class UnitList {
  private readonly api = inject(Api);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly auth = inject(AuthService);

  protected readonly strings = PRODUCTS_STRINGS.units;
  protected readonly common = PRODUCTS_STRINGS;
  protected readonly columns: readonly DataTableColumn[] = [
    { key: 'code', header: this.strings.code },
    { key: 'name', header: this.strings.name },
    { key: 'productCount', header: this.strings.products, align: 'end' },
    { key: 'actions', header: this.strings.actions, align: 'end' },
  ];

  protected readonly search = signal('');
  private readonly searchTerm = debounced(computed(() => this.search().trim()));
  protected readonly canWrite = this.auth.canWrite;
  protected readonly pageSize = signal(this.auth.pageSize());
  protected readonly page = linkedSignal({
    source: () => [this.searchTerm(), this.pageSize()],
    computation: () => 1,
  });

  protected readonly units = resource({
    params: () => ({
      search: this.searchTerm() || undefined,
      page: this.page(),
      pageSize: this.pageSize(),
    }),
    loader: ({ params }) => this.api.invoke(listUnits, params),
  });

  protected readonly rows = linkedSignal<readonly UnitDto[] | null, readonly UnitDto[]>({
    source: () => (this.units.hasValue() ? this.units.value().items : null),
    computation: (items, previous) => items ?? previous?.value ?? [],
  });
  protected readonly total = computed(() =>
    this.units.hasValue() ? this.units.value().totalCount : 0,
  );
  protected readonly loadError = computed(() =>
    this.units.status() === 'error' ? this.strings.loadError : null,
  );
  protected readonly rowKey = (unit: UnitDto) => unit.id;

  protected onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected onPage(change: DataTablePage): void {
    this.pageSize.set(change.pageSize);
    this.page.set(change.page);
  }

  protected async open(unit: UnitDto | null): Promise<void> {
    const ref = this.dialog.open<UnitDialog, UnitDto | null, boolean>(UnitDialog, {
      data: unit,
      width: '28rem',
    });
    if (await firstValueFrom(ref.afterClosed())) {
      this.units.reload();
    }
  }

  protected async remove(unit: UnitDto): Promise<void> {
    const ref = this.dialog.open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
      data: {
        title: this.strings.deleteTitle,
        message: this.strings.deleteMessage(unit.code),
        confirmLabel: this.common.delete,
        cancelLabel: this.common.cancel,
      },
    });
    if (!(await firstValueFrom(ref.afterClosed()))) {
      return;
    }

    try {
      await this.api.invoke(deleteUnit, { id: unit.id });
      this.notifier.success(this.strings.deleted);
      this.units.reload();
    } catch (error) {
      this.notifier.error(toApiProblem(error).message);
    }
  }
}
