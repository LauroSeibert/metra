# Metra

Metra is a demo ERP for small retail businesses, built to study how to organize context for Claude Code
(`CLAUDE.md`, rules and skills) in a mid-sized system while spending few tokens.

## Features

Products (system code, SKU, duplicate and delete), partners (customers and suppliers), stock, accounts receivable
and payable (installments, payment, reversal and history), sales, dashboard, settings and profile, with light and
dark themes and three access roles (administrator, operator and read-only).

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
