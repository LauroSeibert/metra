import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';

/** Filtro de data da listagem: rótulo embutido e o seletor nativo do navegador. Valor `yyyy-MM-dd`. */
@Component({
  selector: 'erp-date-field',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'block' },
  template: `
    <label
      class="flex h-control w-56 items-center gap-2 rounded-md border border-input bg-card px-3 text-sm focus-within:border-ring focus-within:shadow-focus"
    >
      <span class="text-muted-foreground">{{ label() }}</span>
      <input
        #field
        type="date"
        class="min-w-0 flex-1 border-0 bg-transparent p-0 font-medium text-foreground tabular-nums outline-none"
        [attr.aria-label]="label()"
        [value]="value()"
        (change)="value.set(field.value)"
      />
    </label>
  `,
})
export class DateField {
  readonly label = input.required<string>();
  readonly value = model('');
}
