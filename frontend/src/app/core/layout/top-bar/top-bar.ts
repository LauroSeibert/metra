import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  output,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { ACCESS_STRINGS } from '../../../features/access/access.strings';
import { AuthService } from '../../auth/auth.service';
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
  private readonly auth = inject(AuthService);

  readonly showMenuButton = input(false);
  readonly menuOpen = input(false);
  readonly toggleMenu = output<void>();

  protected readonly strings = LAYOUT_STRINGS;
  protected readonly profile = this.auth.profile;
  protected readonly roleName = computed(() => {
    const role = this.profile()?.role ?? '';
    return ACCESS_STRINGS.roles[role] ?? role;
  });
  /** Iniciais do nome para o avatar ("Maria Silva" → "MS"). */
  protected readonly initials = computed(() => {
    const words = (this.profile()?.fullName ?? '').trim().split(/\s+/).filter(Boolean);
    const letters = words.length > 1 ? [words[0], words[words.length - 1]] : words;
    return letters.map((word) => word[0].toUpperCase()).join('') || '?';
  });

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  protected closeMenu(): void {
    this.host.nativeElement.querySelector<HTMLElement>('#account-menu')?.hidePopover();
  }

  protected logout(): void {
    this.closeMenu();
    void this.auth.logout();
  }

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
