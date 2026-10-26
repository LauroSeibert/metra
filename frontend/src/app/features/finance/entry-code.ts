import { FinancialEntryKind } from '../../api/models/financial-entry-kind';

/** Código único do lançamento, derivado do id: REC-000123 (a receber) ou PAG-000123 (a pagar). */
export function entryCode(entry: {
  readonly kind: FinancialEntryKind;
  readonly id: number;
}): string {
  const prefix = entry.kind === 'Receivable' ? 'REC' : 'PAG';
  return `${prefix}-${String(entry.id).padStart(6, '0')}`;
}
