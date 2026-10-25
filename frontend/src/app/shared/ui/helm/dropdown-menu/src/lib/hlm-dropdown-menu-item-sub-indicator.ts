import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Icon } from '../../../../icon/icon';
import { classes } from '@spartan-ng/helm/utils';

@Component({
  selector: 'hlm-dropdown-menu-item-sub-indicator',
  exportAs: 'hlmDropdownMenuItemSubIndicator',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: ` <erp-icon name="chevron-right" [size]="16" /> `,
})
export class HlmDropdownMenuItemSubIndicator {
  constructor() {
    classes(() => 'ms-auto flex items-center justify-center');
  }
}
