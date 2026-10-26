import { ChangeDetectionStrategy, Component, effect, input, signal } from '@angular/core';
import { Icon } from '../icon/icon';
import { UI_STRINGS } from '../ui.strings';

/**
 * Barra de ações do formulário: estado do registro ("Alterações não salvas" ou "Tudo salvo") e,
 * à direita, as ações projetadas (Salvar, Cancelar).
 */
@Component({
  selector: 'erp-action-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Icon],
  host: { class: 'flex flex-wrap items-center gap-3' },
  template: `
    @if (dirty()) {
      <span class="inline-flex items-center gap-1.5 text-label text-warning-text" role="status">
        <span class="size-1.5 rounded-full bg-warning" aria-hidden="true"></span>
        {{ strings.unsavedChanges }}
      </span>
    } @else if (showSaved() && edited()) {
      <span class="inline-flex items-center gap-1.5 text-label text-success" role="status">
        <erp-icon name="circle-check" [size]="16" />
        {{ strings.allSaved }}
      </span>
    }
    <ng-content />
  `,
})
export class ActionBar {
  readonly dirty = input(false);
  /** Permite "Tudo salvo" (some no cadastro novo, que ainda não foi salvo). */
  readonly showSaved = input(true);

  /** "Tudo salvo" só aparece depois de uma edição, nunca na abertura da tela. */
  protected readonly edited = signal(false);
  private readonly watchEdits = effect(() => {
    if (this.dirty()) {
      this.edited.set(true);
    }
  });

  protected readonly strings = UI_STRINGS;
}
