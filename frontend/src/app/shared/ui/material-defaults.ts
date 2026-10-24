import { EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';

/**
 * Padrões do Material para telas de listagem e cadastro (campos outline).
 * Use em `providers` da rota raiz de cada funcionalidade, nunca no app.config: importar esses
 * tokens no bundle inicial arrasta os módulos inteiros (form-field, select), cerca de
 * 250 kB, e estoura o orçamento de bundle.
 */
export function provideMaterialFormDefaults(): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: { appearance: 'outline' } },
  ]);
}
