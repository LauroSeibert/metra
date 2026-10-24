# ERP Demo

ERP de demonstração para pequenas empresas de comércio, feito para estudar como organizar contexto para o
Claude Code (`CLAUDE.md`, rules e skills) num sistema de porte médio gastando poucos tokens.

Módulos: produtos, parceiros (clientes e fornecedores), estoque, contas a receber e a pagar, vendas,
dashboard e configurações. Stack: Angular 22 + Angular Material, .NET 10 (Minimal APIs, EF Core) e MySQL 9.7.

## Rodar

```sh
cp .env.example .env   # preencha as senhas
docker compose up -d --build
dotnet run --project backend/src/Erp.Seed -- seed --profile small   # dados de exemplo (opcional)
```

- Front: http://localhost:8080 (login com `ADMIN_EMAIL`/`ADMIN_PASSWORD` do `.env`)
- API: http://localhost:5080 (`/health`, `/health/ready`)

## Onde está o contexto para a IA

| Arquivo | Para quê |
| --- | --- |
| `CLAUDE.md`, `backend/CLAUDE.md`, `frontend/CLAUDE.md` | mapa, comandos, regras e armadilhas |
| `.claude/rules/*.md` | regras por tema, carregadas só nos caminhos que importam |
| `.claude/skills/` | receitas: novo endpoint e nova tela |
| `docs/plan.md` | plano e fases (veja "Escopo simplificado") |
| `docs/design-system.md` + `docs/design/listagem.png` | identidade visual |
