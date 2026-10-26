import { Directive } from '@angular/core';
import { classes } from '@spartan-ng/helm/utils';

@Directive({
  selector: '[hlmEmpty],hlm-empty',
  exportAs: 'hlmEmpty',
  host: { 'data-slot': 'empty' },
})
export class HlmEmpty {
  constructor() {
    classes(
      () =>
        'gap-3 rounded-lg border border-dashed border-border p-10 flex w-full min-w-0 flex-1 flex-col items-center justify-center text-center text-balance',
    );
  }
}
