import { Directive } from '@angular/core';
import { BrnComboboxContent } from '@spartan-ng/brain/combobox';
import { classes } from '@spartan-ng/helm/utils';

@Directive({
  selector: '[hlmComboboxContent],hlm-combobox-content',
  exportAs: 'hlmComboboxContent',
  hostDirectives: [{ directive: BrnComboboxContent, inputs: ['id'] }],
  host: { slot: 'combobox-content' },
})
export class HlmComboboxContent {
  constructor() {
    classes(() => [
      'bg-popover text-popover-foreground data-open:animate-in data-closed:animate-out data-closed:fade-out-0 data-open:fade-in-0 data-closed:zoom-out-95 data-open:zoom-in-95 data-[side=bottom]:slide-in-from-top-2 data-[side=left]:slide-in-from-right-2 data-[side=right]:slide-in-from-left-2 data-[side=top]:slide-in-from-bottom-2 max-h-72 min-w-36 overflow-hidden rounded-md border border-border shadow-overlay duration-100 **:data-[slot=input-group]:h-control **:data-[slot=input-group]:rounded-none **:data-[slot=input-group]:border-0 **:data-[slot=input-group]:border-b **:data-[slot=input-group]:border-border **:data-[slot=input-group]:bg-transparent **:data-[slot=input-group]:shadow-none! **:data-[slot=input-group]:focus-within:shadow-none! **:data-[slot=input-group]:focus-within:border-border group/combobox-content relative flex w-(--brn-combobox-width) flex-col p-0',
    ]);
  }
}
