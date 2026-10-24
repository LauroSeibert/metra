import { NgTemplateOutlet } from '@angular/common';
import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LAYOUT_STRINGS } from '../layout.strings';
import { NAV_FOOTER, NAV_GROUPS } from '../nav-items';

@Component({
  selector: 'erp-side-nav',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [NgTemplateOutlet, MatIconModule, RouterLink, RouterLinkActive],
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
  protected readonly footer = NAV_FOOTER;
  protected readonly strings = LAYOUT_STRINGS;
}
