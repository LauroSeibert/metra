import { Injectable, inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';

/** Mensagens curtas de retorno (salvo, excluído, erro). */
@Injectable({ providedIn: 'root' })
export class Notifier {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, undefined, { duration: 3000, panelClass: 'erp-snack-success' });
  }

  error(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 6000, panelClass: 'erp-snack-error' });
  }
}
