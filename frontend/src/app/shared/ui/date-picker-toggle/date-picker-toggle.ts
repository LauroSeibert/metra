import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { UI_STRINGS } from '../ui.strings';

/**
 * Botão de calendário para um `<input type="date">` dentro de `mat-form-field` (`matSuffix`).
 * Abre o seletor de datas do navegador; o indicador nativo fica oculto (ver styles.scss).
 */
@Component({
  selector: 'button[erpDatePickerToggle]',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule],
  host: {
    type: 'button',
    class: 'erp-date-toggle',
    '[attr.aria-label]': 'strings.openCalendar',
    '(click)': 'open()',
  },
  template: `<mat-icon aria-hidden="true">calendar_today</mat-icon>`,
})
export class DatePickerToggle {
  readonly erpDatePickerToggle = input.required<HTMLInputElement>();
  protected readonly strings = UI_STRINGS;

  protected open(): void {
    const field = this.erpDatePickerToggle();
    field.focus();
    try {
      field.showPicker();
    } catch {
      // Sem showPicker (ou campo desabilitado): o foco no campo já permite digitar a data.
    }
  }
}
