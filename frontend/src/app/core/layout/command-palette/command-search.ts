import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { Icon } from '../../../shared/ui/icon/icon';
import { LAYOUT_STRINGS } from '../layout.strings';
import { CommandCatalog, normalizeSearch, type PaletteCommand } from './command-catalog';

const STRINGS = LAYOUT_STRINGS.palette;
const MAX_RESULTS = 8;

/**
 * "Buscar ou ir para...": campo na barra superior que abre uma lista logo abaixo, com telas,
 * criações e ações. Ctrl K (ou ⌘ K) foca o campo; setas escolhem; Enter executa; Esc fecha.
 */
@Component({
  selector: 'erp-command-search',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  host: {
    class: 'relative block',
    '(document:keydown)': 'onGlobalKey($event)',
    '(focusout)': 'onFocusOut($event)',
  },
  template: `
    <div class="relative">
      <erp-icon
        name="search"
        class="pointer-events-none absolute top-1/2 left-3 -translate-y-1/2 text-muted-foreground"
      />
      <input
        #input
        type="text"
        role="combobox"
        autocomplete="off"
        aria-autocomplete="list"
        aria-controls="command-search-results"
        class="h-button w-full rounded-md border border-input bg-card pr-16 pl-9 text-sm text-foreground outline-none placeholder:text-muted-foreground focus:border-ring focus:shadow-focus"
        [attr.aria-expanded]="open()"
        [attr.aria-activedescendant]="
          open() && results().length ? 'command-search-' + active() : null
        "
        [attr.aria-label]="strings.open"
        [placeholder]="strings.placeholder"
        [value]="query()"
        (input)="onInput($event)"
        (focus)="open.set(true)"
        (keydown)="onKey($event)"
      />
      <span
        class="erp-kbd pointer-events-none absolute top-1/2 right-2 -translate-y-1/2"
        aria-hidden="true"
        >Ctrl K</span
      >
    </div>
    @if (open()) {
      <ul
        id="command-search-results"
        role="listbox"
        class="absolute top-full right-0 left-0 z-50 m-0 mt-1 list-none rounded-md border border-border bg-popover p-1 shadow-overlay"
      >
        @for (command of results(); track command.label; let index = $index) {
          <li
            role="option"
            class="flex cursor-pointer items-center gap-3 rounded-sm px-3 py-2 text-sm"
            [id]="'command-search-' + index"
            [class.bg-accent]="index === active()"
            [attr.aria-selected]="index === active()"
            (mousedown)="$event.preventDefault()"
            (click)="run(command)"
            (mouseenter)="active.set(index)"
          >
            <erp-icon [name]="command.icon" class="text-muted-foreground" />
            <span class="flex-1 truncate">{{ command.label }}</span>
            <span class="text-caption text-muted-foreground">{{ command.hint }}</span>
          </li>
        } @empty {
          <li class="px-3 py-4 text-center text-sm text-muted-foreground" role="presentation">
            {{ strings.empty }}
          </li>
        }
      </ul>
    }
  `,
})
export class CommandSearch {
  private readonly catalog = inject(CommandCatalog);
  private readonly input = viewChild.required<ElementRef<HTMLInputElement>>('input');

  protected readonly strings = STRINGS;
  protected readonly query = signal('');
  protected readonly open = signal(false);
  protected readonly active = signal(0);

  protected readonly results = computed(() => {
    const term = normalizeSearch(this.query().trim());
    const commands = this.catalog.commands();
    return (
      term ? commands.filter((command) => normalizeSearch(command.label).includes(term)) : commands
    ).slice(0, MAX_RESULTS);
  });

  protected onGlobalKey(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault();
      this.input().nativeElement.focus();
      this.input().nativeElement.select();
    }
  }

  protected onInput(event: Event): void {
    this.query.set((event.target as HTMLInputElement).value);
    this.active.set(0);
    this.open.set(true);
  }

  protected onKey(event: KeyboardEvent): void {
    const count = this.results().length;
    if (event.key === 'ArrowDown' && count) {
      event.preventDefault();
      this.active.set((this.active() + 1) % count);
    } else if (event.key === 'ArrowUp' && count) {
      event.preventDefault();
      this.active.set((this.active() - 1 + count) % count);
    } else if (event.key === 'Enter' && count) {
      event.preventDefault();
      this.run(this.results()[this.active()]);
    } else if (event.key === 'Escape') {
      this.close();
      this.input().nativeElement.blur();
    }
  }

  protected onFocusOut(event: FocusEvent): void {
    const next = event.relatedTarget as Node | null;
    if (!next || !(event.currentTarget as HTMLElement).contains(next)) {
      this.close();
    }
  }

  protected run(command: PaletteCommand): void {
    this.close();
    this.input().nativeElement.blur();
    command.run();
  }

  private close(): void {
    this.open.set(false);
    this.query.set('');
    this.active.set(0);
  }
}
