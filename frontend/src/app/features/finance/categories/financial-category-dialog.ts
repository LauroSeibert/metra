import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, maxLength, required, submit } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Api } from '../../../api/api';
import { createFinancialCategory } from '../../../api/fn/financial-categories/create-financial-category';
import { updateFinancialCategory } from '../../../api/fn/financial-categories/update-financial-category';
import { FinancialCategoryDto } from '../../../api/models/financial-category-dto';
import { FinancialCategoryKind } from '../../../api/models/financial-category-kind';
import { toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { FINANCE_STRINGS } from '../finance.strings';

const NAME_MAX_LENGTH = 100;

/** Cria ou edita uma categoria financeira. Fecha com `true` quando salva. */
@Component({
  selector: 'erp-financial-category-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
  ],
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
        <mat-form-field>
          <mat-label>{{ strings.kind }}</mat-label>
          <mat-select [formField]="categoryForm.kind">
            <mat-option value="Revenue">{{ strings.Revenue }}</mat-option>
            <mat-option value="Expense">{{ strings.Expense }}</mat-option>
          </mat-select>
        </mat-form-field>
        @for (error of categoryForm().errors(); track $index) {
          <p class="error" role="alert">{{ error.message }}</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button matButton type="button" mat-dialog-close>{{ strings.cancel }}</button>
        <button
          matButton="filled"
          class="erp-primary-action"
          type="submit"
          [disabled]="categoryForm().submitting()"
        >
          {{ strings.save }}
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
export class FinancialCategoryDialog {
  private readonly api = inject(Api);
  private readonly notifier = inject(Notifier);
  private readonly dialogRef = inject<MatDialogRef<FinancialCategoryDialog, boolean>>(MatDialogRef);

  protected readonly category = inject<FinancialCategoryDto | null>(MAT_DIALOG_DATA);
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
