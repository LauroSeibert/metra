import { ParamMap } from '@angular/router';
import { DataTableSort } from '../ui/data-table/data-table';

/**
 * Estado de listagem guardado na URL, para que voltar, recarregar ou compartilhar o link
 * mantenha busca, filtros, ordenação e página.
 */
export type UrlParamValue = string | number | boolean | null | undefined;

export function readText(params: ParamMap, key: string): string {
  return params.get(key) ?? '';
}

export function readInt(params: ParamMap, key: string, fallback: number): number {
  const value = Number(params.get(key));
  return Number.isInteger(value) && value > 0 ? value : fallback;
}

export function readOptionalInt(params: ParamMap, key: string): number | null {
  const value = Number(params.get(key));
  return Number.isInteger(value) && value > 0 ? value : null;
}

/** Lê `sort=campo` ou `sort=-campo`, aceitando só campos conhecidos. */
export function readSort(
  params: ParamMap,
  allowed: readonly string[],
  fallback: DataTableSort,
): DataTableSort {
  const raw = params.get('sort');
  if (!raw) {
    return fallback;
  }
  const descending = raw.startsWith('-');
  const field = descending ? raw.slice(1) : raw;
  return allowed.includes(field) ? { field, direction: descending ? 'desc' : 'asc' } : fallback;
}

/** Converte a ordenação da tabela para o parâmetro `sort` da API. */
export function toSortParam(sort: DataTableSort): string {
  return sort.direction === 'desc' ? `-${sort.field}` : sort.field;
}

/** Remove vazios para a URL ficar limpa (nada de `?search=&page=1`). */
export function compactParams(
  params: Readonly<Record<string, UrlParamValue>>,
  defaults: Readonly<Record<string, UrlParamValue>> = {},
): Record<string, string | number | boolean> {
  const result: Record<string, string | number | boolean> = {};
  for (const [key, value] of Object.entries(params)) {
    if (value !== null && value !== undefined && value !== '' && value !== defaults[key]) {
      result[key] = value;
    }
  }
  return result;
}
