import { Directive, input } from '@angular/core';
import { classes } from '@spartan-ng/helm/utils';
import { HlmCardConfig, injectHlmCardConfig } from './hlm-card.token';

@Directive({
  selector: '[hlmCard],hlm-card',
  exportAs: 'hlmCard',
  host: {
    'data-slot': 'card',
    '[attr.data-size]': 'size()',
  },
})
export class HlmCard {
  private readonly _defaultConfig = injectHlmCardConfig();
  public readonly size = input<HlmCardConfig['size']>(this._defaultConfig.size);

  constructor() {
    classes(
      () =>
        'group/card flex flex-col overflow-hidden rounded-lg border border-border bg-card text-card-foreground text-sm shadow-xs',
    );
  }
}
