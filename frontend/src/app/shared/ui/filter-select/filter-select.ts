import { ChangeDetectionStrategy, Component, computed, input, model } from '@angular/core';
import { HlmSelectImports } from '@spartan-ng/helm/select';

export interface FilterOption<T> {
  readonly value: T;
  readonly label: string;
}

/** Select de filtro da listagem, com o rótulo embutido: "Situação  Todas". */
@Component({
  selector: 'erp-filter-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmSelectImports],
  host: { class: 'block' },
  template: `
    <hlm-select [value]="value()" [itemToString]="itemLabel" (valueChange)="onChange($event)">
      <hlm-select-trigger class="w-56" [attr.aria-label]="label()">
        <span class="flex min-w-0 items-center gap-2">
          <span class="text-muted-foreground">{{ label() }}</span>
          <hlm-select-value class="truncate font-medium" />
        </span>
      </hlm-select-trigger>
      <hlm-select-content *hlmSelectPortal>
        @for (option of options(); track option.value) {
          <hlm-select-item [value]="option.value">{{ option.label }}</hlm-select-item>
        }
      </hlm-select-content>
    </hlm-select>
  `,
})
export class FilterSelect<T extends string | number | boolean> {
  readonly label = input.required<string>();
  readonly options = input.required<readonly FilterOption<T>[]>();
  readonly value = model.required<T>();

  private readonly labels = computed(
    () => new Map(this.options().map((option) => [option.value, option.label])),
  );
  protected readonly itemLabel = (value: T): string => this.labels().get(value) ?? '';

  protected onChange(next: T | undefined | null): void {
    if (next !== undefined && next !== null) {
      this.value.set(next);
    }
  }
}
