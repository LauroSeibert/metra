import { Injectable, computed, inject, signal } from '@angular/core';
import { Api } from '../../api/api';
import { updateMyProfile } from '../../api/fn/profile/update-my-profile';
import { toApiProblem } from '../http/api-problem';
import { Notifier } from '../notifications/notifier';
import { AuthService } from './auth.service';
import { applyTheme } from './theme';

/** Alterna claro/escuro: troca na hora e grava a preferência no perfil. */
@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly auth = inject(AuthService);
  private readonly api = inject(Api);
  private readonly notifier = inject(Notifier);

  readonly isDark = computed(() => this.auth.profile()?.theme === 'Dark');
  readonly saving = signal(false);

  /** Se a API recusar a gravação, volta ao tema anterior. */
  async toggle(): Promise<void> {
    const current = this.auth.profile();
    if (!current || this.saving()) {
      return;
    }
    const theme = current.theme === 'Dark' ? 'Light' : 'Dark';
    applyTheme(theme);
    this.saving.set(true);
    try {
      const { fullName, pageSize, version } = current;
      this.auth.profile.set(
        await this.api.invoke(updateMyProfile, { body: { fullName, theme, pageSize, version } }),
      );
    } catch (error) {
      applyTheme(current.theme);
      this.notifier.error(toApiProblem(error).message);
    } finally {
      this.saving.set(false);
    }
  }
}
