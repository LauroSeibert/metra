# Backend (.NET 10)

## Camadas (dependência em uma só direção)
- `Erp.Domain`: sem referência a outros projetos nem a pacotes de infraestrutura.
- `Erp.Application`: depende só de `Domain`; define as interfaces que a infraestrutura implementa.
- `Erp.Infrastructure`: EF Core (`MySql.EntityFrameworkCore`, `UseMySQL`), health check do banco.
- `Erp.Api`: Minimal APIs sob `/api/v1`, DI, OpenAPI, ProblemDetails, CORS.
- `tests/Erp.UnitTests/LayerDependencyTests.cs` quebra o build de testes se uma regra for violada.

## Padrões
- Warnings são erros (`Directory.Build.props`); versões de pacote só em `Directory.Packages.props`.
- Async de ponta a ponta com `CancellationToken`; relógio via `TimeProvider`.
- Nunca expor entidade do EF; sempre DTO.

## Armadilhas
- Testes usam Microsoft.Testing.Platform (configurado em `global.json` na raiz):
  rode `dotnet test --solution backend/Erp.slnx`, não `dotnet test backend`.
- Connection string local: `dotnet user-secrets set ConnectionStrings:Erp "<valor>" --project backend/src/Erp.Api`.
  Nunca grave senha em `appsettings*.json`.
- `/health` não toca o banco; `/health/ready` testa a conexão.
