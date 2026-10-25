import { ChangeDetectionStrategy, Component, computed, inject, input, output } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRouteSnapshot, NavigationEnd, Router } from '@angular/router';
import { HlmAvatar, HlmAvatarFallback } from '@spartan-ng/helm/avatar';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';
import { filter, map, startWith } from 'rxjs';
import { ACCESS_STRINGS } from '../../../features/access/access.strings';
import { AuthService } from '../../auth/auth.service';
import { ThemeService } from '../../auth/theme.service';
import { Icon } from '../../../shared/ui/icon/icon';
import { CommandPaletteService } from '../command-palette/command-palette.service';
import { LAYOUT_STRINGS } from '../layout.strings';
import { navGroupOf } from '../nav-items';

@Component({
  selector: 'erp-top-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmAvatar, HlmAvatarFallback, HlmBadge, HlmButton, HlmDropdownMenuImports, Icon],
  templateUrl: './top-bar.html',
  host: {
    class:
      'flex h-topbar items-center gap-4 border-b border-border bg-card px-page text-foreground',
  },
})
export class TopBar {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  protected readonly theme = inject(ThemeService);
  protected readonly palette = inject(CommandPaletteService);

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

  protected openProfile(): void {
    void this.router.navigateByUrl('/profile');
  }

  protected logout(): void {
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

  /** Trilha: o grupo do menu ("Cadastros") e os data.breadcrumb das rotas ativas. */
  private collectBreadcrumb(): string[] {
    const group = navGroupOf(this.router.url);
    const labels: string[] = group ? [group] : [];
    let current: ActivatedRouteSnapshot | null = this.router.routerState.snapshot.root;
    while (current) {
      // Só o data declarado na própria rota: filhas de caminho vazio herdam o data do pai.
      const label: unknown = current.routeConfig?.data?.['breadcrumb'];
      if (typeof label === 'string') {
        labels.push(label);
      }
      current = current.firstChild;
    }
    // Grupo com o mesmo nome do item (Financeiro › Financeiro) aparece uma vez só.
    return labels.length > 1 && labels[0] === labels[1] ? labels.slice(1) : labels;
  }
}
