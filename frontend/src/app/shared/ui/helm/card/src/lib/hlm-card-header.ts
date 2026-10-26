import { Directive } from '@angular/core';
import { classes } from '@spartan-ng/helm/utils';

@Directive({
  selector: '[hlmCardHeader],hlm-card-header',
  exportAs: 'hlmCardHeader',
  host: { 'data-slot': 'card-header' },
})
export class HlmCardHeader {
  constructor() {
    classes(
      () =>
        'group/card-header grid auto-rows-min items-start gap-0.5 border-b border-border px-5 py-4 has-data-[slot=card-action]:grid-cols-[1fr_auto]',
    );
  }
}
