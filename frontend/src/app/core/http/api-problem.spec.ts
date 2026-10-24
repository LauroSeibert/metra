import { HttpErrorResponse } from '@angular/common/http';
import { toApiProblem } from './api-problem';

describe('toApiProblem', () => {
  it('reads code, detail and field errors from a validation ProblemDetails', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: {
        title: 'Dados inválidos.',
        detail: 'Um ou mais campos são inválidos.',
        code: 'validation_failed',
        errors: { sku: ['Campo obrigatório.'], salePrice: ['O valor não pode ser negativo.', 7] },
      },
    });

    expect(toApiProblem(error)).toEqual({
      status: 400,
      code: 'validation_failed',
      message: 'Um ou mais campos são inválidos.',
      fieldErrors: { sku: ['Campo obrigatório.'], salePrice: ['O valor não pode ser negativo.'] },
    });
  });

  it('falls back to the title when there is no detail', () => {
    const error = new HttpErrorResponse({ status: 404, error: { title: 'Não encontrado.' } });

    expect(toApiProblem(error).message).toBe('Não encontrado.');
  });

  it('reports a connection problem for status 0', () => {
    const problem = toApiProblem(new HttpErrorResponse({ status: 0 }));

    expect(problem.status).toBe(0);
    expect(problem.message).toBe('Sem conexão com o servidor.');
  });

  it('handles errors that are not HTTP responses', () => {
    expect(toApiProblem(new Error('boom')).code).toBeNull();
  });
});
