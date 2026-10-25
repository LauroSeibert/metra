/** Mensagem do primeiro erro de um campo do Signal Forms, só depois de tocado ou do envio. */
export function fieldError(field: {
  touched(): boolean;
  errors(): readonly { readonly message?: string }[];
}): string | null {
  return field.touched() ? (field.errors()[0]?.message ?? null) : null;
}
