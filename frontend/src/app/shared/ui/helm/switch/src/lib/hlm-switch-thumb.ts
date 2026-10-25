import { Directive } from '@angular/core';
import { BrnSwitchThumb } from '@spartan-ng/brain/switch';
import { classes } from '@spartan-ng/helm/utils';

@Directive({
  selector: '[hlmSwitchThumb],hlm-switch-thumb',
  exportAs: 'hlmSwitchThumb',
  hostDirectives: [BrnSwitchThumb],
  host: { 'data-slot': 'switch-thumb' },
})
export class HlmSwitchThumb {
  constructor() {
    classes(
      () =>
        'pointer-events-none block switch-thumb rounded-full bg-card shadow-xs ring-0 transition-transform data-unchecked:translate-x-0.5 data-checked:thumb-on data-checked:bg-primary-foreground',
    );
  }
}
