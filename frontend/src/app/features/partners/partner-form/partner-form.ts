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
  email,
  form,
  FormField,
  maxLength,
  required,
  schema,
  submit,
  validate,
} from '@angular/forms/signals';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { Api } from '../../../api/api';
import { createPartner } from '../../../api/fn/partners/create-partner';
import { getPartner } from '../../../api/fn/partners/get-partner';
import { updatePartner } from '../../../api/fn/partners/update-partner';
import { CreatePartnerRequest } from '../../../api/models/create-partner-request';
import { PartnerDto } from '../../../api/models/partner-dto';
import { PartnerType } from '../../../api/models/partner-type';
import { AuthService } from '../../../core/auth/auth.service';
import { API_ERROR_CODES, toApiProblem } from '../../../core/http/api-problem';
import { toServerErrors } from '../../../core/http/server-errors';
import { Notifier } from '../../../core/notifications/notifier';
import { delayedFlag } from '../../../shared/data/delayed-flag';
import { StatusChip } from '../../../shared/ui/status-chip/status-chip';
import { formatDocument, isValidDocument, PARTNER_RULES } from '../partner-rules';
import { fieldError } from '../../../shared/ui/field-shell/field-error';
import { PARTNERS_STRINGS } from '../partners.strings';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSelectImports } from '@spartan-ng/helm/select';
import { HlmSkeleton } from '@spartan-ng/helm/skeleton';
import { ActionBar } from '../../../shared/ui/action-bar/action-bar';
import { FieldShell } from '../../../shared/ui/field-shell/field-shell';
import { FormBanner } from '../../../shared/ui/form-banner/form-banner';
import { FormHeader } from '../../../shared/ui/form-header/form-header';
import { Icon } from '../../../shared/ui/icon/icon';
import { LoadError } from '../../../shared/ui/load-error/load-error';
import { SectionCard } from '../../../shared/ui/section-card/section-card';

export interface PartnerFormModel {
  type: PartnerType;
  name: string;
  document: string;
  email: string;
  phone: string;
}

export const EMPTY_PARTNER: PartnerFormModel = {
  type: 'Customer',
  name: '',
  document: '',
  email: '',
  phone: '',
};

export function toPartnerFormModel(partner: PartnerDto): PartnerFormModel {
  return {
    type: partner.type,
    name: partner.name,
    document: formatDocument(partner.document),
    email: partner.email ?? '',
    phone: partner.phone ?? '',
  };
}

const messages = PARTNERS_STRINGS.validation;

/** Mesmas regras do servidor (Partner.Apply), para o erro aparecer antes do envio. */
export const partnerSchema = schema<PartnerFormModel>((path) => {
  required(path.name, { message: messages.required });
  maxLength(path.name, PARTNER_RULES.nameMaxLength, {
    message: messages.maxLength(PARTNER_RULES.nameMaxLength),
  });
  required(path.document, { message: messages.required });
  validate(path.document, ({ value }) =>
    value().trim() !== '' && !isValidDocument(value())
      ? { kind: 'document', message: messages.document }
      : undefined,
  );
  email(path.email, { message: messages.email });
  maxLength(path.email, PARTNER_RULES.emailMaxLength, {
    message: messages.maxLength(PARTNER_RULES.emailMaxLength),
  });
  maxLength(path.phone, PARTNER_RULES.phoneMaxLength, {
    message: messages.maxLength(PARTNER_RULES.phoneMaxLength),
  });
});

/** Cadastro e edição de parceiro. Sem `id`, cria; com `id`, edita com versão otimista. */
@Component({
  selector: 'erp-partner-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    HlmButton,
    HlmInput,
    HlmSelectImports,
    HlmSkeleton,
    ActionBar,
    FieldShell,
    FormBanner,
    FormHeader,
    Icon,
    LoadError,
    SectionCard,
    DatePipe,
    FormField,
    StatusChip,
  ],
  templateUrl: './partner-form.html',
})
export class PartnerForm {
  private readonly api = inject(Api);
  private readonly router = inject(Router);
  private readonly notifier = inject(Notifier);

  readonly id = input<string>();

  protected readonly strings = PARTNERS_STRINGS;
  protected readonly types: readonly PartnerType[] = ['Customer', 'Supplier', 'Both'];
  protected readonly canWrite = inject(AuthService).canWrite;
  protected readonly isEdit = computed(() => this.id() !== undefined);
  protected readonly partnerId = computed(() => {
    const id = Number(this.id());
    return Number.isInteger(id) && id > 0 ? id : null;
  });

  protected readonly partner = resource({
    params: () => this.partnerId() ?? undefined,
    loader: ({ params }) => this.api.invoke(getPartner, { id: params }),
  });

  protected readonly model = linkedSignal<PartnerFormModel>(() =>
    this.partner.hasValue() ? toPartnerFormModel(this.partner.value()) : { ...EMPTY_PARTNER },
  );
  // Perfil Leitura vê o cadastro, mas não edita.
  protected readonly partnerForm = form(this.model, (path) => {
    apply(path, partnerSchema);
    disabled(path, () => !this.canWrite());
  });

  protected readonly conflict = signal(false);
  protected readonly loadingPartner = computed(
    () => this.isEdit() && this.partner.isLoading() && !this.partner.hasValue(),
  );
  protected readonly showSkeleton = delayedFlag(this.loadingPartner);
  protected readonly loadError = computed(
    () => this.isEdit() && (this.partnerId() === null || this.partner.status() === 'error'),
  );
  protected readonly formErrors = computed(() => this.partnerForm().errors());
  protected readonly fieldError = fieldError;
  protected readonly typeLabel = (type: PartnerType): string => PARTNERS_STRINGS.types[type];

  /** Muda em relação ao que está salvo (ou ao formulário vazio, no cadastro). */
  protected readonly dirty = computed(() => {
    const saved = this.partner.hasValue()
      ? toPartnerFormModel(this.partner.value())
      : EMPTY_PARTNER;
    return JSON.stringify(this.model()) !== JSON.stringify(saved);
  });

  protected setType(type: PartnerType | null | undefined): void {
    if (type) {
      this.partnerForm.type().value.set(type);
    }
  }

  /** Mostra o documento formatado ao sair do campo. */
  protected formatDocumentField(): void {
    this.model.update((value) => ({ ...value, document: formatDocument(value.document) }));
  }

  protected async save(event: Event): Promise<void> {
    event.preventDefault();
    this.conflict.set(false);

    await submit(this.partnerForm, async () => {
      const request = this.toRequest();
      try {
        // Como em produtos: depois de salvar, continua na tela com os dados gravados.
        const current = this.partner.hasValue() ? this.partner.value() : null;
        if (current) {
          const updated = await this.api.invoke(updatePartner, {
            id: current.id,
            body: { ...request, version: current.version },
          });
          this.partner.set(updated);
          this.notifier.success(PARTNERS_STRINGS.form.updated);
        } else {
          const created = await this.api.invoke(createPartner, { body: request });
          this.notifier.success(PARTNERS_STRINGS.form.created);
          await this.router.navigate(['/partners', 'edit', created.id], { replaceUrl: true });
        }
        return undefined;
      } catch (error) {
        const problem = toApiProblem(error);
        this.conflict.set(problem.code === API_ERROR_CODES.concurrencyConflict);
        return toServerErrors(this.partnerForm, problem, {
          'partner.document_duplicate': 'document',
        });
      }
    });
  }

  protected reload(): void {
    this.conflict.set(false);
    this.partner.reload();
  }

  private toRequest(): CreatePartnerRequest {
    const value = this.model();
    return {
      type: value.type,
      name: value.name.trim(),
      document: value.document.trim(),
      email: value.email.trim() || null,
      phone: value.phone.trim() || null,
    };
  }
}
