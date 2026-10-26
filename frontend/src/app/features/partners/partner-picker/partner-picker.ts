import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  model,
  resource,
  signal,
} from '@angular/core';
import { Api } from '../../../api/api';
import { listPartners } from '../../../api/fn/partners/list-partners';
import { PartnerType } from '../../../api/models/partner-type';
import { debounced } from '../../../shared/data/debounced';
import { SearchPicker } from '../../../shared/ui/search-picker/search-picker';
import { formatDocument } from '../partner-rules';
import { PARTNERS_STRINGS } from '../partners.strings';

/** Parceiro escolhido: o mínimo para mostrar e gravar o id. */
export interface PickedPartner {
  readonly id: number;
  readonly name: string;
  readonly document: string;
}

/**
 * Busca de parceiro por prefixo do nome ou do documento, com sugestões. Com <c>type</c>, sugere só
 * clientes ou só fornecedores (quem é os dois aparece nos dois). Só parceiros ativos.
 */
@Component({
  selector: 'erp-partner-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SearchPicker],
  host: { class: 'block min-w-0' },
  template: `
    <erp-search-picker
      [label]="label()"
      [items]="options()"
      [key]="key"
      [itemToString]="display"
      [(selected)]="selected"
      [searchPlaceholder]="strings.placeholder"
      [noResults]="strings.noResults"
      [clearLabel]="strings.clear"
      [inline]="inline()"
      [required]="required()"
      [disabled]="disabled()"
      [error]="error()"
      (search)="typed.set($event)"
    >
      <ng-template let-partner>
        <span class="flex w-full items-baseline justify-between gap-3">
          <span class="truncate">{{ partner.name }}</span>
          <span class="shrink-0 font-mono text-label text-muted-foreground">{{
            format(partner.document)
          }}</span>
        </span>
      </ng-template>
    </erp-search-picker>
  `,
})
export class PartnerPicker {
  private readonly api = inject(Api);

  readonly selected = model<PickedPartner | null>(null);
  readonly type = input<PartnerType | null>(null);
  readonly label = input<string>(PARTNERS_STRINGS.picker.label);
  readonly inline = input(false);
  readonly required = input(false);
  readonly disabled = input(false);
  readonly error = input<string | null>(null);

  protected readonly strings = PARTNERS_STRINGS.picker;
  protected readonly format = formatDocument;
  protected readonly typed = signal('');
  private readonly term = debounced(computed(() => this.typed().trim()));

  private readonly results = resource({
    params: () =>
      this.term() ? { search: this.term(), type: this.type() ?? undefined } : undefined,
    loader: ({ params }) =>
      this.api.invoke(listPartners, { ...params, active: true, pageSize: 10 }),
  });
  protected readonly options = computed<readonly PickedPartner[]>(() =>
    this.results.hasValue() && this.term()
      ? this.results.value().items.map((p) => ({ id: p.id, name: p.name, document: p.document }))
      : [],
  );

  protected readonly key = (partner: PickedPartner): number => partner.id;
  /** Nome e, se houver, o CPF/CNPJ: é o que aparece no campo depois de escolher. */
  protected readonly display = (partner: PickedPartner): string =>
    partner.document ? `${partner.name} · ${formatDocument(partner.document)}` : partner.name;
}
