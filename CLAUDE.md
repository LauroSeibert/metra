# ERP Demo

Simulação de ERP para pequenas empresas de comércio.
Monorepo: `backend/` (.NET 10, MySQL 9.7) e `frontend/` (Angular 22).
Plano completo e fases: `docs/plan.md` (leia só a seção que a tarefa pedir).

## Mapa
- `backend/src/Erp.Domain`: entidades e regras de negócio
- `backend/src/Erp.Application`: casos de uso e DTOs
- `backend/src/Erp.Infrastructure`: EF Core e migrations
- `backend/src/Erp.Api`: endpoints por funcionalidade
- `frontend/src/app/features`: uma pasta por funcionalidade
- `frontend/src/app/styles`: tokens de design e tema do Material

## Comandos
- Ambiente: `cp .env.example .env` (preencha as senhas) e `docker compose up -d --build`
  - Front em http://localhost:8080, API em http://localhost:5080 (`/health`, `/health/ready`)
- API local: `dotnet run --project backend/src/Erp.Api`
- Build e testes da API: `dotnet build backend/Erp.slnx` e `dotnet test --solution backend/Erp.slnx`
- Lint da API: `dotnet format backend/Erp.slnx --verify-no-changes`
- Front, dentro de `frontend/`: `npm start`, `npm run build`, `npm test` e `npm run lint`

## Regras globais
- Código em inglês, interface em pt-BR.
- Mudou a API? Regenere o cliente no mesmo commit.
- Visual: use só os tokens de `frontend/src/app/styles/tokens.scss`.
- Nova dependência exige ADR em `docs/adr/`.
- Ao terminar, rode build, testes e lint e relate o resultado real.

## Leia só o que for tocar
- Camadas e dependências: `docs/architecture.md`
- Decisões de arquitetura: `docs/adr/`

## Pare e pergunte
Regra de negócio nova, mudança de contrato público, esquema destrutivo ou nova dependência.

<!-- Fase 1. Entram nas próximas fases: glossário, api-conventions, design-system,
módulo de referência (produtos), seed e api:generate. Só cite arquivos que existam. -->
