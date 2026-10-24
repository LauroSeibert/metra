import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LAYOUT_STRINGS } from '../layout.strings';
import { NAV_GROUPS } from '../nav-items';

@Component({
  selector: 'erp-side-nav',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule, MatTooltipModule, RouterLink, RouterLinkActive],
  templateUrl: './side-nav.html',
  styleUrl: './side-nav.scss',
  host: { '[class.compact]': 'compact()' },
})
export class SideNav {
  /** Só ícones (tablet ou menu recolhido no desktop). */
  readonly compact = input(false);
  readonly canCollapse = input(false);
  readonly toggleCollapse = output<void>();

  protected readonly groups = NAV_GROUPS;
  protected readonly strings = LAYOUT_STRINGS;
}
