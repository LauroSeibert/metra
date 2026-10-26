import { FinancialEntryDto } from '../../api/models/financial-entry-dto';
import { StatusTone } from '../../shared/ui/status-chip/status-chip';
import { FINANCE_STRINGS } from './finance.strings';

/** Texto da situação: "Vencido há N dias", "Recebido/Pago", "Aberto" ou "Cancelado". */
export function entryStatusLabel(entry: FinancialEntryDto, paidChip: string): string {
  if (entry.overdue) {
    return FINANCE_STRINGS.status.overdue(entry.daysOverdue);
  }
  return entry.status === 'Paid' ? paidChip : FINANCE_STRINGS.status[entry.status];
}

export function entryStatusTone(entry: FinancialEntryDto): StatusTone {
  if (entry.overdue) {
    return 'danger';
  }
  return entry.status === 'Paid' ? 'success' : entry.status === 'Open' ? 'primary' : 'neutral';
}
