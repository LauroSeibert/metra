import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, maxLength, required, submit } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Api } from '../../../api/api';
import { createProductCategory } from '../../../api/fn/product-categories/create-product-category';
import { updateProductCategory } from '../../../api/fn/product-categories/update-product-category';
import { ProductCategoryDto } from '../../../api/models/product-category-dto';
import { toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { CATALOG_RULES } from '../product-rules';
import { PRODUCTS_STRINGS } from '../products.strings';

/** Cria ou renomeia uma categoria. Fecha com `true` quando salva. */
@Component({
  selector: 'erp-category-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  template: `
    <form novalidate (submit)="save($event)">
      <h2 mat-dialog-title>{{ category ? strings.editCategory : strings.newCategory }}</h2>
      <mat-dialog-content>
        <mat-form-field>
          <mat-label>{{ strings.name }}</mat-label>
          <input matInput cdkFocusInitial [formField]="categoryForm.name" />
          @if (categoryForm.name().errors()[0]; as error) {
            <mat-error>{{ error.message }}</mat-error>
          }
        </mat-form-field>
        @for (error of categoryForm().errors(); track $index) {
          <p class="error" role="alert">{{ error.message }}</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button matButton type="button" mat-dialog-close>{{ common.cancel }}</button>
        <button
          matButton="filled"
          class="erp-primary-action"
          type="submit"
          [disabled]="categoryForm().submitting()"
        >
          {{ common.save }}
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    mat-form-field {
      width: 100%;
    }
    .error {
      margin: 0;
      color: var(--erp-danger);
    }
  `,
})
export class CategoryDialog {
  private readonly api = inject(Api);
  private readonly notifier = inject(Notifier);
  private readonly dialogRef = inject<MatDialogRef<CategoryDialog, boolean>>(MatDialogRef);

  protected readonly category = inject<ProductCategoryDto | null>(MAT_DIALOG_DATA);
  protected readonly strings = PRODUCTS_STRINGS.categories;
  protected readonly common = PRODUCTS_STRINGS;
  protected readonly model = signal({ name: this.category?.name ?? '' });
  protected readonly categoryForm = form(this.model, (path) => {
    required(path.name, { message: PRODUCTS_STRINGS.validation.required });
    maxLength(path.name, CATALOG_RULES.categoryNameMaxLength, {
      message: PRODUCTS_STRINGS.validation.maxLength(CATALOG_RULES.categoryNameMaxLength),
    });
  });

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    await submit(this.categoryForm, async () => {
      const name = this.model().name.trim();
      try {
        if (this.category) {
          await this.api.invoke(updateProductCategory, {
            id: this.category.id,
            body: { name, version: this.category.version },
          });
          this.notifier.success(this.strings.updated);
        } else {
          await this.api.invoke(createProductCategory, { body: { name, version: null } });
          this.notifier.success(this.strings.created);
        }
        this.dialogRef.close(true);
        return undefined;
      } catch (error) {
        return toServerErrors(this.categoryForm, toApiProblem(error), {
          'product_category.name_duplicate': 'name',
        });
      }
    });
  }
}
