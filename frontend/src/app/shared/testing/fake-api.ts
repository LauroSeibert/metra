import { Provider } from '@angular/core';
import { Api } from '../../api/api';

type ApiFunction = (...args: never[]) => unknown;
type Handler = (params: unknown) => unknown;

/**
 * Api falsa para testes de tela: cada função gerada (listProducts, createProduct...) recebe uma
 * resposta. Guarda as chamadas para os testes conferirem os parâmetros enviados.
 */
export class FakeApi {
  readonly calls: { fn: ApiFunction; params: unknown }[] = [];
  private readonly handlers = new Map<ApiFunction, Handler>();

  on(fn: ApiFunction, handler: Handler): this {
    this.handlers.set(fn, handler);
    return this;
  }

  callsTo(fn: ApiFunction): unknown[] {
    return this.calls.filter((call) => call.fn === fn).map((call) => call.params);
  }

  readonly invoke = async (fn: ApiFunction, params?: unknown): Promise<unknown> => {
    this.calls.push({ fn, params });
    const handler = this.handlers.get(fn);
    if (!handler) {
      throw new Error(`FakeApi sem resposta para ${fn.name}`);
    }
    return handler(params);
  };

  provide(): Provider {
    return { provide: Api, useValue: this };
  }
}

export function page<T>(items: T[]): {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
} {
  return { items, page: 1, pageSize: 20, totalCount: items.length };
}
