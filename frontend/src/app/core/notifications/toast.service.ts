import { Injectable } from '@angular/core';
import { toast } from '@spartan-ng/brain/sonner';

/** Toast no canto inferior direito (sonner): título curto e, se ajudar, uma linha de detalhe. */
@Injectable({ providedIn: 'root' })
export class ToastService {
  success(title: string, description?: string): void {
    toast.success(title, { description });
  }

  error(title: string, description?: string): void {
    toast.error(title, { description, duration: 6000 });
  }

  info(title: string, description?: string): void {
    toast.info(title, { description });
  }
}
