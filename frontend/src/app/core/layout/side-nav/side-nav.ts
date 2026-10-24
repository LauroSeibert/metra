import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { LAYOUT_STRINGS } from '../layout.strings';
import { NAV_ITEMS } from '../nav-items';

@Component({
  selector: 'erp-side-nav',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule, MatTooltipModule, RouterLink, RouterLinkActive],
  templateUrl: './side-nav.html',
  styleUrl: './side-nav.scss',
})
export class SideNav {
  protected readonly items = NAV_ITEMS;
  protected readonly strings = LAYOUT_STRINGS;
}
