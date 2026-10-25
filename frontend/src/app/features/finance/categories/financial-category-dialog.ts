import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, maxLength, required, submit } from '@angular/forms/signals';
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSelectImports } from '@spartan-ng/helm/select';
import { Api } from '../../../api/api';
import { createFinancialCategory } from '../../../api/fn/financial-categories/create-financial-category';
import { updateFinancialCategory } from '../../../api/fn/financial-categories/update-financial-category';
import { FinancialCategoryDto } from '../../../api/models/financial-category-dto';
import { FinancialCategoryKind } from '../../../api/models/financial-category-kind';
import { toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { FieldShell } from '../../../shared/ui/field-shell/field-shell';
import { FINANCE_STRINGS } from '../finance.strings';

const NAME_MAX_LENGTH = 100;

/** Dados passados a quem abre o diálogo: `null` cria, uma categoria edita. */
export interface FinancialCategoryDialogContext {
  readonly category: FinancialCategoryDto | null;
}

/** Cria ou edita uma categoria financeira. Fecha com `true` quando salva. */
@Component({
  selector: 'erp-financial-category-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField, HlmButton, HlmDialogImports, HlmInput, HlmSelectImports, FieldShell],
  template: `
    <form class="grid gap-4" novalidate (submit)="save($event)">
      <hlm-dialog-header>
        <h3 hlmDialogTitle>{{ category ? strings.editCategory : strings.newCategory }}</h3>
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
      <erp-field-shell [label]="strings.kind">
        <hlm-select
          [value]="model().kind"
          [itemToString]="kindLabel"
          (valueChange)="setKind($event)"
        >
          <hlm-select-trigger class="w-full">
            <hlm-select-value />
          </hlm-select-trigger>
          <hlm-select-content *hlmSelectPortal>
            <hlm-select-item value="Revenue">{{ strings.Revenue }}</hlm-select-item>
            <hlm-select-item value="Expense">{{ strings.Expense }}</hlm-select-item>
          </hlm-select-content>
        </hlm-select>
      </erp-field-shell>
      @for (error of categoryForm().errors(); track $index) {
        <p class="m-0 text-sm text-destructive" role="alert">{{ error.message }}</p>
      }

      <hlm-dialog-footer>
        <button hlmBtn variant="outline" type="button" hlmDialogClose>{{ strings.cancel }}</button>
        <button hlmBtn type="submit" [disabled]="categoryForm().submitting()">
          {{ strings.save }}
        </button>
      </hlm-dialog-footer>
    </form>
  `,
})
export class FinancialCategoryDialog {
  private readonly api = inject(Api);
  private readonly notifier = inject(Notifier);
  private readonly dialogRef = inject<BrnDialogRef<boolean>>(BrnDialogRef);

  protected readonly category = injectBrnDialogContext<FinancialCategoryDialogContext>().category;
  protected readonly strings = FINANCE_STRINGS.categories;
  protected readonly model = signal<{ name: string; kind: FinancialCategoryKind }>({
    name: this.category?.name ?? '',
    kind: this.category?.kind ?? 'Expense',
  });
  protected readonly categoryForm = form(this.model, (path) => {
    required(path.name, { message: FINANCE_STRINGS.validation.required });
    maxLength(path.name, NAME_MAX_LENGTH, {
      message: FINANCE_STRINGS.validation.maxLength(NAME_MAX_LENGTH),
    });
  });

  protected readonly kindLabel = (kind: FinancialCategoryKind): string => this.strings[kind];

  protected setKind(kind: FinancialCategoryKind | null | undefined): void {
    if (kind) {
      this.model.update((value) => ({ ...value, kind }));
    }
  }

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    await submit(this.categoryForm, async () => {
      const { name, kind } = this.model();
      try {
        if (this.category) {
          await this.api.invoke(updateFinancialCategory, {
            id: this.category.id,
            body: { name: name.trim(), kind, version: this.category.version },
          });
          this.notifier.success(this.strings.updated);
        } else {
          await this.api.invoke(createFinancialCategory, {
            body: { name: name.trim(), kind, version: null },
          });
          this.notifier.success(this.strings.created);
        }
        this.dialogRef.close(true);
        return undefined;
      } catch (error) {
        return toServerErrors(this.categoryForm, toApiProblem(error), {
          'financial_category.name_duplicate': 'name',
        });
      }
    });
  }
}
