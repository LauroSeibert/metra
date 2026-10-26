import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, maxLength, required, submit } from '@angular/forms/signals';
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmInput } from '@spartan-ng/helm/input';
import { Api } from '../../../api/api';
import { createProductCategory } from '../../../api/fn/product-categories/create-product-category';
import { updateProductCategory } from '../../../api/fn/product-categories/update-product-category';
import { ProductCategoryDto } from '../../../api/models/product-category-dto';
import { toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { FieldShell } from '../../../shared/ui/field-shell/field-shell';
import { CATALOG_RULES } from '../product-rules';
import { PRODUCTS_STRINGS } from '../products.strings';

/** Dados passados a quem abre o diálogo: `null` cria, uma categoria renomeia. */
export interface CategoryDialogContext {
  readonly category: ProductCategoryDto | null;
}

/**
 * Cria ou renomeia uma categoria. Enter salva, Esc cancela. Fecha com `true` quando salva.
 * Abra com `HlmDialogService.open(CategoryDialog, { context: { category } })`.
 */
@Component({
  selector: 'erp-category-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField, HlmButton, HlmDialogImports, HlmInput, FieldShell],
  template: `
    <form class="grid gap-4" novalidate (submit)="save($event)">
      <hlm-dialog-header>
        <h3 hlmDialogTitle>{{ category ? strings.editCategory : strings.newCategory }}</h3>
        <p hlmDialogDescription>{{ strings.dialogHint }}</p>
      </hlm-dialog-header>

      <erp-field-shell
        [label]="strings.name"
        [required]="true"
        [error]="
          categoryForm.name().touched() ? (categoryForm.name().errors()[0]?.message ?? null) : null
        "
      >
        <input hlmInput autocomplete="off" [formField]="categoryForm.name" />
      </erp-field-shell>
      @for (error of categoryForm().errors(); track $index) {
        <p class="m-0 text-sm text-destructive" role="alert">{{ error.message }}</p>
      }

      <hlm-dialog-footer>
        <button hlmBtn variant="outline" type="button" hlmDialogClose>{{ common.cancel }}</button>
        <button hlmBtn type="submit" [disabled]="categoryForm().submitting()">
          {{ categoryForm().submitting() ? strings.saving : common.save }}
        </button>
      </hlm-dialog-footer>
    </form>
  `,
})
export class CategoryDialog {
  private readonly api = inject(Api);
  private readonly notifier = inject(Notifier);
  private readonly dialogRef = inject<BrnDialogRef<boolean>>(BrnDialogRef);

  protected readonly category = injectBrnDialogContext<CategoryDialogContext>().category;
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
