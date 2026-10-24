import { ChangeDetectionStrategy, Component, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { LAYOUT_STRINGS } from '../layout.strings';

@Component({
  selector: 'erp-top-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, MatTooltipModule, RouterLink],
  templateUrl: './top-bar.html',
  styleUrl: './top-bar.scss',
})
export class TopBar {
  private readonly router = inject(Router);

  readonly showMenuButton = input(false);
  readonly menuOpen = input(false);
  readonly toggleMenu = output<void>();

  protected readonly strings = LAYOUT_STRINGS;

  /** Trilha montada a partir de `data.breadcrumb` das rotas ativas. */
  protected readonly breadcrumb = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      startWith(null),
      map(() => this.collectBreadcrumb()),
    ),
    { initialValue: [] as string[] },
  );

  private collectBreadcrumb(): string[] {
    const labels: string[] = [];
    let current: ActivatedRouteSnapshot | null = this.router.routerState.snapshot.root;
    while (current) {
      // Só o data declarado na própria rota: filhas de caminho vazio herdam o data do pai.
      const label: unknown = current.routeConfig?.data?.['breadcrumb'];
      if (typeof label === 'string') {
        labels.push(label);
      }
      current = current.firstChild;
    }
    return labels;
  }
}
