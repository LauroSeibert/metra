import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  linkedSignal,
  resource,
} from '@angular/core';
import {
  apply,
  disabled,
  form,
  FormField,
  maxLength,
  required,
  schema,
  submit,
  validate,
} from '@angular/forms/signals';
import { HlmAvatar, HlmAvatarFallback } from '@spartan-ng/helm/avatar';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCard } from '@spartan-ng/helm/card';
import {
  HlmEmpty,
  HlmEmptyContent,
  HlmEmptyDescription,
  HlmEmptyHeader,
  HlmEmptyMedia,
  HlmEmptyTitle,
} from '@spartan-ng/helm/empty';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSwitch } from '@spartan-ng/helm/switch';
import { Api } from '../../api/api';
import { getCompanySettings } from '../../api/fn/settings/get-company-settings';
import { updateCompanySettings } from '../../api/fn/settings/update-company-settings';
import { listUsers } from '../../api/fn/users/list-users';
import { CompanySettingsDto } from '../../api/models/company-settings-dto';
import { UserDto } from '../../api/models/user-dto';
import { AuthService } from '../../core/auth/auth.service';
import { toApiProblem } from '../../core/http/api-problem';
import { toServerErrors } from '../../core/http/server-errors';
import { Notifier } from '../../core/notifications/notifier';
import { DataTable, DataTableColumn } from '../../shared/ui/data-table/data-table';
import { DataTableCell } from '../../shared/ui/data-table/data-table-templates';
import { FieldShell } from '../../shared/ui/field-shell/field-shell';
import { Icon } from '../../shared/ui/icon/icon';
import { PageHeader } from '../../shared/ui/page-header/page-header';
import { SectionCard } from '../../shared/ui/section-card/section-card';
import { formatCompanyDocument, initialsOf } from './company-document';
import { SETTINGS_STRINGS } from './settings.strings';

/** Mesmos limites do servidor (SettingsService). */
const RULES = { companyNameMaxLength: 200, companyDocumentMaxLength: 20 } as const;

interface SettingsModel {
  companyName: string;
  companyDocument: string;
  maxDiscountPercent: number;
  allowNegativeStock: boolean;
}

const messages = SETTINGS_STRINGS.validation;
const settingsSchema = schema<SettingsModel>((path) => {
  required(path.companyName, { message: messages.required });
  maxLength(path.companyName, RULES.companyNameMaxLength, {
    message: messages.maxLength(RULES.companyNameMaxLength),
  });
  maxLength(path.companyDocument, RULES.companyDocumentMaxLength, {
    message: messages.maxLength(RULES.companyDocumentMaxLength),
  });
  validate(path.maxDiscountPercent, ({ value }) =>
    !(value() >= 0 && value() <= 100) ? { kind: 'range', message: messages.discount } : undefined,
  );
});

function toModel(dto: CompanySettingsDto | null): SettingsModel {
  return {
    companyName: dto?.companyName ?? '',
    companyDocument: dto?.companyDocument ?? '',
    maxDiscountPercent: dto?.maxDiscountPercent ?? 0,
    allowNegativeStock: dto?.allowNegativeStock ?? false,
  };
}

/** Configurações: dados e regras da empresa (Admin edita) e a lista de usuários (só Admin vê). */
@Component({
  selector: 'erp-settings',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    HlmAvatar,
    HlmAvatarFallback,
    HlmBadge,
    HlmButton,
    HlmCard,
    HlmEmpty,
    HlmEmptyContent,
    HlmEmptyDescription,
    HlmEmptyHeader,
    HlmEmptyMedia,
    HlmEmptyTitle,
    HlmInput,
    HlmSwitch,
    DataTable,
    DataTableCell,
    FieldShell,
    Icon,
    PageHeader,
    SectionCard,
  ],
  templateUrl: './settings.html',
})
export class Settings {
  private readonly api = inject(Api);
  private readonly notifier = inject(Notifier);

  protected readonly strings = SETTINGS_STRINGS;
  protected readonly isAdmin = inject(AuthService).isAdmin;
  protected readonly columns: readonly DataTableColumn[] = [
    { key: 'fullName', header: SETTINGS_STRINGS.columns.name, role: 'title' },
    { key: 'email', header: SETTINGS_STRINGS.columns.email, role: 'subtitle' },
    { key: 'role', header: SETTINGS_STRINGS.columns.role },
    { key: 'status', header: SETTINGS_STRINGS.columns.status },
  ];

  protected readonly settings = resource({
    loader: () => this.api.invoke(getCompanySettings, {}),
  });
  protected readonly users = resource({
    params: () => (this.isAdmin() ? true : undefined),
    loader: () => this.api.invoke(listUsers, { pageSize: 100 }),
  });

  protected readonly model = linkedSignal(() =>
    toModel(this.settings.hasValue() ? this.settings.value() : null),
  );
  protected readonly settingsForm = form(this.model, (path) => {
    apply(path, settingsSchema);
    disabled(path, () => !this.isAdmin());
  });
  protected readonly formErrors = computed(() => this.settingsForm().errors());

  protected readonly userRows = computed<readonly UserDto[]>(() =>
    this.users.hasValue() ? this.users.value().items : [],
  );
  protected readonly userTotal = computed(() =>
    this.users.hasValue() ? this.users.value().totalCount : 0,
  );
  protected readonly usersError = computed(() =>
    this.users.status() === 'error' ? SETTINGS_STRINGS.usersError : null,
  );
  protected readonly userKey = (user: UserDto) => user.id;
  protected readonly inactive = (user: UserDto) => !user.active;
  protected readonly initials = initialsOf;

  protected setAllowNegative(checked: boolean): void {
    this.model.update((model) => ({ ...model, allowNegativeStock: checked }));
  }

  /** Aplica a máscara do CNPJ antes de o campo ler o valor digitado. */
  protected maskDocument(event: Event): void {
    const input = event.target as HTMLInputElement;
    input.value = formatCompanyDocument(input.value);
    this.model.update((model) => ({ ...model, companyDocument: input.value }));
  }

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    await submit(this.settingsForm, async () => {
      const value = this.model();
      try {
        const saved = await this.api.invoke(updateCompanySettings, {
          body: {
            companyName: value.companyName.trim(),
            companyDocument: value.companyDocument.trim() || null,
            maxDiscountPercent: value.maxDiscountPercent,
            allowNegativeStock: value.allowNegativeStock,
          },
        });
        this.settings.set(saved);
        this.notifier.success(SETTINGS_STRINGS.saved);
        return undefined;
      } catch (error) {
        return toServerErrors(this.settingsForm, toApiProblem(error));
      }
    });
  }
}
