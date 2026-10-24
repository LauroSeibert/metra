import { BreakpointObserver } from '@angular/cdk/layout';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterOutlet } from '@angular/router';
import { filter, map } from 'rxjs';
import { AppShell } from '../../shared/ui/app-shell/app-shell';
import { BREAKPOINTS } from '../../styles/breakpoints';
import { SideNav } from './side-nav/side-nav';
import { TopBar } from './top-bar/top-bar';

const COLLAPSED_KEY = 'erp.nav.collapsed';

/** Decide como o menu aparece em cada largura e guarda a preferência de menu recolhido. */
@Component({
  selector: 'erp-main-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppShell, SideNav, TopBar, RouterOutlet],
  template: `
    <erp-app-shell
      [compact]="compact()"
      [mobile]="isMobile()"
      [navOpen]="navOpen()"
      (closeNav)="navOpen.set(false)"
    >
      <erp-top-bar
        shellTopBar
        [showMenuButton]="isMobile()"
        [menuOpen]="navOpen()"
        (toggleMenu)="navOpen.set(!navOpen())"
      />
      <erp-side-nav
        shellNav
        [compact]="compact()"
        [canCollapse]="!isMobile() && !isTablet()"
        (toggleCollapse)="collapsed.set(!collapsed())"
      />
      <router-outlet />
    </erp-app-shell>
  `,
})
export class MainLayout {
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly router = inject(Router);

  protected readonly isMobile = this.matches(BREAKPOINTS.mobile);
  protected readonly isTablet = this.matches(BREAKPOINTS.tablet);
  protected readonly collapsed = signal(readCollapsed());
  protected readonly navOpen = signal(false);
  protected readonly compact = computed(
    () => !this.isMobile() && (this.isTablet() || this.collapsed()),
  );

  constructor() {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe(() => this.navOpen.set(false));

    effect(() => {
      try {
        localStorage.setItem(COLLAPSED_KEY, String(this.collapsed()));
      } catch {
        // Armazenamento indisponível (modo privado): a preferência só não persiste.
      }
    });
  }

  private matches(query: string) {
    return toSignal(this.breakpoints.observe(query).pipe(map((state) => state.matches)), {
      initialValue: this.breakpoints.isMatched(query),
    });
  }
}

function readCollapsed(): boolean {
  try {
    return localStorage.getItem(COLLAPSED_KEY) === 'true';
  } catch {
    return false;
  }
}
