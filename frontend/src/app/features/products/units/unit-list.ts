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
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDialogService } from '@spartan-ng/helm/dialog';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmTooltip } from '@spartan-ng/helm/tooltip';
import { MatDialog } from '@angular/material/dialog';
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
import { Icon } from '../../../shared/ui/icon/icon';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { SlashFocus } from '../../../shared/ui/slash-focus';
import { PRODUCTS_STRINGS } from '../products.strings';
import { UnitDialog, UnitDialogContext } from './unit-dialog';

@Component({
  selector: 'erp-unit-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe,
    RouterLink,
    HlmBadge,
    HlmButton,
    HlmInput,
    HlmTooltip,
    DataTable,
    DataTableCell,
    Icon,
    PageHeader,
    SlashFocus,
  ],
  templateUrl: './unit-list.html',
})
export class UnitList {
  private readonly api = inject(Api);
  private readonly dialog = inject(MatDialog);
  private readonly dialogs = inject(HlmDialogService);
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

  protected clearSearch(input: HTMLInputElement): void {
    this.search.set('');
    input.focus();
  }

  protected onPage(change: DataTablePage): void {
    this.pageSize.set(change.pageSize);
    this.page.set(change.page);
  }

  protected async open(unit: UnitDto | null): Promise<void> {
    const ref = this.dialogs.open<boolean, UnitDialogContext>(UnitDialog, {
      context: { unit },
      contentClass: 'sm:max-w-md',
    });
    if (await firstValueFrom(ref.closed$)) {
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
