# Backend (.NET 10)

## Camadas (dependência em uma só direção)
- `Erp.Domain`: entidades e regras, sem referência a outros projetos nem a pacotes de infraestrutura.
- `Erp.Application`: casos de uso (`XxxService`) e DTOs; depende de `Domain` e só das abstrações do EF Core (`IErpDbContext`).
- `Erp.Infrastructure`: `ErpDbContext` (MySQL via `UseMySQL`), configurações, migrations, health check.
- `Erp.Api`: Minimal APIs sob `/api/v1`, ProblemDetails, OpenAPI, CORS.
- `Erp.Seed`: seed com Bogus (`seed --profile small|full`) e medição de p95 (`measure`).
- `tests/Erp.UnitTests/LayerDependencyTests.cs` falha se uma regra de camada for violada.

## Padrões
- Siga `Products/` em cada camada (skill `new-endpoint`). Convenções HTTP: `docs/api-conventions.md`.
- Warnings são erros; versões de pacote só em `Directory.Packages.props`.
- Async de ponta a ponta com `CancellationToken`; relógio via `TimeProvider`.
- Testes de integração: `MySqlApiFixture` (Testcontainers, MySQL 9.7) + `ApiClient`; dados com `Unique(...)`.
  Cliente logado: `fixture.ClientFor(Roles.Operator)`; enums vêm como texto (use `ApiClient.Json`).
- Acesso: Identity em `Erp.Infrastructure/Identity` atrás de `IUserAccounts`/`ISessionTokens`; usuário logado via `ICurrentUser`.

## Armadilhas
- `ErpDbContext` herda de `IdentityDbContext`: `OnModelCreating(builder)` chama `base` antes da convenção de nomes.
- Sem sessão, rota inexistente responde 401 (política padrão), não 404.
- Testes usam Microsoft.Testing.Platform (`global.json` na raiz):
  rode `dotnet test --solution backend/Erp.slnx`, não `dotnet test backend`.
- Connection string local: `dotnet user-secrets set ConnectionStrings:Erp "<valor>" --project backend/src/Erp.Api`.
  Nunca grave senha em `appsettings*.json`.
- `dotnet build` regenera `backend/openapi/erp-api.json`; commite junto com o cliente do front.
- Migration: `dotnet ef migrations add <Nome> --project backend/src/Erp.Infrastructure --startup-project backend/src/Erp.Infrastructure --output-dir Persistence/Migrations`.
- `LIKE` com escape `\` quebra no provider da Oracle; use `SearchPattern` (escape `!`).
- `OR` entre duas colunas com `ORDER BY ... LIMIT` engana o otimizador; veja a busca em `ProductService.ListAsync`.
