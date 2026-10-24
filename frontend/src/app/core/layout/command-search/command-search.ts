import {
  ChangeDetectionStrategy,
  Component,
  computed,
  ElementRef,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { Router } from '@angular/router';
import { AuthService } from '../../auth/auth.service';
import { LAYOUT_STRINGS } from '../layout.strings';
import { NAV_ITEMS } from '../nav-items';

interface Command {
  readonly label: string;
  readonly hint: string;
  readonly icon: string;
  readonly path: string;
  /** Só aparece para quem grava (criar registro). */
  readonly write?: boolean;
}

const STRINGS = LAYOUT_STRINGS.search;

/** Atalhos de cadastro, além das telas do menu. */
const CREATE: readonly Command[] = [
  { label: 'Nova venda', hint: STRINGS.create, icon: 'add', path: '/sales/edit', write: true },
  { label: 'Novo produto', hint: STRINGS.create, icon: 'add', path: '/products/edit', write: true },
  {
    label: 'Novo parceiro',
    hint: STRINGS.create,
    icon: 'add',
    path: '/partners/edit',
    write: true,
  },
  {
    label: 'Nova conta a receber',
    hint: STRINGS.create,
    icon: 'add',
    path: '/finance/receivables/edit',
    write: true,
  },
  {
    label: 'Nova conta a pagar',
    hint: STRINGS.create,
    icon: 'add',
    path: '/finance/payables/edit',
    write: true,
  },
];

const PAGES: readonly Command[] = [
  ...NAV_ITEMS.filter((item) => item.available).map((item) => ({
    label: item.label,
    hint: STRINGS.goTo,
    icon: item.icon,
    path: item.path,
  })),
  {
    label: 'Contas a receber',
    hint: STRINGS.goTo,
    icon: 'call_received',
    path: '/finance/receivables',
  },
  { label: 'Contas a pagar', hint: STRINGS.goTo, icon: 'call_made', path: '/finance/payables' },
  { label: 'Histórico de estoque', hint: STRINGS.goTo, icon: 'history', path: '/stock/movements' },
  { label: LAYOUT_STRINGS.myProfile, hint: STRINGS.goTo, icon: 'person', path: '/profile' },
];

/** Sem acento e em minúsculas, para "estoque" achar "Histórico de estoque" e "cadastro" achar "Cadastros". */
function normalize(text: string): string {
  return text.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase();
}

/**
 * "Buscar ou ir para...": encontra uma tela ou um cadastro pelo nome e navega. Ctrl K (ou ⌘ K)
 * foca o campo; setas escolhem; Enter abre; Esc fecha.
 */
@Component({
  selector: 'erp-command-search',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule],
  host: {
    '(document:keydown)': 'onGlobalKey($event)',
    '(focusout)': 'onFocusOut($event)',
  },
  template: `
    <div class="field" [class.open]="open()">
      <mat-icon aria-hidden="true">search</mat-icon>
      <input
        #input
        type="text"
        role="combobox"
        autocomplete="off"
        aria-autocomplete="list"
        aria-controls="command-results"
        [attr.aria-expanded]="open()"
        [attr.aria-activedescendant]="open() && results().length ? 'command-' + active() : null"
        [attr.aria-label]="strings.label"
        [placeholder]="strings.placeholder"
        [value]="query()"
        (input)="onInput($event)"
        (focus)="open.set(true)"
        (keydown)="onKey($event)"
      />
      <span class="erp-kbd shortcut" aria-hidden="true">Ctrl K</span>
    </div>
    @if (open()) {
      <ul id="command-results" class="results" role="listbox">
        @for (command of results(); track command.path; let index = $index) {
          <li
            role="option"
            [id]="'command-' + index"
            [class.active]="index === active()"
            [attr.aria-selected]="index === active()"
            (mousedown)="$event.preventDefault()"
            (click)="go(command)"
            (mouseenter)="active.set(index)"
          >
            <mat-icon aria-hidden="true">{{ command.icon }}</mat-icon>
            <span class="label">{{ command.label }}</span>
            <span class="hint">{{ command.hint }}</span>
          </li>
        } @empty {
          <li class="empty" role="presentation">{{ strings.empty }}</li>
        }
      </ul>
    }
  `,
  styleUrl: './command-search.scss',
})
export class CommandSearch {
  private readonly router = inject(Router);
  private readonly auth = inject(AuthService);
  private readonly input = viewChild.required<ElementRef<HTMLInputElement>>('input');

  protected readonly strings = STRINGS;
  protected readonly query = signal('');
  protected readonly open = signal(false);
  protected readonly active = signal(0);

  protected readonly results = computed(() => {
    const term = normalize(this.query().trim());
    const commands = [...PAGES, ...CREATE.filter((command) => this.auth.canWrite())];
    return (
      term ? commands.filter((command) => normalize(command.label).includes(term)) : commands
    ).slice(0, 8);
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
      this.go(this.results()[this.active()]);
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

  protected go(command: Command): void {
    this.close();
    this.input().nativeElement.blur();
    void this.router.navigateByUrl(command.path);
  }

  private close(): void {
    this.open.set(false);
    this.query.set('');
    this.active.set(0);
  }
}
