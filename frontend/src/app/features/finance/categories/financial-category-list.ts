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
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Api } from '../../../api/api';
import { deleteFinancialCategory } from '../../../api/fn/financial-categories/delete-financial-category';
import { listFinancialCategories } from '../../../api/fn/financial-categories/list-financial-categories';
import { FinancialCategoryDto } from '../../../api/models/financial-category-dto';
import { AuthService } from '../../../core/auth/auth.service';
import { toApiProblem } from '../../../core/http/api-problem';
import { Notifier } from '../../../core/notifications/notifier';
import { ConfirmDialog, ConfirmDialogData } from '../../../shared/ui/confirm-dialog/confirm-dialog';
import {
  DataTable,
  DataTableColumn,
  DataTablePage,
} from '../../../shared/ui/data-table/data-table';
import { DataTableCell } from '../../../shared/ui/data-table/data-table-templates';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { StatusChip } from '../../../shared/ui/status-chip/status-chip';
import { FINANCE_STRINGS } from '../finance.strings';
import { FinancialCategoryDialog } from './financial-category-dialog';

/** Categorias financeiras de receita e de despesa. */
@Component({
  selector: 'erp-financial-category-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DecimalPipe,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    DataTable,
    DataTableCell,
    PageHeader,
    StatusChip,
  ],
  template: `
    <erp-page-header [title]="strings.title" [subtitle]="strings.subtitle">
      <a matButton="outlined" class="erp-secondary-action" routerLink="/finance">
        <mat-icon>arrow_back</mat-icon>
        {{ strings.back }}
      </a>
      @if (canWrite()) {
        <button matButton="filled" class="erp-primary-action" type="button" (click)="open(null)">
          <mat-icon>add</mat-icon>
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
          <div class="erp-row-actions">
            <button
              class="erp-row-action"
              type="button"
              [matTooltip]="strings.editCategory"
              [attr.aria-label]="strings.editCategory + ' ' + category.name"
              (click)="open(category)"
            >
              <mat-icon>edit_square</mat-icon>
            </button>
            <button
              class="erp-row-action danger"
              type="button"
              [disabled]="category.entryCount > 0"
              [matTooltip]="category.entryCount > 0 ? strings.inUse : strings.delete"
              [attr.aria-label]="strings.delete + ' ' + category.name"
              (click)="remove(category)"
            >
              <mat-icon>delete</mat-icon>
            </button>
          </div>
        }
      </ng-template>
    </erp-data-table>
  `,
})
export class FinancialCategoryList {
  private readonly api = inject(Api);
  private readonly dialog = inject(MatDialog);
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
      this.dialog.open(FinancialCategoryDialog, { data: category, width: '440px' }).afterClosed(),
    );
    if (saved) {
      this.categories.reload();
    }
  }

  protected async remove(category: FinancialCategoryDto): Promise<void> {
    const ref = this.dialog.open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
      data: {
        title: this.strings.deleteTitle,
        message: this.strings.deleteMessage(category.name),
        confirmLabel: this.strings.delete,
        cancelLabel: this.strings.cancel,
      },
    });
    if (!(await firstValueFrom(ref.afterClosed()))) {
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
