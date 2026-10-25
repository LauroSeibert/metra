/** Textos da tela inicial (dashboard). */
export const HOME_STRINGS = {
  title: 'Dashboard',
  subtitle: 'Como está o negócio hoje.',
  newSale: 'Nova venda',
  kpis: 'Indicadores',
  salesMonth: 'Vendas no mês',
  salesMonthHint: (count: number, change: string | null) =>
    `${count.toLocaleString('pt-BR')} vendas` + (change ? ` · ${change} vs. mês anterior` : ''),
  receivable: 'A receber em aberto',
  payable: 'A pagar em aberto',
  overdueHint: (amount: string) => `${amount} vencido`,
  lowStock: 'Estoque baixo',
  lowStockHint: 'produtos abaixo do mínimo',
  drafts: 'Rascunhos de venda',
  draftsHint: 'aguardando confirmação',
  dailyTitle: 'Vendas nos últimos 30 dias',
  dailyHint: 'Vendas confirmadas por dia.',
  dailyBar: (date: string, total: string, count: number) =>
    `${date}: ${total} em ${count} ${count === 1 ? 'venda' : 'vendas'}`,
  topTitle: 'Mais vendidos',
  topHint: 'Por faturamento, últimos 30 dias.',
  recentTitle: 'Últimas vendas',
  seeAll: 'Ver todas',
  empty: 'Nenhuma venda no período.',
  loadError: 'Não foi possível carregar os indicadores.',
  retry: 'Tentar de novo',
  status: { Draft: 'Rascunho', Confirmed: 'Confirmada', Cancelled: 'Cancelada' },
} as const;
