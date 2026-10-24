import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { AppShell } from '../../shared/ui/app-shell/app-shell';
import { SideNav } from './side-nav/side-nav';
import { TopBar } from './top-bar/top-bar';

@Component({
  selector: 'erp-main-layout',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AppShell, SideNav, TopBar, RouterOutlet],
  template: `
    <erp-app-shell>
      <erp-top-bar shellTopBar />
      <erp-side-nav shellNav />
      <router-outlet />
    </erp-app-shell>
  `,
})
export class MainLayout {}
