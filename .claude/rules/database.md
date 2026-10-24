---
paths:
  - "backend/src/Erp.Infrastructure/**/*.cs"
  - "backend/src/Erp.Domain/**/*.cs"
---

# Regras de banco e domínio

- Esquema só muda por migration nova (`dotnet ef migrations add <NomeDescritivo>`); nunca editar migration existente.
  `DROP` ou mudança destrutiva exige aprovação explícita.
- Nomes saem em snake_case pela convenção de `SnakeCaseNaming`; não use `ToTable`/`HasColumnName` à mão.
- Dinheiro `DecimalLimits.Money*` (18,2), quantidade `DecimalLimits.Quantity*` (18,3). Nunca float/double.
- Entidade herda de `Entity` (id BIGINT, created_at, updated_at, version); datas e versão são preenchidas no `SaveChanges`.
- Regras de entrada ficam no domínio com `ValidationErrors` (chaves em camelCase iguais ao contrato).
- Índice novo só com a consulta que o justifica.
- Provider Oracle: `LIKE ... ESCAPE '\'` quebra o SQL; use `SearchPattern.EscapeCharacter` ("!").
