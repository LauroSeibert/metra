---
name: new-endpoint
description: Criar ou alterar um endpoint da API (.NET) seguindo o módulo de produtos, do domínio ao cliente OpenAPI do front.
---

# Novo endpoint (padrão do módulo de produtos)

Referência viva: `backend/src/Erp.*/Products/` e `backend/tests/Erp.IntegrationTests/Products/`.
Copie a estrutura; não invente camada nova.

## Passos

1. **Domínio** (`Erp.Domain/<Modulo>/`): entidade `sealed` herdando `Entity`, construtor privado,
   `static Create(...)` e métodos de alteração. Valide com `ValidationErrors` (chave = nome do campo em camelCase).
2. **Banco** (`Erp.Infrastructure/Persistence/`):
   - `DbSet` em `ErpDbContext` e em `IErpDbContext`;
   - `Configurations/<Entidade>Configuration.cs` chamando `builder.ConfigureEntity()`, com tamanhos, precisão e índices;
   - migration: `dotnet ef migrations add <Nome> --project backend/src/Erp.Infrastructure --startup-project backend/src/Erp.Infrastructure --output-dir Persistence/Migrations`.
3. **Caso de uso** (`Erp.Application/<Modulo>/`):
   - DTOs como `record` em `<Modulo>Dtos.cs` (lista, detalhe com `Version`, requests de criação e edição);
   - `<Entidade>Service` com `ListAsync` (`PageRequest.From`, `SortRequest.Parse`, `Select` para DTO, `AsNoTracking`),
     `GetAsync`, `CreateAsync`, `UpdateAsync` (compara `Version` → `ConflictException.StaleVersion()`);
   - códigos de erro novos em `Common/Exceptions.cs` (`ErrorCodes`, formato `entidade.motivo`);
   - registre o serviço em `Erp.Application/DependencyInjection.cs`.
4. **Endpoint** (`Erp.Api/<Modulo>/<Entidade>Endpoints.cs`): `MapGroup("/kebab-case-plural")`, `WithName` único
   (vira o nome da função no front), `TypedResults`, `ProducesValidationProblem`/`ProducesProblem` para 400/404/409.
   Mapeie em `Program.cs` dentro de `MapGroup("/api/v1")`.
5. **Contrato e front**: `dotnet build backend/Erp.slnx` regenera `backend/openapi/erp-api.json`;
   depois `npm run api:generate` em `frontend/`. Commite os dois juntos.

## Antes de entregar

`dotnet build backend/Erp.slnx`. Sem testes, `EXPLAIN` ou docs de módulo novos (escopo simplificado em `docs/plan.md`).
Regra de negócio fora de `docs/plan.md` ou do glossário: pare e pergunte.
