import {
  ChangeDetectionStrategy,
  Component,
  computed,
  DestroyRef,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import {
  form,
  FormField,
  maxLength,
  pattern,
  required,
  schema,
  submit,
  validate,
} from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { Api } from '../../../api/api';
import { changeMyPassword } from '../../../api/fn/profile/change-my-password';
import { updateMyProfile } from '../../../api/fn/profile/update-my-profile';
import { ThemePreference } from '../../../api/models/theme-preference';
import { AuthService } from '../../../core/auth/auth.service';
import { applyTheme } from '../../../core/auth/theme';
import { API_ERROR_CODES, toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { Card } from '../../../shared/ui/card/card';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import {
  SegmentedControl,
  SegmentedOption,
} from '../../../shared/ui/segmented-control/segmented-control';
import { StatusChip } from '../../../shared/ui/status-chip/status-chip';
import { ACCESS_STRINGS } from '../access.strings';

interface ProfileModel {
  fullName: string;
  theme: ThemePreference;
  pageSize: number;
}

interface PasswordModel {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

/** Mesmas regras da API (AppUser e IdentityOptions); a API continua sendo a autoridade. */
export const PROFILE_RULES = {
  fullNameMaxLength: 100,
  pageSizes: [10, 20, 50, 100],
  password: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$/,
} as const;

const messages = ACCESS_STRINGS.validation;
const profileSchema = schema<ProfileModel>((path) => {
  required(path.fullName, { message: messages.required });
  maxLength(path.fullName, PROFILE_RULES.fullNameMaxLength, {
    message: messages.maxLength(PROFILE_RULES.fullNameMaxLength),
  });
});
const passwordSchema = schema<PasswordModel>((path) => {
  required(path.currentPassword, { message: messages.required });
  required(path.newPassword, { message: messages.required });
  pattern(path.newPassword, PROFILE_RULES.password, { message: messages.passwordRules });
  validate(path.confirmPassword, ({ value, valueOf }) =>
    value() !== valueOf(path.newPassword)
      ? { kind: 'mismatch', message: messages.passwordMismatch }
      : undefined,
  );
});

const EMPTY_PASSWORD: PasswordModel = { currentPassword: '', newPassword: '', confirmPassword: '' };

/** "Meu perfil": dados, preferências (tema e itens por página) e troca de senha. */
@Component({
  selector: 'erp-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    Card,
    PageHeader,
    SegmentedControl,
    StatusChip,
  ],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile {
  private readonly api = inject(Api);
  private readonly auth = inject(AuthService);
  private readonly notifier = inject(Notifier);

  protected readonly strings = ACCESS_STRINGS.profile;
  protected readonly pageSizes = PROFILE_RULES.pageSizes;
  protected readonly themeOptions: readonly SegmentedOption<ThemePreference>[] = [
    { value: 'Light', label: ACCESS_STRINGS.profile.light },
    { value: 'Dark', label: ACCESS_STRINGS.profile.dark },
  ];

  protected readonly profile = this.auth.profile;
  protected readonly roleName = computed(() => {
    const role = this.profile()?.role ?? '';
    return ACCESS_STRINGS.roles[role] ?? role;
  });
  protected readonly model = linkedSignal<ProfileModel>(() => {
    const profile = this.profile();
    return {
      fullName: profile?.fullName ?? '',
      theme: profile?.theme ?? 'Light',
      pageSize: profile?.pageSize ?? 20,
    };
  });
  protected readonly profileForm = form(this.model, profileSchema);
  protected readonly conflict = signal(false);
  protected readonly profileErrors = computed(() => this.profileForm().errors());

  protected readonly password = signal<PasswordModel>({ ...EMPTY_PASSWORD });
  protected readonly passwordForm = form(this.password, passwordSchema);
  protected readonly passwordErrors = computed(() => this.passwordForm().errors());

  constructor() {
    // Prévia de tema não salva não sobrevive à saída da tela.
    inject(DestroyRef).onDestroy(() => {
      const profile = this.auth.profile();
      if (profile) {
        applyTheme(profile.theme);
      }
    });
  }

  /** O tema muda na hora, como prévia; só vira preferência ao salvar. */
  protected previewTheme(theme: ThemePreference): void {
    this.model.update((value) => ({ ...value, theme }));
    applyTheme(theme);
  }

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    this.conflict.set(false);
    const current = this.profile();
    if (!current) {
      return;
    }
    await submit(this.profileForm, async () => {
      try {
        const { fullName, theme, pageSize } = this.model();
        const updated = await this.api.invoke(updateMyProfile, {
          body: { fullName: fullName.trim(), theme, pageSize, version: current.version },
        });
        this.auth.profile.set(updated);
        this.notifier.success(this.strings.saved);
        return undefined;
      } catch (error) {
        const problem = toApiProblem(error);
        this.conflict.set(problem.code === API_ERROR_CODES.concurrencyConflict);
        return toServerErrors(this.profileForm, problem);
      }
    });
  }

  protected async reload(): Promise<void> {
    this.conflict.set(false);
    await this.auth.loadProfile();
  }

  protected async changePassword(event: Event): Promise<void> {
    event.preventDefault();
    await submit(this.passwordForm, async () => {
      try {
        const { currentPassword, newPassword } = this.password();
        await this.api.invoke(changeMyPassword, { body: { currentPassword, newPassword } });
        this.password.set({ ...EMPTY_PASSWORD });
        this.passwordForm().reset();
        this.notifier.success(this.strings.passwordChanged);
        return undefined;
      } catch (error) {
        return toServerErrors(this.passwordForm, toApiProblem(error));
      }
    });
  }
}
