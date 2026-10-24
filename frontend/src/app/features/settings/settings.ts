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
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
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
import { Card } from '../../shared/ui/card/card';
import { DataTable, DataTableColumn } from '../../shared/ui/data-table/data-table';
import { DataTableCell } from '../../shared/ui/data-table/data-table-templates';
import { PageHeader } from '../../shared/ui/page-header/page-header';
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
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSlideToggleModule,
    Card,
    DataTable,
    DataTableCell,
    PageHeader,
  ],
  templateUrl: './settings.html',
  styleUrl: './settings.scss',
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

  protected setAllowNegative(checked: boolean): void {
    this.model.update((model) => ({ ...model, allowNegativeStock: checked }));
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
