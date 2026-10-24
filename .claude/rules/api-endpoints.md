---
paths:
  - "backend/src/Erp.Api/**/*.cs"
  - "backend/src/Erp.Application/**/*.cs"
---

# Regras de endpoints e casos de uso

- Endpoint fino: recebe a entrada, chama um caso de uso (`XxxService`) e devolve DTO com `TypedResults`.
- Nunca devolver entidade do EF; projetar com `Select` para DTO e `AsNoTracking` nas leituras.
- Erros saem como exceção e viram ProblemDetails em `ErrorHandling/ApiExceptionHandler.cs`:
  `DomainValidationException` 400, `NotFoundException` 404, `ConflictException` 409. Código estável em `ErrorCodes`.
- Toda listagem usa `PageRequest.From` (máximo 100) e `ToPagedResultAsync`; ordenação só por `SortRequest` com lista fechada.
- Busca por prefixo com `SearchPattern.Prefix` + `EF.Functions.Like(..., SearchPattern.EscapeCharacter)`.
- Toda rota exige login (política padrão). Endpoint que grava: `.RequireWrite()`; administração: `Policies.Admin`.
  Dados do usuário logado vêm de `ICurrentUser`, nunca de um id na rota.
- Edição recebe `Version` e compara antes de salvar (409 `concurrency_conflict`).
- Mudou endpoint ou DTO: `dotnet build` regenera `backend/openapi/erp-api.json`; rode `npm run api:generate` no front no mesmo commit.
- Padrão completo: `docs/api-conventions.md`. Receita: skill `new-endpoint`.
