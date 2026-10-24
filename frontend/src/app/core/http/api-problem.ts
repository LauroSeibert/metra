import { HttpErrorResponse } from '@angular/common/http';

/** Erro da API já normalizado a partir do ProblemDetails (RFC 9457). */
export interface ApiProblem {
  readonly status: number;
  /** Código estável enviado pela API (ex.: `product.sku_duplicate`). */
  readonly code: string | null;
  readonly message: string;
  /** Erros por campo, com as chaves em camelCase iguais às do contrato. */
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>;
}

export const API_ERROR_CODES = {
  validationFailed: 'validation_failed',
  concurrencyConflict: 'concurrency_conflict',
} as const;

const FALLBACK_MESSAGE = 'Não foi possível concluir a operação. Tente de novo.';
const OFFLINE_MESSAGE = 'Sem conexão com o servidor.';

interface ProblemBody {
  readonly code?: unknown;
  readonly title?: unknown;
  readonly detail?: unknown;
  readonly errors?: unknown;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function readFieldErrors(errors: unknown): Record<string, string[]> {
  if (!isRecord(errors)) {
    return {};
  }
  const result: Record<string, string[]> = {};
  for (const [field, messages] of Object.entries(errors)) {
    if (Array.isArray(messages)) {
      result[field] = messages.filter((message): message is string => typeof message === 'string');
    }
  }
  return result;
}

/** Converte qualquer erro de chamada HTTP em {@link ApiProblem}. */
export function toApiProblem(error: unknown): ApiProblem {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0, code: null, message: FALLBACK_MESSAGE, fieldErrors: {} };
  }
  if (error.status === 0) {
    return { status: 0, code: null, message: OFFLINE_MESSAGE, fieldErrors: {} };
  }

  const body: ProblemBody = isRecord(error.error) ? error.error : {};
  const detail = typeof body.detail === 'string' ? body.detail : null;
  const title = typeof body.title === 'string' ? body.title : null;
  return {
    status: error.status,
    code: typeof body.code === 'string' ? body.code : null,
    message: detail ?? title ?? FALLBACK_MESSAGE,
    fieldErrors: readFieldErrors(body.errors),
  };
}
