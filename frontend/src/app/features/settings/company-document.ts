/**
 * Máscara do CNPJ (00.000.000/0000-00) enquanto se digita. Aceita letras nos 12 primeiros
 * caracteres (CNPJ alfanumérico) e limita a 14 caracteres úteis.
 */
export function formatCompanyDocument(raw: string): string {
  const chars = raw
    .toUpperCase()
    .replace(/[^0-9A-Z]/g, '')
    .slice(0, 14);
  const parts = [
    chars.slice(0, 2),
    chars.slice(2, 5),
    chars.slice(5, 8),
    chars.slice(8, 12),
    chars.slice(12, 14),
  ];
  let result = parts[0];
  if (parts[1]) result += `.${parts[1]}`;
  if (parts[2]) result += `.${parts[2]}`;
  if (parts[3]) result += `/${parts[3]}`;
  if (parts[4]) result += `-${parts[4]}`;
  return result;
}

/** Iniciais para o avatar: primeira letra do primeiro e do último nome. */
export function initialsOf(fullName: string): string {
  const words = fullName.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) {
    return '?';
  }
  const first = words[0][0];
  const last = words.length > 1 ? words[words.length - 1][0] : '';
  return (first + last).toUpperCase();
}
