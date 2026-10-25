import { ChangeDetectionStrategy, Component } from '@angular/core';

/** Contador de resultados da listagem, no canto direito dos filtros. */
@Component({
  selector: 'erp-result-count',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    class:
      'ml-auto inline-flex h-control items-center rounded-md border border-border bg-muted px-3 text-sm whitespace-nowrap text-muted-foreground tabular-nums',
  },
  template: `<ng-content />`,
})
export class ResultCount {}
