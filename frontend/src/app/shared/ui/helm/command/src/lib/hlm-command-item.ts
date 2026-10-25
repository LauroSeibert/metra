import { Directive } from '@angular/core';
import { BrnCommandItem } from '@spartan-ng/brain/command';
import { classes } from '@spartan-ng/helm/utils';

@Directive({
  selector: 'button[hlmCommandItem],button[hlm-command-item]',
  exportAs: 'hlmCommandItem',
  hostDirectives: [
    {
      directive: BrnCommandItem,
      inputs: ['value', 'disabled', 'id'],
      outputs: ['selected'],
    },
  ],
  host: {
    'data-slot': 'command-item',
  },
})
export class HlmCommandItem {
  constructor() {
    classes(
      () =>
        "border-0 bg-transparent text-left data-selected:bg-muted data-selected:text-foreground relative flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-hidden select-none in-data-[slot=dialog-content]:rounded-lg! [&_ng-icon:not([class*='text-'])]:text-[length:--spacing(4)] group/command-item w-full data-disabled:pointer-events-none data-disabled:opacity-50 data-hidden:hidden [&>ng-icon]:pointer-events-none [&>ng-icon]:shrink-0",
    );
  }
}
