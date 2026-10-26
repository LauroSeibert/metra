import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Icon } from '../../../../icon/icon';
import { BrnComboboxItem } from '@spartan-ng/brain/combobox';
import { classes } from '@spartan-ng/helm/utils';

@Component({
  selector: 'hlm-combobox-item',
  exportAs: 'hlmComboboxItem',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  hostDirectives: [{ directive: BrnComboboxItem, inputs: ['id', 'disabled', 'value'] }],
  host: { 'data-slot': 'combobox-item' },
  template: `
    <ng-content />
    @if (_active()) {
      <erp-icon name="check" [size]="16" class="absolute end-2 flex items-center justify-center" />
    }
  `,
})
export class HlmComboboxItem {
  private readonly _brnComboboxItem = inject(BrnComboboxItem);

  protected readonly _active = this._brnComboboxItem.active;

  constructor() {
    classes(
      () =>
        'data-highlighted:bg-accent data-highlighted:text-accent-foreground not-data-[variant=destructive]:data-highlighted:**:text-accent-foreground gap-2 rounded-sm py-1.5 ps-2 pe-8 text-sm relative flex w-full cursor-pointer items-center outline-hidden select-none data-disabled:pointer-events-none data-disabled:opacity-50 data-hidden:hidden [&_ng-icon]:pointer-events-none [&_ng-icon]:shrink-0',
    );
  }
}
