import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Icon } from '../../../../icon/icon';
import { classes } from '@spartan-ng/helm/utils';

@Component({
  selector: 'hlm-dropdown-menu-checkbox-indicator',
  exportAs: 'hlmDropdownMenuCheckboxIndicator',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { 'data-slot': 'dropdown-menu-checkbox-item-indicator' },
  template: ` <erp-icon name="check" [size]="16" /> `,
})
export class HlmDropdownMenuCheckboxIndicator {
  constructor() {
    classes(
      () =>
        'absolute end-2 flex items-center justify-center pointer-events-none opacity-0 group-data-checked/dropdown-menu-checkbox:opacity-100',
    );
  }
}
