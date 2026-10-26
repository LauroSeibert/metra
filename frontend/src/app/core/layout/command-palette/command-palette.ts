import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { HlmCommandImports } from '@spartan-ng/helm/command';
import { Icon } from '../../../shared/ui/icon/icon';
import { LAYOUT_STRINGS } from '../layout.strings';
import { CommandCatalog, normalizeSearch, type PaletteCommand } from './command-catalog';
import { CommandPaletteService } from './command-palette.service';

const STRINGS = LAYOUT_STRINGS.palette;

/**
 * Paleta de comandos em diálogo, usada no celular (onde a busca do topo não cabe). No desktop a
 * busca fica no topo (`erp-command-search`).
 */
@Component({
  selector: 'erp-command-palette',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmCommandImports, Icon],
  template: `
    <hlm-command-dialog
      dialogContentClass="w-full sm:max-w-xl"
      [title]="strings.title"
      [description]="strings.description"
      [state]="palette.open() ? 'open' : 'closed'"
      (stateChange)="onState($event)"
    >
      <hlm-command [filter]="filter">
        <hlm-command-input [placeholder]="strings.placeholder" />
        <hlm-command-list>
          <div *hlmCommandEmptyState hlmCommandEmpty>{{ strings.empty }}</div>
          @for (group of catalog.groups(); track group.label) {
            <hlm-command-group>
              <hlm-command-group-label>{{ group.label }}</hlm-command-group-label>
              @for (command of group.commands; track command.label) {
                <button hlmCommandItem [value]="command.label" (selected)="run(command)">
                  <erp-icon [name]="command.icon" />
                  {{ command.label }}
                </button>
              }
            </hlm-command-group>
          }
        </hlm-command-list>
      </hlm-command>
    </hlm-command-dialog>
  `,
})
export class CommandPalette {
  protected readonly palette = inject(CommandPaletteService);
  protected readonly catalog = inject(CommandCatalog);

  protected readonly strings = STRINGS;
  protected readonly filter = (value: string, search: string): boolean =>
    normalizeSearch(value).includes(normalizeSearch(search.trim()));

  protected onState(state: string): void {
    this.palette.open.set(state === 'open');
  }

  protected run(command: PaletteCommand): void {
    this.palette.open.set(false);
    command.run();
  }
}
