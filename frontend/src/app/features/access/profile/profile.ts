import {
  ChangeDetectionStrategy,
  Component,
  computed,
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
import { Api } from '../../../api/api';
import { changeMyPassword } from '../../../api/fn/profile/change-my-password';
import { updateMyProfile } from '../../../api/fn/profile/update-my-profile';
import { AuthService } from '../../../core/auth/auth.service';
import { API_ERROR_CODES, toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { StatusChip } from '../../../shared/ui/status-chip/status-chip';
import { ACCESS_STRINGS } from '../access.strings';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSelectImports } from '@spartan-ng/helm/select';
import { fieldError } from '../../../shared/ui/field-shell/field-error';
import { FieldShell } from '../../../shared/ui/field-shell/field-shell';
import { FormBanner } from '../../../shared/ui/form-banner/form-banner';
import { Icon } from '../../../shared/ui/icon/icon';
import { SectionCard } from '../../../shared/ui/section-card/section-card';

interface ProfileModel {
  fullName: string;
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

/** "Meu perfil": dados, preferências (itens por página) e troca de senha. */
@Component({
  selector: 'erp-profile',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    HlmButton,
    HlmInput,
    HlmSelectImports,
    FieldShell,
    FormBanner,
    Icon,
    SectionCard,
    FormField,
    PageHeader,
    StatusChip,
  ],
  templateUrl: './profile.html',
})
export class Profile {
  private readonly api = inject(Api);
  private readonly auth = inject(AuthService);
  private readonly notifier = inject(Notifier);

  protected readonly strings = ACCESS_STRINGS.profile;
  protected readonly pageSizes = PROFILE_RULES.pageSizes;
  protected readonly fieldError = fieldError;
  protected readonly sizeLabel = (size: number): string => String(size);

  protected setPageSize(size: number | null | undefined): void {
    if (size) {
      this.profileForm.pageSize().value.set(size);
    }
  }

  protected readonly profile = this.auth.profile;
  protected readonly roleName = computed(() => {
    const role = this.profile()?.role ?? '';
    return ACCESS_STRINGS.roles[role] ?? role;
  });
  protected readonly model = linkedSignal<ProfileModel>(() => {
    const profile = this.profile();
    return {
      fullName: profile?.fullName ?? '',
      pageSize: profile?.pageSize ?? 20,
    };
  });
  protected readonly profileForm = form(this.model, profileSchema);
  protected readonly conflict = signal(false);
  protected readonly profileErrors = computed(() => this.profileForm().errors());

  protected readonly password = signal<PasswordModel>({ ...EMPTY_PASSWORD });
  protected readonly passwordForm = form(this.password, passwordSchema);
  protected readonly passwordErrors = computed(() => this.passwordForm().errors());

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    this.conflict.set(false);
    const current = this.profile();
    if (!current) {
      return;
    }
    await submit(this.profileForm, async () => {
      try {
        const { fullName, pageSize } = this.model();
        const updated = await this.api.invoke(updateMyProfile, {
          body: {
            fullName: fullName.trim(),
            theme: current.theme,
            pageSize,
            version: current.version,
          },
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
