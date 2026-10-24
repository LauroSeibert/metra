import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { LAYOUT_STRINGS } from '../layout.strings';

@Component({
  selector: 'erp-top-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonModule, MatIconModule, MatTooltipModule],
  templateUrl: './top-bar.html',
  styleUrl: './top-bar.scss',
})
export class TopBar {
  private readonly router = inject(Router);

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
      const label: unknown = current.data['breadcrumb'];
      if (typeof label === 'string') {
        labels.push(label);
      }
      current = current.firstChild;
    }
    return labels;
  }
}
