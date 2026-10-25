import { ChangeDetectionStrategy, Component, inject, Injectable } from '@angular/core';
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDialogImports, HlmDialogService } from '@spartan-ng/helm/dialog';
import { firstValueFrom } from 'rxjs';

export interface ConfirmDialogData {
  readonly title: string;
  readonly message: string;
  readonly confirmLabel: string;
  readonly cancelLabel: string;
}

/** Confirmação de ação destrutiva. Fecha com `true` quando confirmada. */
@Component({
  selector: 'erp-confirm-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [HlmButton, HlmDialogImports],
  template: `
    <div class="grid gap-4">
      <hlm-dialog-header>
        <h3 hlmDialogTitle>{{ data.title }}</h3>
        <p hlmDialogDescription>{{ data.message }}</p>
      </hlm-dialog-header>
      <hlm-dialog-footer>
        <button hlmBtn variant="outline" type="button" (click)="ref.close(false)">
          {{ data.cancelLabel }}
        </button>
        <button hlmBtn variant="destructive" type="button" (click)="ref.close(true)">
          {{ data.confirmLabel }}
        </button>
      </hlm-dialog-footer>
    </div>
  `,
})
export class ConfirmDialog {
  protected readonly data = injectBrnDialogContext<ConfirmDialogData>();
  protected readonly ref = inject<BrnDialogRef<boolean>>(BrnDialogRef);
}

/** Abre a confirmação e devolve se a pessoa confirmou (Esc ou Cancelar devolvem `false`). */
@Injectable({ providedIn: 'root' })
export class ConfirmService {
  private readonly dialogs = inject(HlmDialogService);

  async ask(data: ConfirmDialogData): Promise<boolean> {
    const ref = this.dialogs.open<boolean, ConfirmDialogData>(ConfirmDialog, {
      context: data,
      contentClass: 'sm:max-w-md',
    });
    return (await firstValueFrom(ref.closed$)) === true;
  }
}
