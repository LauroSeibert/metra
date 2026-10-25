import { Directive } from '@angular/core';
import { classes } from '@spartan-ng/helm/utils';

@Directive({
  selector: '[hlmCardFooter],hlm-card-footer',
  exportAs: 'hlmCardFooter',
  host: { 'data-slot': 'card-footer' },
})
export class HlmCardFooter {
  constructor() {
    classes(() => 'flex items-center gap-2 border-t border-border px-5 py-3');
  }
}
