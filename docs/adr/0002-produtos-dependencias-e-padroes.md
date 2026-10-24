# ADR 0002: Dependências e padrões do módulo de produtos

- **Data:** 2026-10-23
- **Status:** aceito

## Contexto

A Fase 2 cria o módulo de referência (produtos) e fecha duas decisões em aberto do plano: o padrão de
formulário e o gerador do cliente OpenAPI. As decisões foram tomadas pela IA seguindo as recomendações do
plano e ficam registradas aqui para revisão.

## Decisões

### Dependências novas

| Dependência | Versão | Licença | Motivo |
| --- | --- | --- | --- |
| `Microsoft.EntityFrameworkCore` (Application) | 10.0.13 | MIT | A `Application` referencia só as abstrações do EF Core, sem o provider |
| `Microsoft.EntityFrameworkCore.Design` + ferramenta `dotnet-ef` | 10.0.13 | MIT | Migrations versionadas |
| `Microsoft.Extensions.ApiDescription.Server` | 10.0.13 | MIT | Gera `backend/openapi/erp-api.json` no build |
| `Microsoft.Extensions.Hosting` (Erp.Seed) | 10.0.13 | MIT | Configuração e user-secrets do seed |
| `Bogus` | 35.6.5 | MIT | Dados sintéticos e reproduzíveis do seed |
| `Testcontainers.MySql` | 4.16.0 | MIT | Testes de integração com MySQL 9.7 real |
| `ng-openapi-gen` | 1.1.0 | MIT | Cliente TypeScript gerado (funções tipadas + `Api.invoke`) |

Validação de entrada: regras no domínio (`ValidationErrors`), sem FluentValidation.

### Acesso a dados

A `Application` define `IErpDbContext` (com `DbSet`) e os casos de uso consultam direto, com `Select` para DTO.
Alternativa descartada: repositórios na Infrastructure, que dobram os arquivos por funcionalidade sem ganho
para um monólito com um único banco. A regra "Domain sem dependências" segue verificada por teste.

### Formulários

Signal Forms (`@angular/forms/signals`), estável no Angular 22. O Angular Material 22 integra direto
(`matInput`, `mat-select` e `mat-error` com `[formField]`), sem adaptadores. Reactive Forms não é usado.

### Gerador do cliente

`ng-openapi-gen`, entre NSwag, `ng-openapi-gen` e `openapi-typescript`, por gerar serviços Angular tipados a partir
de um script npm, sem runtime extra. O contrato sai em OpenAPI 3.0 (`--openapi-version OpenApi3_0`) para
compatibilidade com o gerador.

### Outros padrões

- Caractere de escape do `LIKE` é `!` por causa de um defeito do provider (ver `docs/modules/products.md`).
- Orçamento de bundle inicial passou a 550 kB (aviso) e 650 kB (erro); medido 514 kB após a Fase 2.
- Hooks do Claude Code (`.claude/settings.json`) bloqueiam edição de arquivos gerados e formatam o front.

## Consequências

- Mudou a API: `dotnet build` e `npm run api:generate` no mesmo commit; o CI confere os dois.
- Testes de integração exigem Docker (Testcontainers).
- Filtro de estoque baixo adiado para a Fase 4 (depende de `stock_balance`).
