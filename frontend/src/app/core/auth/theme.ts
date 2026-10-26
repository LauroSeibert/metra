import { ThemePreference } from '../../api/models/theme-preference';

const THEME_KEY = 'erp.theme';

/**
 * Aplica o tema no <html> (os tokens escuros ficam em styles/tokens.scss) e lembra a escolha, para
 * a próxima abertura já começar no tema certo, antes de o perfil chegar da API.
 */
export function applyTheme(theme: ThemePreference): void {
  suspendTransitions();
  if (theme === 'Dark') {
    document.documentElement.setAttribute('data-theme', 'dark');
  } else {
    document.documentElement.removeAttribute('data-theme');
  }
  try {
    localStorage.setItem(THEME_KEY, theme);
  } catch {
    // Sem armazenamento: o tema só não é lembrado antes do login.
  }
}

/** Tema lembrado da última sessão (usado na abertura do app). */
export function restoreTheme(): void {
  try {
    const theme = localStorage.getItem(THEME_KEY);
    if (theme === 'Dark' || theme === 'Light') {
      applyTheme(theme);
    }
  } catch {
    // Mantém o tema claro.
  }
}

/**
 * Desliga as transições por um instante ao trocar o tema: sem isso, fundos e bordas animam da cor
 * antiga para a nova e passam por tons intermediários (o "piscar" de botões e células).
 */
function suspendTransitions(): void {
  const root = document.documentElement;
  root.classList.add('theme-switching');
  // Força o navegador a aplicar o novo estilo sem transição antes de religá-las.
  void getComputedStyle(root).opacity;
  setTimeout(() => root.classList.remove('theme-switching'), 100);
}
