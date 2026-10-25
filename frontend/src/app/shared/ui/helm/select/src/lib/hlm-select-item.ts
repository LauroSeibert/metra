import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Icon } from '../../../../icon/icon';
import { BrnSelectItem } from '@spartan-ng/brain/select';
import { classes } from '@spartan-ng/helm/utils';

@Component({
  selector: 'hlm-select-item',
  exportAs: 'hlmSelectItem',
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  hostDirectives: [{ directive: BrnSelectItem, inputs: ['id', 'disabled', 'value'] }],
  host: { 'data-slot': 'select-item' },
  template: `
    <ng-content />
    @if (_active()) {
      <erp-icon name="check" [size]="16" class="absolute end-2 flex items-center justify-center" />
    }
  `,
})
export class HlmSelectItem {
  private readonly _brnSelectItem = inject(BrnSelectItem);

  protected readonly _active = this._brnSelectItem.active;

  constructor() {
    classes(
      () =>
        'data-highlighted:bg-accent data-highlighted:text-accent-foreground not-data-[variant=destructive]:data-highlighted:**:text-accent-foreground gap-2 rounded-sm py-1.5 ps-2 pe-8 text-sm *:[span]:last:flex *:[span]:last:items-center *:[span]:last:gap-2 relative flex w-full cursor-pointer items-center outline-hidden select-none data-disabled:pointer-events-none data-disabled:opacity-50 [&_ng-icon]:pointer-events-none [&_ng-icon]:shrink-0',
    );
  }
}
