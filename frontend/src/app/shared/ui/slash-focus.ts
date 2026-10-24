import { Directive, ElementRef, inject } from '@angular/core';

/**
 * Tecla "/" foca este campo (busca da listagem), como em GitHub e Gmail. Ignorada enquanto o usuário
 * digita em outro campo.
 */
@Directive({
  selector: 'input[erpSlashFocus]',
  host: { '(document:keydown)': 'onKey($event)' },
})
export class SlashFocus {
  private readonly input = inject<ElementRef<HTMLInputElement>>(ElementRef);

  protected onKey(event: KeyboardEvent): void {
    if (event.key !== '/' || event.ctrlKey || event.metaKey || event.altKey) {
      return;
    }
    const target = event.target as HTMLElement | null;
    if (target?.closest('input, textarea, select, [contenteditable="true"]')) {
      return;
    }
    event.preventDefault();
    this.input.nativeElement.focus();
  }
}
