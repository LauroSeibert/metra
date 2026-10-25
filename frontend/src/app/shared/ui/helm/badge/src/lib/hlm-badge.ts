import { Directive, input } from '@angular/core';
import { classes } from '@spartan-ng/helm/utils';
import { type VariantProps, cva } from 'class-variance-authority';

const badgeVariants = cva(
  'inline-flex h-badge w-fit shrink-0 items-center justify-center gap-1.5 overflow-hidden rounded-full border border-transparent px-2.5 text-caption font-medium whitespace-nowrap before:size-1.5 before:shrink-0 before:rounded-full before:bg-current',
  {
    variants: {
      variant: {
        ok: 'bg-success-soft text-success',
        off: 'bg-muted text-muted-foreground',
        warn: 'bg-warning-soft text-warning-text',
        danger: 'bg-danger-soft text-destructive',
        outline: 'border-border text-foreground',
        code: 'rounded-md bg-muted px-2 font-mono text-muted-foreground before:hidden',
      },
    },
    defaultVariants: {
      variant: 'off',
    },
  },
);

export type BadgeVariants = VariantProps<typeof badgeVariants>;

@Directive({
  selector: '[hlmBadge],hlm-badge',
  exportAs: 'hlmBadge',
  host: {
    'data-slot': 'badge',
    '[attr.data-variant]': 'variant()',
  },
})
export class HlmBadge {
  public readonly variant = input<BadgeVariants['variant']>('off');

  constructor() {
    classes(() => badgeVariants({ variant: this.variant() }));
  }
}
