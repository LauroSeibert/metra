import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { form, FormField, maxLength, required, submit } from '@angular/forms/signals';
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmInput } from '@spartan-ng/helm/input';
import { Api } from '../../../api/api';
import { createUnit } from '../../../api/fn/units/create-unit';
import { updateUnit } from '../../../api/fn/units/update-unit';
import { UnitDto } from '../../../api/models/unit-dto';
import { toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { FieldShell } from '../../../shared/ui/field-shell/field-shell';
import { CATALOG_RULES } from '../product-rules';
import { PRODUCTS_STRINGS } from '../products.strings';

/** Dados passados a quem abre o diálogo: `null` cria, uma unidade edita. */
export interface UnitDialogContext {
  readonly unit: UnitDto | null;
}

/**
 * Cria ou edita uma unidade de medida. Enter salva, Esc cancela. Fecha com `true` quando salva.
 * Abra com `HlmDialogService.open(UnitDialog, { context: { unit } })`.
 */
@Component({
  selector: 'erp-unit-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormField, HlmButton, HlmDialogImports, HlmInput, FieldShell],
  template: `
    <form class="grid gap-4" novalidate (submit)="save($event)">
      <hlm-dialog-header>
        <h3 hlmDialogTitle>{{ unit ? strings.editUnit : strings.newUnit }}</h3>
        <p hlmDialogDescription>{{ strings.dialogHint }}</p>
      </hlm-dialog-header>

      <erp-field-shell
        [label]="strings.code"
        [required]="true"
        [error]="unitForm.code().touched() ? (unitForm.code().errors()[0]?.message ?? null) : null"
      >
        <input hlmInput class="font-mono" autocomplete="off" [formField]="unitForm.code" />
      </erp-field-shell>
      <erp-field-shell
        [label]="strings.name"
        [required]="true"
        [error]="unitForm.name().touched() ? (unitForm.name().errors()[0]?.message ?? null) : null"
      >
        <input hlmInput autocomplete="off" [formField]="unitForm.name" />
      </erp-field-shell>
      @for (error of unitForm().errors(); track $index) {
        <p class="m-0 text-sm text-destructive" role="alert">{{ error.message }}</p>
      }

      <hlm-dialog-footer>
        <button hlmBtn variant="outline" type="button" hlmDialogClose>{{ common.cancel }}</button>
        <button hlmBtn type="submit" [disabled]="unitForm().submitting()">
          {{ unitForm().submitting() ? strings.saving : common.save }}
        </button>
      </hlm-dialog-footer>
    </form>
  `,
})
export class UnitDialog {
  private readonly api = inject(Api);
  private readonly notifier = inject(Notifier);
  private readonly dialogRef = inject<BrnDialogRef<boolean>>(BrnDialogRef);

  protected readonly unit = injectBrnDialogContext<UnitDialogContext>().unit;
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
