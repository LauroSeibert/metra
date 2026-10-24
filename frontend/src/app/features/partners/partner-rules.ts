/**
 * Regras de parceiro espelhadas do servidor (Erp.Domain/Partners). O servidor é a autoridade;
 * aqui só se antecipa o erro e se formata o documento para exibição.
 */
export const PARTNER_RULES = {
  nameMaxLength: 200,
  emailMaxLength: 200,
  phoneMaxLength: 20,
} as const;

/** Só letras maiúsculas e dígitos ("12.abc.345/01de-35" → "12ABC34501DE35"). */
export function normalizeDocument(value: string): string {
  return value.replace(/[^0-9a-z]/gi, '').toUpperCase();
}

/** Formata CPF (000.000.000-00) e CNPJ (00.000.000/0000-00); outro tamanho fica como veio. */
export function formatDocument(value: string): string {
  const doc = normalizeDocument(value);
  if (doc.length === 11) {
    return `${doc.slice(0, 3)}.${doc.slice(3, 6)}.${doc.slice(6, 9)}-${doc.slice(9)}`;
  }
  if (doc.length === 14) {
    return `${doc.slice(0, 2)}.${doc.slice(2, 5)}.${doc.slice(5, 8)}/${doc.slice(8, 12)}-${doc.slice(12)}`;
  }
  return value;
}

/** Valida CPF ou CNPJ (inclusive alfanumérico) com o mesmo algoritmo do servidor. */
export function isValidDocument(value: string): boolean {
  const doc = normalizeDocument(value);
  if (doc.length === 11) {
    return isValidCpf(doc);
  }
  if (doc.length === 14) {
    return isValidCnpj(doc);
  }
  return false;
}

function isValidCpf(cpf: string): boolean {
  if (!/^\d{11}$/.test(cpf) || /^(\d)\1+$/.test(cpf)) {
    return false;
  }
  return (
    cpfDigit(cpf.slice(0, 9)) === Number(cpf[9]) && cpfDigit(cpf.slice(0, 10)) === Number(cpf[10])
  );
}

function cpfDigit(digits: string): number {
  let sum = 0;
  for (let i = 0; i < digits.length; i++) {
    sum += Number(digits[i]) * (digits.length + 1 - i);
  }
  const remainder = sum % 11;
  return remainder < 2 ? 0 : 11 - remainder;
}

function isValidCnpj(cnpj: string): boolean {
  if (!/^[0-9A-Z]{12}\d{2}$/.test(cnpj) || /^(.)\1+$/.test(cnpj)) {
    return false;
  }
  return (
    cnpjDigit(cnpj.slice(0, 12)) === Number(cnpj[12]) &&
    cnpjDigit(cnpj.slice(0, 13)) === Number(cnpj[13])
  );
}

/** Pesos 2 a 9 da direita para a esquerda; cada posição vale o código ASCII menos 48. */
function cnpjDigit(characters: string): number {
  let sum = 0;
  let weight = 2;
  for (let i = characters.length - 1; i >= 0; i--) {
    sum += (characters.charCodeAt(i) - 48) * weight;
    weight = weight === 9 ? 2 : weight + 1;
  }
  const remainder = sum % 11;
  return remainder < 2 ? 0 : 11 - remainder;
}
