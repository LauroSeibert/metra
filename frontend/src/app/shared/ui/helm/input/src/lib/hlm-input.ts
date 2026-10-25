import { Directive } from '@angular/core';
import { BrnFieldControlDescribedBy } from '@spartan-ng/brain/field';
import { BrnInput } from '@spartan-ng/brain/input';
import { classes } from '@spartan-ng/helm/utils';

@Directive({
  selector: '[hlmInput]',
  exportAs: 'hlmInput',
  hostDirectives: [
    { directive: BrnInput, inputs: ['id', 'forceInvalid'] },
    BrnFieldControlDescribedBy,
  ],
  host: { 'data-slot': 'input' },
})
export class HlmInput {
  constructor() {
    classes(
      () =>
        'h-control w-full min-w-0 rounded-md border border-input bg-card px-3 text-sm outline-none transition-[color,box-shadow] placeholder:text-muted-foreground focus-visible:border-ring focus-visible:shadow-focus disabled:pointer-events-none disabled:cursor-not-allowed disabled:opacity-50 data-[matches-spartan-invalid=true]:border-destructive file:inline-flex file:border-0 file:bg-transparent file:text-sm file:font-medium',
    );
  }
}
