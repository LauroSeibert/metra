import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet],
  // TEMP (fase 1): elemento de teste da ponte de tokens; remover na fase 9.
  template:
    '<router-outlet /><div class="bg-primary text-primary-foreground fixed bottom-2 left-2 z-50 rounded-md px-2 py-1 text-xs">spartan bridge</div>',
})
export class App {}
