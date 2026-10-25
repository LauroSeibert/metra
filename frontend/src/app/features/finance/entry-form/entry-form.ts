import { DatePickerToggle } from '../../../shared/ui/date-picker-toggle/date-picker-toggle';
import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
  resource,
  signal,
} from '@angular/core';
import {
  apply,
  disabled,
  form,
  FormField,
  hidden,
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
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Api } from '../../../api/api';
import { createFinancialEntry } from '../../../api/fn/financial-entries/create-financial-entry';
import { getFinancialEntry } from '../../../api/fn/financial-entries/get-financial-entry';
import { updateFinancialEntry } from '../../../api/fn/financial-entries/update-financial-entry';
import { listFinancialCategories } from '../../../api/fn/financial-categories/list-financial-categories';
import { FinancialEntryDto } from '../../../api/models/financial-entry-dto';
import { FinancialEntryKind } from '../../../api/models/financial-entry-kind';
import { AuthService } from '../../../core/auth/auth.service';
import { API_ERROR_CODES, toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { delayedFlag } from '../../../shared/data/delayed-flag';
import { Card } from '../../../shared/ui/card/card';
import { MoneyInput } from '../../../shared/ui/money-input/money-input';
import { PageHeader } from '../../../shared/ui/page-header/page-header';
import { StatusChip } from '../../../shared/ui/status-chip/status-chip';
import { PartnerPicker, PickedPartner } from '../../partners/partner-picker/partner-picker';
import { FINANCE_STRINGS } from '../finance.strings';
import { todayIso } from '../pay-dialog/pay-dialog';

/** Mesmos limites do servidor (FinancialEntry). */
export const FINANCE_RULES = { descriptionMaxLength: 200, maxInstallments: 60 } as const;

interface EntryModel {
  description: string;
  categoryId: number | null;
  amount: number | null;
  dueDate: string;
  installments: number;
  editing: boolean;
}

interface Installment {
  readonly number: number;
  readonly amount: number;
  readonly dueDate: string;
}

/**
 * Parcelas como o servidor vai gravar: total dividido, centavos que sobram na primeira, vencimentos
 * mensais (dia 31 vira o último dia dos meses mais curtos).
 */
export function previewInstallments(total: number, count: number, firstDue: string): Installment[] {
  if (!total || count < 1 || count > FINANCE_RULES.maxInstallments || !firstDue) {
    return [];
  }
  const cents = Math.round(total * 100);
  const share = Math.floor(cents / count);
  const [year, month, day] = firstDue.split('-').map(Number);
  return Array.from({ length: count }, (_, index) => {
    const lastDay = new Date(Date.UTC(year, month - 1 + index + 1, 0)).getUTCDate();
    const due = new Date(Date.UTC(year, month - 1 + index, Math.min(day, lastDay)));
    return {
      number: index + 1,
      amount: (index === 0 ? cents - share * (count - 1) : share) / 100,
      dueDate: due.toISOString().slice(0, 10),
    };
  });
}

const messages = FINANCE_STRINGS.validation;
const entrySchema = schema<EntryModel>((path) => {
  required(path.description, { message: messages.required });
  maxLength(path.description, FINANCE_RULES.descriptionMaxLength, {
    message: messages.maxLength(FINANCE_RULES.descriptionMaxLength),
  });
  required(path.categoryId, { message: messages.required });
  required(path.amount, { message: messages.required });
  validate(path.amount, ({ value }) =>
    value() !== null && (value() ?? 0) <= 0
      ? { kind: 'min', message: messages.positive }
      : undefined,
  );
  required(path.dueDate, { message: messages.required });
  hidden(path.installments, ({ valueOf }) => valueOf(path.editing));
  validate(path.installments, ({ value }) =>
    !Number.isInteger(value()) || value() < 1 || value() > FINANCE_RULES.maxInstallments
      ? { kind: 'range', message: messages.installments(FINANCE_RULES.maxInstallments) }
      : undefined,
  );
});

/** Cadastro (com parcelas) e edição de um lançamento aberto. O tipo vem da rota. */
@Component({
  selector: 'erp-entry-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DatePickerToggle,
    CurrencyPipe,
    DatePipe,
    RouterLink,
    FormField,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    Card,
    MoneyInput,
    PageHeader,
    PartnerPicker,
    StatusChip,
  ],
  templateUrl: './entry-form.html',
  styleUrl: './entry-form.scss',
})
export class EntryForm {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly notifier = inject(Notifier);

  readonly kind = input.required<FinancialEntryKind>();
  readonly id = input<string>();

  protected readonly strings = FINANCE_STRINGS;
  protected readonly texts = computed(() => FINANCE_STRINGS.kinds[this.kind()]);
  protected readonly canWrite = inject(AuthService).canWrite;
  protected readonly isEdit = computed(() => this.id() !== undefined);
  private readonly entryId = computed(() => {
    const id = Number(this.id());
    return Number.isInteger(id) && id > 0 ? id : null;
  });

  protected readonly entry = resource({
    params: () => this.entryId() ?? undefined,
    loader: ({ params }) => this.api.invoke(getFinancialEntry, { id: params }),
  });
  protected readonly categories = resource({
    params: () => (this.kind() === 'Receivable' ? 'Revenue' : 'Expense'),
    loader: ({ params }) =>
      this.api.invoke(listFinancialCategories, { kind: params, pageSize: 100 }),
  });
  protected readonly categoryOptions = computed(() =>
    this.categories.hasValue() ? this.categories.value().items : [],
  );

  protected readonly partner = linkedSignal<PickedPartner | null>(() => {
    const entry = this.entry.hasValue() ? this.entry.value() : null;
    return entry?.partnerId && entry.partnerName
      ? { id: entry.partnerId, name: entry.partnerName, document: '' }
      : null;
  });
  protected readonly model = linkedSignal<EntryModel>(() => {
    const entry = this.entry.hasValue() ? this.entry.value() : null;
    return entry
      ? {
          description: entry.description,
          categoryId: entry.categoryId,
          amount: entry.amount,
          dueDate: entry.dueDate,
          installments: 1,
          editing: true,
        }
      : {
          description: '',
          categoryId: null,
          amount: null,
          dueDate: todayIso(),
          installments: 1,
          editing: false,
        };
  });
  /** Só lançamento aberto é editado, e só por quem grava. */
  protected readonly editable = computed(() => {
    if (!this.canWrite()) {
      return false;
    }
    const entry = this.entry.hasValue() ? this.entry.value() : null;
    return !this.isEdit() || entry?.status === 'Open';
  });
  protected readonly entryForm = form(this.model, (path) => {
    apply(path, entrySchema);
    disabled(path, () => !this.editable());
  });

  protected readonly conflict = signal(false);
  protected readonly loading = computed(
    () => this.isEdit() && this.entry.isLoading() && !this.entry.hasValue(),
  );
  protected readonly showSkeleton = delayedFlag(this.loading);
  protected readonly loadError = computed(
    () => this.isEdit() && (this.entryId() === null || this.entry.status() === 'error'),
  );
  protected readonly formErrors = computed(() => this.entryForm().errors());
  protected readonly preview = computed(() => {
    const { amount, installments, dueDate, editing } = this.model();
    return editing || installments < 2
      ? []
      : previewInstallments(amount ?? 0, installments, dueDate);
  });
  protected readonly loaded = computed<FinancialEntryDto | null>(() =>
    this.entry.hasValue() ? this.entry.value() : null,
  );

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    this.conflict.set(false);
    await submit(this.entryForm, async () => {
      const value = this.model();
      const partnerId = this.partner()?.id ?? null;
      try {
        const current = this.loaded();
        if (current) {
          const updated = await this.api.invoke(updateFinancialEntry, {
            id: current.id,
            body: {
              description: value.description.trim(),
              categoryId: value.categoryId ?? 0,
              partnerId,
              amount: value.amount ?? 0,
              dueDate: value.dueDate,
              version: current.version,
            },
          });
          this.entry.set(updated);
          this.notifier.success(FINANCE_STRINGS.form.updated);
        } else {
          const created = await this.api.invoke(createFinancialEntry, {
            body: {
              kind: this.kind(),
              description: value.description.trim(),
              categoryId: value.categoryId ?? 0,
              partnerId,
              amount: value.amount ?? 0,
              dueDate: value.dueDate,
              installments: value.installments,
            },
          });
          this.notifier.success(FINANCE_STRINGS.form.created(created.length));
          // Uma parcela: abre a edição dela. Várias: volta para a lista, onde aparecem todas.
          await this.router.navigate(
            created.length === 1 ? ['..', 'edit', created[0].id] : ['..'],
            {
              relativeTo: this.route,
              replaceUrl: true,
            },
          );
        }
        return undefined;
      } catch (error) {
        const problem = toApiProblem(error);
        this.conflict.set(problem.code === API_ERROR_CODES.concurrencyConflict);
        return toServerErrors(this.entryForm, problem);
      }
    });
  }

  protected reload(): void {
    this.conflict.set(false);
    this.entry.reload();
  }
}
