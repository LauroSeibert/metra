import { Directive } from '@angular/core';
import { classes } from '@spartan-ng/helm/utils';

@Directive({
  selector: '[hlmCardContent]',
  exportAs: 'hlmCardContent',
  host: { 'data-slot': 'card-content' },
})
export class HlmCardContent {
  constructor() {
    classes(() => 'flex flex-col gap-4 p-5');
  }
}
