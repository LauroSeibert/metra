import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ActivatedRouteSnapshot, NavigationEnd, Router, RouterLink } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { ACCESS_STRINGS } from '../../../features/access/access.strings';
import { Api } from '../../../api/api';
import { updateMyProfile } from '../../../api/fn/profile/update-my-profile';
import { AuthService } from '../../auth/auth.service';
import { applyTheme } from '../../auth/theme';
import { toApiProblem } from '../../http/api-problem';
import { Notifier } from '../../notifications/notifier';
import { CommandSearch } from '../command-search/command-search';
import { LAYOUT_STRINGS } from '../layout.strings';
import { navGroupOf } from '../nav-items';

@Component({
  selector: 'erp-top-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommandSearch, MatButtonModule, MatIconModule, RouterLink],
  templateUrl: './top-bar.html',
  styleUrl: './top-bar.scss',
})
export class TopBar {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly api = inject(Api);
  private readonly notifier = inject(Notifier);

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

  protected readonly isDark = computed(() => this.profile()?.theme === 'Dark');
  protected readonly savingTheme = signal(false);

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  protected closeMenu(): void {
    this.host.nativeElement.querySelector<HTMLElement>('#account-menu')?.hidePopover();
  }

  /** Troca o tema na hora e grava a preferência no perfil; se a API recusar, volta ao anterior. */
  protected async toggleTheme(): Promise<void> {
    const current = this.profile();
    if (!current || this.savingTheme()) {
      return;
    }
    const theme = current.theme === 'Dark' ? 'Light' : 'Dark';
    applyTheme(theme);
    this.savingTheme.set(true);
    try {
      const { fullName, pageSize, version } = current;
      this.auth.profile.set(
        await this.api.invoke(updateMyProfile, { body: { fullName, theme, pageSize, version } }),
      );
    } catch (error) {
      applyTheme(current.theme);
      this.notifier.error(toApiProblem(error).message);
    } finally {
      this.savingTheme.set(false);
    }
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
