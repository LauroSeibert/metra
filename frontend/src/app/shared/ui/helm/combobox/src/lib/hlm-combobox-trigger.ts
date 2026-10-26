import { BooleanInput } from '@angular/cdk/coercion';
import {
  booleanAttribute,
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
} from '@angular/core';
import { Icon } from '../../../../icon/icon';
import {
  BrnComboboxAnchor,
  BrnComboboxPopoverTrigger,
  BrnComboboxTrigger,
} from '@spartan-ng/brain/combobox';
import { BrnFieldControlDescribedBy } from '@spartan-ng/brain/field';
import { ButtonVariants, HlmButton } from '@spartan-ng/helm/button';
import { hlm } from '@spartan-ng/helm/utils';
import type { ClassValue } from 'clsx';

@Component({
  selector: 'hlm-combobox-trigger',
  exportAs: 'hlmComboboxTrigger',
  imports: [
    Icon,
    HlmButton,
    BrnComboboxAnchor,
    BrnComboboxTrigger,
    BrnComboboxPopoverTrigger,
    BrnFieldControlDescribedBy,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <button
      brnComboboxTrigger
      brnComboboxAnchor
      brnComboboxPopoverTrigger
      brnFieldControlDescribedBy
      hlmBtn
      data-slot="combobox-trigger"
      [id]="buttonId()"
      [class]="_computedClass()"
      [variant]="variant()"
      [forceInvalid]="forceInvalid()"
    >
      <ng-content />
      <erp-icon name="chevron-down" [size]="16" class="text-muted-foreground" />
    </button>
  `,
})
export class HlmComboboxTrigger {
  private static _id = 0;

  public readonly userClass = input<ClassValue>('', {
    alias: 'class',
  });
  protected readonly _computedClass = computed(() =>
    hlm('data-placeholder:text-muted-foreground', this.userClass()),
  );

  public readonly buttonId = input<string>(`hlm-combobox-trigger-${HlmComboboxTrigger._id++}`);

  public readonly variant = input<ButtonVariants['variant']>('outline');

  public readonly forceInvalid = input<boolean, BooleanInput>(false, {
    transform: booleanAttribute,
  });
}
