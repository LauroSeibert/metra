import { Injectable, signal } from '@angular/core';

/** Estado da paleta de comandos (Ctrl/Cmd K): a barra superior abre, a paleta se fecha sozinha. */
@Injectable({ providedIn: 'root' })
export class CommandPaletteService {
  readonly open = signal(false);

  toggle(): void {
    this.open.update((open) => !open);
  }
}
