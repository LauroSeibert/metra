import { Injectable, inject } from '@angular/core';
import { ToastService } from './toast.service';

/** Mensagens curtas de retorno (salvo, excluído, erro): toast com a mensagem como título. */
@Injectable({ providedIn: 'root' })
export class Notifier {
  private readonly toasts = inject(ToastService);

  success(message: string): void {
    this.toasts.success(message);
  }

  error(message: string): void {
    this.toasts.error(message);
  }
}
