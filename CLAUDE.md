# ERP Demo

Simulação de ERP para pequenas empresas de comércio.
Monorepo: `backend/` (.NET 10, MySQL 9.7) e `frontend/` (Angular 22).
Plano completo e fases: `docs/plan.md` (leia só a seção que a tarefa pedir).

## Mapa
- `backend/src/Erp.Domain`: entidades e regras de negócio
- `backend/src/Erp.Application`: casos de uso e DTOs
- `backend/src/Erp.Infrastructure`: EF Core e migrations
- `backend/src/Erp.Api`: endpoints por funcionalidade
- `backend/src/Erp.Seed`: seed sintético e medição de performance
- `frontend/src/app/features`: uma pasta por funcionalidade
- `frontend/src/app/shared/ui`: componentes visuais (DataTable, PageHeader, StatusChip...)

## Comandos
- Ambiente: `cp .env.example .env` (preencha as senhas) e `docker compose up -d --build`
  - Front em http://localhost:8080, API em http://localhost:5080 (`/health`, `/health/ready`)
  - Login: `ADMIN_EMAIL`/`ADMIN_PASSWORD` do `.env`; `DEMO_USERS_PASSWORD` cria `operador@` e `leitura@erp.local`
- API local: `dotnet run --project backend/src/Erp.Api` (connection string via user-secrets)
- Build, testes e lint da API: `dotnet build backend/Erp.slnx`, `dotnet test --solution backend/Erp.slnx`,
  `dotnet format backend/Erp.slnx --verify-no-changes` (testes de integração precisam de Docker)
- Front, dentro de `frontend/`: `npm start`, `npm run build`, `npm test` e `npm run lint`
- Regenerar cliente OpenAPI: `npm run api:generate` dentro de `frontend/` (depois do `dotnet build`)
- Seed: `dotnet run --project backend/src/Erp.Seed -- seed --profile small|full`
- Medir listagens: `dotnet run --project backend/src/Erp.Seed -c Release -- measure --target products|stock|all`,
  com `ERP_EMAIL` e `ERP_PASSWORD`. O seed só completa tabelas vazias (produtos, parceiros, estoque...).

## Regras globais
- Código em inglês, interface em pt-BR; termos de negócio em `docs/domain-glossary.md`.
- Toda rota da API exige login; gravação exige `.RequireWrite()` e a tela esconde a ação sem `canWrite` (ADR 0003).
- Siga o módulo de referência: produtos (`backend/src/Erp.*/Products` e `frontend/src/app/features/products`).
- Mudou a API? Regenere o cliente no mesmo commit.
- Visual: siga `docs/design-system.md` e use só os tokens de `frontend/src/app/styles/`.
- Nova dependência exige ADR em `docs/adr/`.
- Ao terminar, rode build, testes e lint e relate o resultado real.

## Leia só o que for tocar
- Convenções de API: `docs/api-conventions.md`
- Regras de negócio por módulo: `docs/modules/` (um arquivo por módulo)
- Camadas e dependências: `docs/architecture.md`
- Decisões de arquitetura: `docs/adr/`

## Pare e pergunte
Regra de negócio nova, mudança de contrato público, esquema destrutivo ou nova dependência.
