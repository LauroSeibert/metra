import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';

export interface SegmentedOption<T> {
  readonly value: T;
  readonly label: string;
}

/** Filtro rápido em pílula, com a opção ativa em azul (padrão do protótipo). */
@Component({
  selector: 'erp-segmented-control',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="group" role="radiogroup" [attr.aria-label]="label()">
      @for (option of options(); track option.value) {
        <button
          type="button"
          role="radio"
          [class.active]="option.value === value()"
          [attr.aria-checked]="option.value === value()"
          (click)="value.set(option.value)"
        >
          {{ option.label }}
        </button>
      }
    </div>
  `,
  styleUrl: './segmented-control.scss',
})
export class SegmentedControl<T> {
  readonly options = input.required<readonly SegmentedOption<T>[]>();
  readonly value = model.required<T>();
  readonly label = input<string>();
}
