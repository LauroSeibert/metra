/** Código do sistema com seis dígitos (000123), igual ao que a listagem ordena. */
export function productCode(code: number): string {
  return String(code).padStart(6, '0');
}
