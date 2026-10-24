# ADR 0001: Stack e dependências iniciais

- **Data:** 2026-10-23
- **Status:** aceito

## Contexto

A Fase 1 (Fundação) precisa da solução .NET, do projeto Angular e da conexão com o MySQL. As regras do projeto exigem ADR e conferência de licença para toda dependência.

## Decisão

| Dependência | Versão | Licença | Motivo |
| --- | --- | --- | --- |
| .NET SDK / ASP.NET Core | 10.0 (SDK 10.0.402) | MIT | Runtime LTS, suporte até 11/2028 |
| `MySql.EntityFrameworkCore` | 10.0.12 (puxa EF Core 10.0.12) | `GPL-2.0-only WITH Universal-FOSS-exception-1.0` (conferida no nuspec) | Provider oficial do MySQL para EF Core 10; o Pomelo não tem build para EF Core 10. A exceção permite uso com software sob outras licenças, mas a licença deve ser revista antes de qualquer distribuição comercial |
| `Microsoft.AspNetCore.OpenApi` | 10.0.13 | MIT | OpenAPI nativo |
| `xunit.v3` | 4.0.2 | Apache-2.0 | Testes, rodando em Microsoft.Testing.Platform |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.13 | MIT | `WebApplicationFactory` nos testes de integração |
| Angular, Angular CDK e Material | 22.2.x | MIT | Framework e UI do front |
| Vitest, jsdom, Prettier | do template do Angular CLI 22 | MIT | Testes e checagem de formatação |
| Imagens `mysql:9.7`, `dotnet/sdk:10.0`, `dotnet/aspnet:10.0`, `node:24-alpine`, `nginx:1.29-alpine` | — | GPL-2.0 (MySQL), MIT (.NET), MIT (Node), BSD-2 (nginx) | Ambiente com um comando |
| Fontes Inter e Material Symbols (Google Fonts) | — | OFL-1.1 / Apache-2.0 | Tipografia e ícones do protótipo |

Regras associadas:
- A versão maior do provider MySQL é sempre igual à do EF Core (10).
- Versões de pacote .NET ficam só em `backend/Directory.Packages.props`.

## Consequências

- O provider da Oracle traduz consultas de forma diferente do Pomelo: buscas por prefixo usam `EF.Functions.Like` explícito e são conferidas com `EXPLAIN`.
- O SDK 10 exige Microsoft.Testing.Platform para o xUnit v3 (`global.json`), então o comando de teste é `dotnet test --solution backend/Erp.slnx`.
- O lint do front por enquanto é só formatação (Prettier). Adotar ESLint (`angular-eslint`) fica para decisão futura, com outro ADR.
