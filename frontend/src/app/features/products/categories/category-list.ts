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
import { deleteProductCategory } from '../../../api/fn/product-categories/delete-product-category';
import { listProductCategories } from '../../../api/fn/product-categories/list-product-categories';
import { ProductCategoryDto } from '../../../api/models/product-category-dto';
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
import { PRODUCTS_STRINGS } from '../products.strings';
import { CategoryDialog } from './category-dialog';

@Component({
  selector: 'erp-category-list',
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
  ],
  templateUrl: './category-list.html',
})
export class CategoryList {
  private readonly api = inject(Api);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly auth = inject(AuthService);

  protected readonly strings = PRODUCTS_STRINGS.categories;
  protected readonly common = PRODUCTS_STRINGS;
  protected readonly columns: readonly DataTableColumn[] = [
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

  protected readonly categories = resource({
    params: () => ({
      search: this.searchTerm() || undefined,
      page: this.page(),
      pageSize: this.pageSize(),
    }),
    loader: ({ params }) => this.api.invoke(listProductCategories, params),
  });

  protected readonly rows = linkedSignal<
    readonly ProductCategoryDto[] | null,
    readonly ProductCategoryDto[]
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
  protected readonly rowKey = (category: ProductCategoryDto) => category.id;

  protected onSearchInput(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  protected onPage(change: DataTablePage): void {
    this.pageSize.set(change.pageSize);
    this.page.set(change.page);
  }

  protected async open(category: ProductCategoryDto | null): Promise<void> {
    const ref = this.dialog.open<CategoryDialog, ProductCategoryDto | null, boolean>(
      CategoryDialog,
      {
        data: category,
        width: '28rem',
      },
    );
    if (await firstValueFrom(ref.afterClosed())) {
      this.categories.reload();
    }
  }

  protected async remove(category: ProductCategoryDto): Promise<void> {
    const ref = this.dialog.open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, {
      data: {
        title: this.strings.deleteTitle,
        message: this.strings.deleteMessage(category.name),
        confirmLabel: this.common.delete,
        cancelLabel: this.common.cancel,
      },
    });
    if (!(await firstValueFrom(ref.afterClosed()))) {
      return;
    }

    try {
      await this.api.invoke(deleteProductCategory, { id: category.id });
      this.notifier.success(this.strings.deleted);
      this.categories.reload();
    } catch (error) {
      this.notifier.error(toApiProblem(error).message);
    }
  }
}
