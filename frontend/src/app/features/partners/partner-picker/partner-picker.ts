import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
  linkedSignal,
  model,
  resource,
} from '@angular/core';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Api } from '../../../api/api';
import { listPartners } from '../../../api/fn/partners/list-partners';
import { PartnerType } from '../../../api/models/partner-type';
import { debounced } from '../../../shared/data/debounced';
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
  imports: [
    MatAutocompleteModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
  ],
  template: `
    <mat-form-field [subscriptSizing]="subscriptSizing()">
      <mat-label>{{ label() }}</mat-label>
      <mat-icon matPrefix>person_search</mat-icon>
      <input
        matInput
        autocomplete="off"
        [placeholder]="strings.placeholder"
        [matAutocomplete]="auto"
        [value]="text()"
        [disabled]="disabled()"
        (input)="onInput($event)"
      />
      @if (selected() && !disabled()) {
        <button
          matIconButton
          matSuffix
          type="button"
          [attr.aria-label]="strings.clear"
          (click)="clear()"
        >
          <mat-icon>close</mat-icon>
        </button>
      }
      <mat-autocomplete
        #auto="matAutocomplete"
        [displayWith]="display"
        (optionSelected)="pick($event.option.value)"
      >
        @for (partner of options(); track partner.id) {
          <mat-option [value]="partner">
            {{ partner.name }} · <span class="document">{{ format(partner.document) }}</span>
          </mat-option>
        } @empty {
          @if (term()) {
            <mat-option disabled>{{ strings.noResults }}</mat-option>
          }
        }
      </mat-autocomplete>
      @if (error()) {
        <mat-hint class="error">{{ error() }}</mat-hint>
      }
    </mat-form-field>
  `,
  styles: `
    :host {
      display: block;
    }

    mat-form-field {
      width: 100%;
    }

    .document {
      color: var(--erp-text-subtle);
      font-size: var(--erp-font-size-sm);
    }

    .error {
      color: var(--erp-danger);
    }
  `,
})
export class PartnerPicker {
  private readonly api = inject(Api);

  readonly selected = model<PickedPartner | null>(null);
  readonly type = input<PartnerType | null>(null);
  readonly label = input<string>(PARTNERS_STRINGS.picker.label);
  readonly disabled = input(false);
  readonly error = input<string | null>(null);
  readonly subscriptSizing = input<'fixed' | 'dynamic'>('fixed');

  protected readonly strings = PARTNERS_STRINGS.picker;
  protected readonly format = formatDocument;
  protected readonly text = linkedSignal(() => this.display(this.selected()));
  private readonly typed = linkedSignal<PickedPartner | null, string>({
    source: this.selected,
    computation: () => '',
  });
  protected readonly term = debounced(computed(() => this.typed().trim()));

  private readonly results = resource({
    params: () =>
      this.term() ? { search: this.term(), type: this.type() ?? undefined } : undefined,
    loader: ({ params }) =>
      this.api.invoke(listPartners, { ...params, active: true, pageSize: 10 }),
  });
  protected readonly options = computed(() =>
    this.results.hasValue() && this.term() ? this.results.value().items : [],
  );

  protected readonly display = (partner: PickedPartner | string | null): string =>
    typeof partner === 'string' ? partner : (partner?.name ?? '');

  protected onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.text.set(value);
    this.typed.set(value);
    if (this.selected()) {
      this.selected.set(null);
    }
  }

  protected pick(partner: PickedPartner): void {
    this.selected.set({ id: partner.id, name: partner.name, document: partner.document });
  }

  protected clear(): void {
    this.selected.set(null);
    this.text.set('');
    this.typed.set('');
  }
}
