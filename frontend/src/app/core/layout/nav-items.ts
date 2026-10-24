export interface NavItem {
  readonly label: string;
  readonly icon: string;
  readonly path: string;
  /** Módulos ainda não implementados aparecem desabilitados no menu. */
  readonly available: boolean;
}

export interface NavGroup {
  readonly label: string;
  readonly items: readonly NavItem[];
}

export const NAV_GROUPS: readonly NavGroup[] = [
  {
    label: 'Visão geral',
    items: [{ label: 'Início', icon: 'space_dashboard', path: '/', available: true }],
  },
  {
    label: 'Cadastros',
    items: [
      { label: 'Produtos', icon: 'inventory_2', path: '/products', available: true },
      { label: 'Parceiros', icon: 'groups', path: '/partners', available: true },
    ],
  },
  {
    label: 'Operação',
    items: [
      { label: 'Vendas', icon: 'point_of_sale', path: '/sales', available: false },
      { label: 'Estoque', icon: 'warehouse', path: '/stock', available: true },
      { label: 'Financeiro', icon: 'account_balance_wallet', path: '/finance', available: false },
    ],
  },
  {
    label: 'Sistema',
    items: [{ label: 'Configurações', icon: 'settings', path: '/settings', available: false }],
  },
];

export const NAV_ITEMS: readonly NavItem[] = NAV_GROUPS.flatMap((group) => group.items);
