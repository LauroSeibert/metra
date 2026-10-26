import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { Icon } from '../../../../icon/icon';
import { BrnCommandInput } from '@spartan-ng/brain/command';
import { HlmInputGroupImports } from '@spartan-ng/helm/input-group';
import { classes } from '@spartan-ng/helm/utils';

@Component({
  selector: 'hlm-command-input',
  exportAs: 'hlmCommandInput',
  imports: [HlmInputGroupImports, Icon, BrnCommandInput],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    'data-slot': 'command-input-wrapper',
  },
  template: `
    <hlm-input-group
      class="bg-input/30 border-input/30 h-8! rounded-lg! shadow-none! *:data-[slot=input-group-addon]:pl-2!"
    >
      <input
        brnCommandInput
        data-slot="command-input"
        class="w-full border-0 bg-transparent text-sm outline-hidden disabled:cursor-not-allowed disabled:opacity-50"
        [id]="inputId()"
        [placeholder]="placeholder()"
      />

      <hlm-input-group-addon>
        <erp-icon name="search" [size]="16" />
      </hlm-input-group-addon>
    </hlm-input-group>
  `,
})
export class HlmCommandInput {
  public readonly inputId = input<string | undefined>();
  public readonly placeholder = input<string>('');

  constructor() {
    classes(() => 'p-1 pb-0');
  }
}
