import { Directive, TemplateRef, inject, input } from '@angular/core';

export interface DataTableRowContext<T> {
  readonly $implicit: T;
}

/**
 * Conteúdo personalizado de uma coluna. `erpCellOf` só serve para tipar a variável do template:
 * `<ng-template erpCell="salePrice" [erpCellOf]="rows()" let-product>`.
 */
@Directive({ selector: 'ng-template[erpCell]' })
export class DataTableCell<T> {
  readonly column = input.required<string>({ alias: 'erpCell' });
  readonly of = input<readonly T[]>([], { alias: 'erpCellOf' });
  readonly template = inject<TemplateRef<DataTableRowContext<T>>>(TemplateRef);

  static ngTemplateContextGuard<T>(
    _directive: DataTableCell<T>,
    context: unknown,
  ): context is DataTableRowContext<T> {
    return true;
  }
}

/** Conteúdo da linha expandida: `<ng-template erpExpandedRow [erpExpandedRowOf]="rows()" let-row>`. */
@Directive({ selector: 'ng-template[erpExpandedRow]' })
export class DataTableExpandedRow<T> {
  readonly of = input<readonly T[]>([], { alias: 'erpExpandedRowOf' });
  readonly template = inject<TemplateRef<DataTableRowContext<T>>>(TemplateRef);

  static ngTemplateContextGuard<T>(
    _directive: DataTableExpandedRow<T>,
    context: unknown,
  ): context is DataTableRowContext<T> {
    return true;
  }
}
