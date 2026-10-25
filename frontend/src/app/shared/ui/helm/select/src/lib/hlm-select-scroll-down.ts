import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Icon } from '../../../../icon/icon';
import { BrnSelectScrollDown } from '@spartan-ng/brain/select';
import { classes } from '@spartan-ng/helm/utils';

@Component({
  selector: 'hlm-select-scroll-down',
  exportAs: 'hlmSelectScrollDown',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  hostDirectives: [BrnSelectScrollDown],
  template: ` <erp-icon name="chevron-down" [size]="16" /> `,
})
export class HlmSelectScrollDown {
  constructor() {
    classes(
      () =>
        "bg-popover z-10 flex cursor-default items-center justify-center py-1 [&_ng-icon:not([class*='text-'])]:text-[length:--spacing(4)] sticky bottom-0 w-full data-hidden:hidden",
    );
  }
}
