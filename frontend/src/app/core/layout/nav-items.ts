export interface NavItem {
  readonly label: string;
  readonly icon: string;
  readonly path: string;
  /** Módulos ainda não implementados aparecem desabilitados no menu. */
  readonly available: boolean;
}

export const NAV_ITEMS: readonly NavItem[] = [
  { label: 'Início', icon: 'home', path: '/', available: true },
  { label: 'Produtos', icon: 'inventory_2', path: '/products', available: false },
  { label: 'Parceiros', icon: 'groups', path: '/partners', available: false },
  { label: 'Vendas', icon: 'point_of_sale', path: '/sales', available: false },
  { label: 'Estoque', icon: 'warehouse', path: '/stock', available: false },
  { label: 'Financeiro', icon: 'account_balance_wallet', path: '/finance', available: false },
  { label: 'Configurações', icon: 'settings', path: '/settings', available: false },
];
