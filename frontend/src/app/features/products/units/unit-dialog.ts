import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, maxLength, required, submit } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Api } from '../../../api/api';
import { createUnit } from '../../../api/fn/units/create-unit';
import { updateUnit } from '../../../api/fn/units/update-unit';
import { UnitDto } from '../../../api/models/unit-dto';
import { toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { CATALOG_RULES } from '../product-rules';
import { PRODUCTS_STRINGS } from '../products.strings';

/** Cria ou edita uma unidade de medida. Fecha com `true` quando salva. */
@Component({
  selector: 'erp-unit-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule],
  template: `
    <form novalidate (submit)="save($event)">
      <h2 mat-dialog-title>{{ unit ? strings.editUnit : strings.newUnit }}</h2>
      <mat-dialog-content>
        <mat-form-field>
          <mat-label>{{ strings.code }}</mat-label>
          <input matInput cdkFocusInitial [formField]="unitForm.code" autocomplete="off" />
          @if (unitForm.code().errors()[0]; as error) {
            <mat-error>{{ error.message }}</mat-error>
          }
        </mat-form-field>
        <mat-form-field>
          <mat-label>{{ strings.name }}</mat-label>
          <input matInput [formField]="unitForm.name" />
          @if (unitForm.name().errors()[0]; as error) {
            <mat-error>{{ error.message }}</mat-error>
          }
        </mat-form-field>
        @for (error of unitForm().errors(); track $index) {
          <p class="error" role="alert">{{ error.message }}</p>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button matButton type="button" mat-dialog-close>{{ common.cancel }}</button>
        <button
          matButton="filled"
          class="erp-primary-action"
          type="submit"
          [disabled]="unitForm().submitting()"
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
export class UnitDialog {
  private readonly api = inject(Api);
  private readonly notifier = inject(Notifier);
  private readonly dialogRef = inject<MatDialogRef<UnitDialog, boolean>>(MatDialogRef);

  protected readonly unit = inject<UnitDto | null>(MAT_DIALOG_DATA);
  protected readonly strings = PRODUCTS_STRINGS.units;
  protected readonly common = PRODUCTS_STRINGS;
  protected readonly model = signal({ code: this.unit?.code ?? '', name: this.unit?.name ?? '' });
  protected readonly unitForm = form(this.model, (path) => {
    required(path.code, { message: PRODUCTS_STRINGS.validation.required });
    maxLength(path.code, CATALOG_RULES.unitCodeMaxLength, {
      message: PRODUCTS_STRINGS.validation.maxLength(CATALOG_RULES.unitCodeMaxLength),
    });
    required(path.name, { message: PRODUCTS_STRINGS.validation.required });
    maxLength(path.name, CATALOG_RULES.unitNameMaxLength, {
      message: PRODUCTS_STRINGS.validation.maxLength(CATALOG_RULES.unitNameMaxLength),
    });
  });

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    await submit(this.unitForm, async () => {
      const body = { code: this.model().code.trim(), name: this.model().name.trim() };
      try {
        if (this.unit) {
          await this.api.invoke(updateUnit, {
            id: this.unit.id,
            body: { ...body, version: this.unit.version },
          });
          this.notifier.success(this.strings.updated);
        } else {
          await this.api.invoke(createUnit, { body: { ...body, version: null } });
          this.notifier.success(this.strings.created);
        }
        this.dialogRef.close(true);
        return undefined;
      } catch (error) {
        return toServerErrors(this.unitForm, toApiProblem(error), {
          'unit.code_duplicate': 'code',
        });
      }
    });
  }
}
