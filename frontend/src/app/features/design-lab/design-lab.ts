import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { HlmAvatar, HlmAvatarFallback } from '@spartan-ng/helm/avatar';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import {
  HlmCard,
  HlmCardContent,
  HlmCardDescription,
  HlmCardHeader,
  HlmCardTitle,
} from '@spartan-ng/helm/card';
import {
  HlmEmpty,
  HlmEmptyContent,
  HlmEmptyDescription,
  HlmEmptyHeader,
  HlmEmptyMedia,
  HlmEmptyTitle,
} from '@spartan-ng/helm/empty';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSeparator } from '@spartan-ng/helm/separator';
import { HlmSkeleton } from '@spartan-ng/helm/skeleton';
import { HlmSwitch } from '@spartan-ng/helm/switch';
import { applyTheme } from '../../core/auth/theme';
import { FieldShell } from '../../shared/ui/field-shell/field-shell';
import { Icon } from '../../shared/ui/icon/icon';
import { ICONS, type IconName } from '../../shared/ui/icon/icons';
import { MoneyInput } from '../../shared/ui/money-input/money-input';
import { PageHeader } from '../../shared/ui/page-header/page-header';
import { StatusChip } from '../../shared/ui/status-chip/status-chip';
import { DESIGN_LAB_STRINGS } from './design-lab.strings';

/** Página temporária com todos os primitivos do redesign e seus estados (fase 2). */
@Component({
  selector: 'erp-design-lab',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    HlmAvatar,
    HlmAvatarFallback,
    HlmBadge,
    HlmButton,
    HlmCard,
    HlmCardContent,
    HlmCardDescription,
    HlmCardHeader,
    HlmCardTitle,
    HlmEmpty,
    HlmEmptyContent,
    HlmEmptyDescription,
    HlmEmptyHeader,
    HlmEmptyMedia,
    HlmEmptyTitle,
    HlmInput,
    HlmSeparator,
    HlmSkeleton,
    HlmSwitch,
    FieldShell,
    Icon,
    MoneyInput,
    PageHeader,
    StatusChip,
  ],
  templateUrl: './design-lab.html',
})
export class DesignLab {
  protected readonly strings = DESIGN_LAB_STRINGS;
  protected readonly iconNames = Object.keys(ICONS) as IconName[];
  protected readonly price = signal<number | null>(12.9);
  protected readonly dark = signal(document.documentElement.dataset['theme'] === 'dark');

  protected toggleTheme(): void {
    const next = !this.dark();
    this.dark.set(next);
    applyTheme(next ? 'Dark' : 'Light');
  }
}
