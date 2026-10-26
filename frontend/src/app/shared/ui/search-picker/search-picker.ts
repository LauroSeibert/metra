import { NgTemplateOutlet } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  contentChild,
  input,
  model,
  output,
  signal,
  TemplateRef,
} from '@angular/core';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmComboboxImports } from '@spartan-ng/helm/combobox';
import { FieldShell } from '../field-shell/field-shell';
import { Icon } from '../icon/icon';
import { UI_STRINGS } from '../ui.strings';

/**
 * Combobox com busca no servidor: quem usa filtra os `items` ao receber `(search)` e recebe o
 * escolhido em `[(selected)]`. O texto de cada opção vem de `itemToString`; para um visual próprio,
 * projete `<ng-template let-item>`. Com `inline`, o rótulo vai dentro do botão (filtros de listagem).
 */
@Component({
  selector: 'erp-search-picker',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmButton, HlmComboboxImports, FieldShell, Icon, NgTemplateOutlet],
  host: { class: 'block min-w-0' },
  template: `
    @if (inline()) {
      <ng-container [ngTemplateOutlet]="picker" />
    } @else {
      <erp-field-shell [label]="label()" [required]="required()" [error]="error()">
        <ng-container [ngTemplateOutlet]="picker" />
      </erp-field-shell>
    }

    <ng-template #picker>
      <div class="relative">
        <hlm-combobox
          [value]="selected()"
          [itemToString]="toText"
          [isItemEqualToValue]="same"
          [filter]="acceptAll"
          [disabled]="disabled()"
          (valueChange)="selected.set($event ?? null)"
          (searchChange)="onSearch($event)"
        >
          <hlm-combobox-trigger
            class="w-full justify-between"
            [class.pr-16]="selected() && !disabled()"
            [attr.aria-label]="label()"
          >
            <span class="flex min-w-0 items-center gap-2">
              @if (inline()) {
                <span class="text-muted-foreground">{{ label() }}</span>
              }
              <hlm-combobox-value class="truncate font-medium" [placeholder]="placeholder()" />
            </span>
          </hlm-combobox-trigger>
          <hlm-combobox-content *hlmComboboxPortal class="min-w-80">
            <hlm-combobox-input [placeholder]="searchPlaceholder()" [showTrigger]="false" />
            <hlm-combobox-empty>{{ term() ? noResults() : typeToSearch() }}</hlm-combobox-empty>
            <div hlmComboboxList>
              @for (item of items(); track key()(item)) {
                <hlm-combobox-item [value]="item">
                  @if (itemTemplate(); as template) {
                    <ng-container
                      [ngTemplateOutlet]="template"
                      [ngTemplateOutletContext]="{ $implicit: item }"
                    />
                  } @else {
                    {{ toText(item) }}
                  }
                </hlm-combobox-item>
              }
            </div>
          </hlm-combobox-content>
        </hlm-combobox>
        @if (selected() && !disabled()) {
          <button
            hlmBtn
            variant="ghost"
            size="icon-xs"
            type="button"
            class="absolute top-1/2 right-10 -translate-y-1/2"
            [attr.aria-label]="clearLabel()"
            (click)="selected.set(null)"
          >
            <erp-icon name="x" [size]="14" />
          </button>
        }
      </div>
    </ng-template>
  `,
})
export class SearchPicker<T> {
  readonly label = input.required<string>();
  readonly items = input.required<readonly T[]>();
  readonly key = input.required<(item: T) => string | number>();
  readonly itemToString = input.required<(item: T) => string>();
  readonly selected = model<T | null>(null);
  readonly placeholder = input<string>(UI_STRINGS.select);
  readonly searchPlaceholder = input<string>(UI_STRINGS.searchPlaceholder);
  readonly typeToSearch = input<string>(UI_STRINGS.typeToSearch);
  readonly noResults = input<string>(UI_STRINGS.noResults);
  readonly clearLabel = input<string>(UI_STRINGS.clearSelection);
  readonly inline = input(false);
  readonly required = input(false);
  readonly disabled = input(false);
  readonly error = input<string | null>(null);
  /** Termo digitado na busca (a cada tecla); quem usa aplica o debounce. */
  readonly search = output<string>();

  protected readonly itemTemplate = contentChild<TemplateRef<{ $implicit: T }>>(TemplateRef);
  protected readonly term = signal('');
  protected readonly toText = (item: T | null): string =>
    item === null ? '' : this.itemToString()(item);
  protected readonly same = (a: T, b: T | null | undefined): boolean =>
    b != null && this.key()(a) === this.key()(b);
  protected readonly acceptAll = (): boolean => true;

  protected onSearch(term: string): void {
    this.term.set(term.trim());
    this.search.emit(term);
  }
}
