import { Directive } from '@angular/core';
import { classes } from '@spartan-ng/helm/utils';

@Directive({
  selector: '[hlmCardDescription]',
  exportAs: 'hlmCardDescription',
  host: { 'data-slot': 'card-description' },
})
export class HlmCardDescription {
  constructor() {
    classes(() => 'm-0 text-muted-foreground text-label');
  }
}
