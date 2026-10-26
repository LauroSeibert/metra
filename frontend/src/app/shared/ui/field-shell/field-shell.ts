import {
  afterNextRender,
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  inject,
  Injector,
  input,
  signal,
} from '@angular/core';
import { HlmLabel } from '@spartan-ng/helm/label';

let nextId = 0;

const CONTROL_SELECTOR = 'input, select, textarea, [role="combobox"]';

/**
 * Rótulo, controle, dica e erro de um campo. Sem label flutuante e sem espaço reservado: a linha
 * de dica ou de erro só existe quando há texto. Liga `id`, `aria-describedby` e `aria-invalid` ao
 * primeiro controle projetado (input, select, textarea ou combobox).
 */
@Component({
  selector: 'erp-field-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmLabel],
  host: { class: 'flex min-w-0 flex-col gap-1.5' },
  template: `
    <label hlmLabel [for]="controlId()">
      {{ label() }}
      @if (required()) {
        <span class="text-destructive" aria-hidden="true">*</span>
      }
    </label>
    <ng-content />
    @if (error(); as message) {
      <p [id]="errorId" class="m-0 text-caption text-destructive" role="alert">{{ message }}</p>
    } @else if (hint(); as text) {
      <p [id]="hintId" class="m-0 text-caption text-muted-foreground">{{ text }}</p>
    }
  `,
})
export class FieldShell {
  readonly label = input.required<string>();
  readonly hint = input<string | null>(null);
  readonly error = input<string | null>(null);
  readonly required = input(false);

  private readonly id = `erp-field-${nextId++}`;
  protected readonly controlId = signal(`${this.id}-control`);
  protected readonly hintId = `${this.id}-hint`;
  protected readonly errorId = `${this.id}-error`;
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly injector = inject(Injector);

  constructor() {
    afterNextRender(() => {
      const control = this.control();
      if (control?.id) {
        this.controlId.set(control.id);
      } else if (control) {
        control.id = this.controlId();
      }
      // Reaplica a cada mudança de dica ou erro.
      effect(
        () => {
          const target = this.control();
          if (!target) {
            return;
          }
          const text = this.error() ? this.errorId : this.hint() ? this.hintId : null;
          if (text) {
            target.setAttribute('aria-describedby', text);
          } else {
            target.removeAttribute('aria-describedby');
          }
          if (this.error()) {
            target.setAttribute('aria-invalid', 'true');
          } else {
            target.removeAttribute('aria-invalid');
          }
        },
        { injector: this.injector },
      );
    });
  }

  private control(): HTMLElement | null {
    return this.host.nativeElement.querySelector<HTMLElement>(CONTROL_SELECTOR);
  }
}
