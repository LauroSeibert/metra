import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Icon } from '../../../../icon/icon';
import { BrnSelectScrollUp } from '@spartan-ng/brain/select';
import { classes } from '@spartan-ng/helm/utils';

@Component({
  selector: 'hlm-select-scroll-up',
  exportAs: 'hlmSelectScrollUp',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  hostDirectives: [BrnSelectScrollUp],
  template: ` <erp-icon name="chevron-up" [size]="16" /> `,
})
export class HlmSelectScrollUp {
  constructor() {
    classes(
      () =>
        "bg-popover z-10 flex cursor-default items-center justify-center py-1 [&_ng-icon:not([class*='text-'])]:text-[length:--spacing(4)] sticky top-0 w-full data-hidden:hidden",
    );
  }
}
