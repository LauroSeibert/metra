import { FieldTree, ValidationError } from '@angular/forms/signals';
import { ApiProblem } from './api-problem';

/**
 * Converte um {@link ApiProblem} em erros do Signal Forms, para o `submit` marcar os campos que o
 * servidor rejeitou. O servidor é a autoridade: a validação do front só antecipa as mesmas regras.
 *
 * @param fieldsByCode erros de conflito (409) que pertencem a um campo, ex.: SKU duplicado → sku.
 */
export function toServerErrors<T>(
  form: FieldTree<T>,
  problem: ApiProblem,
  fieldsByCode: Readonly<Record<string, string>> = {},
): ValidationError.WithOptionalFieldTree[] {
  const fields = form as unknown as Record<string, FieldTree<unknown> | undefined>;
  const errors: ValidationError.WithOptionalFieldTree[] = [];

  for (const [field, messages] of Object.entries(problem.fieldErrors)) {
    const fieldTree = fields[field];
    for (const message of messages) {
      errors.push(fieldTree ? { kind: 'server', message, fieldTree } : { kind: 'server', message });
    }
  }

  const conflictField = problem.code ? fieldsByCode[problem.code] : undefined;
  const conflictTree = conflictField ? fields[conflictField] : undefined;
  if (conflictTree) {
    errors.push({ kind: 'server', message: problem.message, fieldTree: conflictTree });
  } else if (errors.length === 0) {
    errors.push({ kind: 'server', message: problem.message });
  }

  return errors;
}
