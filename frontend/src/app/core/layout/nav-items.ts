import type { IconName } from '../../shared/ui/icon/icons';

export interface NavItem {
  readonly label: string;
  readonly icon: IconName;
  readonly path: string;
  /** Módulos ainda não implementados aparecem desabilitados no menu. */
  readonly available: boolean;
}

export interface NavGroup {
  /** Vazio: grupo sem título (Início). O título também abre o breadcrumb ("Cadastros › Produtos"). */
  readonly label: string;
  readonly items: readonly NavItem[];
}

export const NAV_GROUPS: readonly NavGroup[] = [
  {
    label: '',
    items: [{ label: 'Início', icon: 'dashboard', path: '/', available: true }],
  },
  {
    label: 'Cadastros',
    items: [
      { label: 'Produtos', icon: 'package', path: '/products', available: true },
      { label: 'Parceiros', icon: 'users', path: '/partners', available: true },
    ],
  },
  {
    label: 'Operação',
    items: [
      { label: 'Estoque', icon: 'layers', path: '/stock', available: true },
      { label: 'Vendas', icon: 'receipt', path: '/sales', available: true },
    ],
  },
  {
    label: 'Financeiro',
    items: [{ label: 'Financeiro', icon: 'wallet', path: '/finance', available: true }],
  },
];

/** Itens do rodapé do menu, separados dos módulos. */
export const NAV_FOOTER: readonly NavItem[] = [
  { label: 'Configurações', icon: 'sliders', path: '/settings', available: true },
];

export const NAV_ITEMS: readonly NavItem[] = [
  ...NAV_GROUPS.flatMap((group) => group.items),
  ...NAV_FOOTER,
];

/** Grupo do menu da rota atual (para o breadcrumb), pelo item cujo caminho é prefixo da URL. */
export function navGroupOf(url: string): string | null {
  const path = url.split(/[?#]/)[0];
  for (const group of NAV_GROUPS) {
    if (
      group.label &&
      group.items.some(
        (item) => item.path !== '/' && (path === item.path || path.startsWith(item.path + '/')),
      )
    ) {
      return group.label;
    }
  }
  return null;
}
