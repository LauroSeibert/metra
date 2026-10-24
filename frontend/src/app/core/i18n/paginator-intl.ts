import { Injectable } from '@angular/core';
import { MatPaginatorIntl } from '@angular/material/paginator';

/** Textos do paginador do Material em pt-BR. */
@Injectable()
export class PtBrPaginatorIntl extends MatPaginatorIntl {
  override itemsPerPageLabel = 'Itens por página';
  override nextPageLabel = 'Próxima página';
  override previousPageLabel = 'Página anterior';
  override firstPageLabel = 'Primeira página';
  override lastPageLabel = 'Última página';

  override getRangeLabel = (page: number, pageSize: number, length: number): string => {
    if (length === 0) {
      return '0 de 0';
    }
    const start = page * pageSize + 1;
    const end = Math.min((page + 1) * pageSize, length);
    const format = (value: number) => value.toLocaleString('pt-BR');
    return `${format(start)} – ${format(end)} de ${format(length)}`;
  };
}
