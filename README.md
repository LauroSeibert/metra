# Metra

Metra é um ERP de demonstração para pequenas empresas de comércio, feito para estudar como organizar contexto para
o Claude Code (`CLAUDE.md`, rules e skills) num sistema de porte médio gastando poucos tokens.

## O que tem

Produtos (com código do sistema, SKU, duplicar e excluir), parceiros (clientes e fornecedores), estoque,
contas a receber e a pagar (parcelas, baixa, estorno e histórico), vendas, dashboard, configurações e perfil,
com tema claro e escuro e três perfis de acesso (administrador, operador e leitura).

## Stack

| Camada | Tecnologias |
| --- | --- |
| Front | Angular 22, Signal Forms, Spartan UI, Tailwind CSS v4, ícones Lucide |
| API | .NET 10 (Minimal APIs), EF Core, OpenAPI com cliente TypeScript gerado |
| Banco | MySQL 9.7 |
| Execução | Docker Compose |

## Rodar

```sh
git clone https://github.com/LauroSeibert/metra.git && cd metra
cp .env.example .env   # preencha as senhas
docker compose up -d --build
dotnet run --project backend/src/Erp.Seed -- seed --profile small   # dados de exemplo (opcional)
```

- Front: http://localhost:8080 (login com `ADMIN_EMAIL`/`ADMIN_PASSWORD` do `.env`)
- API: http://localhost:5080 (`/health`, `/health/ready`)
- O projeto do Docker Compose chama `metra` (containers `metra-*`, volumes `metra_mysql-data` e `metra_api-keys`).

## Desenvolver

```sh
dotnet build backend/Erp.slnx                  # API
dotnet test --solution backend/Erp.slnx        # testes (precisam do Docker)
cd frontend && npm start                       # front em modo dev (proxy para a API)
cd frontend && npm run build && npm run lint && npm test
cd frontend && npm run api:generate            # regenera o cliente OpenAPI depois de mudar a API
```

## Onde está o contexto para a IA

| Arquivo | Para quê |
| --- | --- |
| `CLAUDE.md`, `backend/CLAUDE.md`, `frontend/CLAUDE.md` | mapa, comandos, regras e armadilhas |
| `.claude/rules/*.md` | regras por tema, carregadas só nos caminhos que importam |
| `.claude/skills/` | receitas: novo endpoint e nova tela |
| `docs/plan.md` | plano e fases (veja "Escopo simplificado") |
| `docs/design-system.md` + `docs/design/listagem.png` | identidade visual (Spartan UI + Tailwind + tokens `--erp-*`) |
| `docs/modules/`, `docs/api-conventions.md` | regras de negócio por módulo e convenções da API |
