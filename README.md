# Metra

Metra is a demo ERP for small retail businesses. It is deliberately small, but it is built the way a real ERP
should be.

## Goals

1. **Apply good ERP practices, even in a low-complexity system.** The functionality is only a base for the second
   goal, so the focus is on how it is built:
   - **Module organization:** clean layers on the API (domain, application, infrastructure, endpoints) and one
     folder per feature on the front end, with a reference module that new ones copy.
   - **Tests:** unit tests for domain rules, integration tests against a real MySQL, and front-end component tests.
   - **Performance:** server-side pagination, filtering and sorting, with every new index checked with `EXPLAIN`
     against a seed of 100 thousand products and 1 million stock movements.
   - **Data integrity:** stock movements and financial entries are never deleted, balances are updated
     transactionally, numbering is sequential without gaps, and edits use optimistic concurrency.
   - **Contracts and access:** an OpenAPI contract with a generated client, role-based authorization, and business
     rules documented per module.
2. **Measure how much Markdown context files affect Claude Code.** The repository carries project context as
   Markdown (`CLAUDE.md`, rules, skills and module docs). A goal of the project is to measure how those files change
   token usage and the speed with which Claude Code makes changes and improvements. The plan is to branch from the
   same commit, remove or restore the context files per scenario, and repeat the same tasks at least three times,
   recording model versions, prompts and results (see `docs/plan.md`).

## Stack

| Layer | Technologies |
| --- | --- |
| Front end | Angular 22, Signal Forms, Spartan UI, Tailwind CSS v4, Lucide icons |
| API | .NET 10 (Minimal APIs), EF Core, OpenAPI with a generated TypeScript client |
| Database | MySQL 9.7 |
| Runtime | Docker Compose |

## Run

```sh
git clone https://github.com/LauroSeibert/metra.git && cd metra
cp .env.example .env   # fill in the passwords
docker compose up -d --build
dotnet run --project backend/src/Erp.Seed -- seed --profile small   # sample data (optional)
```

- Front end: http://localhost:8080 (sign in with `ADMIN_EMAIL`/`ADMIN_PASSWORD` from `.env`)
- API: http://localhost:5080 (`/health`, `/health/ready`)
- The Docker Compose project is named `metra` (containers `metra-*`, volumes `metra_mysql-data` and `metra_api-keys`).

## Develop

```sh
dotnet build backend/Erp.slnx                  # API
dotnet test --solution backend/Erp.slnx        # tests (need Docker)
cd frontend && npm start                       # front end in dev mode (proxies to the API)
cd frontend && npm run build && npm run lint && npm test
cd frontend && npm run api:generate            # regenerate the OpenAPI client after changing the API
```

## Context for the AI

The project instructions and docs are written in Brazilian Portuguese, and so is the user interface. Code is in
English.

| File | Purpose |
| --- | --- |
| `CLAUDE.md`, `backend/CLAUDE.md`, `frontend/CLAUDE.md` | map, commands, rules and pitfalls |
| `.claude/rules/*.md` | rules by topic, loaded only for the paths they apply to |
| `.claude/skills/` | recipes: new endpoint and new screen |
| `docs/plan.md` | plan and phases (see "Escopo simplificado") |
| `docs/design-system.md` + `docs/design/listagem.png` | visual identity (Spartan UI + Tailwind + `--erp-*` tokens) |
| `docs/modules/`, `docs/api-conventions.md` | business rules per module and API conventions |

## License

[MIT](LICENSE) © 2026 Lauro Seibert
