import { EnvironmentProviders, makeEnvironmentProviders } from '@angular/core';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';
import { MatPaginatorIntl } from '@angular/material/paginator';
import { PtBrPaginatorIntl } from '../../core/i18n/paginator-intl';

/**
 * Padrões do Material para telas de listagem e cadastro (campos outline, paginador em pt-BR).
 * Use em `providers` da rota raiz de cada funcionalidade, nunca no app.config: importar esses
 * tokens no bundle inicial arrasta os módulos inteiros (form-field, select, paginator), cerca de
 * 250 kB, e estoura o orçamento de bundle.
 */
export function provideMaterialFormDefaults(): EnvironmentProviders {
  return makeEnvironmentProviders([
    { provide: MAT_FORM_FIELD_DEFAULT_OPTIONS, useValue: { appearance: 'outline' } },
    { provide: MatPaginatorIntl, useClass: PtBrPaginatorIntl },
  ]);
}
