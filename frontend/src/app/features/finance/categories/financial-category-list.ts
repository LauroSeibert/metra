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
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Api } from '../../../api/api';
import { deleteFinancialCategory } from '../../../api/fn/financial-categories/delete-financial-category';
import { listFinancialCategories } from '../../../api/fn/financial-categories/list-financial-categories';
import { FinancialCategoryDto } from '../../../api/models/financial-category-dto';
import { AuthService } from '../../../core/auth/auth.service';
import { toApiProblem } from '../../../core/http/api-problem';
import { Notifier } from '../../../core/notifications/notifier';
import { ConfirmService } from '../../../shared/ui/confirm-dialog/confirm-dialog';
import {
  DataTable,
  DataTableColumn,
  DataTablePage,
} from '../../../shared/ui/data-table/data-table';
import { DataTableCell } from '../../../shared/ui/data-table/data-table-templates';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { StatusChip } from '../../../shared/ui/status-chip/status-chip';
import { FINANCE_STRINGS } from '../finance.strings';
import {
  FinancialCategoryDialog,
  FinancialCategoryDialogContext,
} from './financial-category-dialog';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDialogService } from '@spartan-ng/helm/dialog';
import { HlmTooltip } from '@spartan-ng/helm/tooltip';
import { Icon } from '../../../shared/ui/icon/icon';

/** Categorias financeiras de receita e de despesa. */
@Component({
  selector: 'erp-financial-category-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    HlmButton,
    HlmTooltip,
    Icon,
    DecimalPipe,
    RouterLink,
    DataTable,
    DataTableCell,
    PageHeader,
    StatusChip,
  ],
  template: `
    <erp-page-header [title]="strings.title" [subtitle]="strings.subtitle">
      <a hlmBtn variant="outline" routerLink="/finance">
        <erp-icon name="arrow-left" />
        {{ strings.back }}
      </a>
      @if (canWrite()) {
        <button hlmBtn type="button" (click)="open(null)">
          <erp-icon name="plus" />
          {{ strings.newCategory }}
        </button>
      }
    </erp-page-header>

    <erp-data-table
      [columns]="columns"
      [rows]="rows()"
      [rowKey]="rowKey"
      [loading]="categories.isLoading()"
      [error]="loadError()"
      [emptyMessage]="strings.empty"
      [total]="total()"
      [page]="page()"
      [pageSize]="pageSize()"
      (pageChange)="onPage($event)"
      (retry)="categories.reload()"
      (rowActivate)="canWrite() && open($event)"
    >
      <ng-template erpCell="kind" [erpCellOf]="rows()" let-category>
        <erp-status-chip
          [label]="strings[category.kind]"
          [tone]="category.kind === 'Revenue' ? 'success' : 'danger'"
          [plain]="true"
        />
      </ng-template>
      <ng-template erpCell="entryCount" [erpCellOf]="rows()" let-category>
        {{ category.entryCount | number }}
      </ng-template>
      <ng-template erpCell="actions" [erpCellOf]="rows()" let-category>
        @if (canWrite()) {
          <div class="inline-flex items-center gap-1">
            <button
              hlmBtn
              variant="ghost"
              size="icon"
              type="button"
              [hlmTooltip]="strings.editCategory"
              [attr.aria-label]="strings.editCategory + ' ' + category.name"
              (click)="open(category)"
            >
              <erp-icon name="pencil" />
            </button>
            <span [hlmTooltip]="category.entryCount > 0 ? strings.inUse : strings.delete">
              <button
                hlmBtn
                variant="ghost"
                size="icon"
                type="button"
                class="text-destructive hover:text-destructive"
                [disabled]="category.entryCount > 0"
                [attr.aria-label]="strings.delete + ' ' + category.name"
                (click)="remove(category)"
              >
                <erp-icon name="trash" />
              </button>
            </span>
          </div>
        }
      </ng-template>
    </erp-data-table>
  `,
})
export class FinancialCategoryList {
  private readonly api = inject(Api);
  private readonly dialogs = inject(HlmDialogService);
  private readonly confirm = inject(ConfirmService);
  private readonly notifier = inject(Notifier);
  private readonly auth = inject(AuthService);

  protected readonly strings = FINANCE_STRINGS.categories;
  protected readonly canWrite = this.auth.canWrite;
  protected readonly columns: readonly DataTableColumn[] = [
    { key: 'name', header: this.strings.name, role: 'title' },
    { key: 'kind', header: this.strings.kind, align: 'center' },
    { key: 'entryCount', header: this.strings.entries, align: 'end' },
    { key: 'actions', header: this.strings.actions, align: 'end', role: 'actions' },
  ];
  protected readonly pageSize = signal(this.auth.pageSize());
  protected readonly page = linkedSignal({ source: this.pageSize, computation: () => 1 });

  protected readonly categories = resource({
    params: () => ({ page: this.page(), pageSize: this.pageSize() }),
    loader: ({ params }) => this.api.invoke(listFinancialCategories, params),
  });
  protected readonly rows = linkedSignal<
    readonly FinancialCategoryDto[] | null,
    readonly FinancialCategoryDto[]
  >({
    source: () => (this.categories.hasValue() ? this.categories.value().items : null),
    computation: (items, previous) => items ?? previous?.value ?? [],
  });
  protected readonly total = computed(() =>
    this.categories.hasValue() ? this.categories.value().totalCount : 0,
  );
  protected readonly loadError = computed(() =>
    this.categories.status() === 'error' ? this.strings.loadError : null,
  );
  protected readonly rowKey = (category: FinancialCategoryDto) => category.id;

  protected onPage(change: DataTablePage): void {
    this.pageSize.set(change.pageSize);
    this.page.set(change.page);
  }

  protected async open(category: FinancialCategoryDto | null): Promise<void> {
    const saved = await firstValueFrom(
      this.dialogs.open<boolean, FinancialCategoryDialogContext>(FinancialCategoryDialog, {
        context: { category },
        contentClass: 'sm:max-w-md',
      }).closed$,
    );
    if (saved) {
      this.categories.reload();
    }
  }

  protected async remove(category: FinancialCategoryDto): Promise<void> {
    const confirmed = await this.confirm.ask({
      title: this.strings.deleteTitle,
      message: this.strings.deleteMessage(category.name),
      confirmLabel: this.strings.delete,
      cancelLabel: this.strings.cancel,
    });
    if (!confirmed) {
      return;
    }
    try {
      await this.api.invoke(deleteFinancialCategory, { id: category.id });
      this.notifier.success(this.strings.deleted);
      this.categories.reload();
    } catch (error) {
      this.notifier.error(toApiProblem(error).message);
    }
  }
}
