# Convenções da API

Vale para todo endpoint sob `/api/v1`. Referência de implementação: módulo de produtos
(`backend/src/Erp.Api/Products`).

## Rotas

| Ação | Verbo e rota | Resposta |
| --- | --- | --- |
| Listar | `GET /api/v1/{recurso}` | 200 `PagedResult<ItemDto>` |
| Detalhar | `GET /api/v1/{recurso}/{id}` | 200 `Dto` ou 404 |
| Criar | `POST /api/v1/{recurso}` | 201 `Dto` com `Location` |
| Editar | `PUT /api/v1/{recurso}/{id}` | 200 `Dto` (com nova `version`) |
| Ação de estado | `POST /api/v1/{recurso}/{id}/{acao}` (ex.: `activate`) | 200 `Dto` |
| Excluir | `DELETE /api/v1/{recurso}/{id}` (só sem histórico) | 204 |

- Recurso no plural e em kebab-case: `products`, `product-categories`, `stock-movements`.
- `WithName("ListProducts")` define o `operationId`, que vira o nome da função no cliente do front.
- Registro com histórico nunca tem `DELETE`: é inativado (produto) ou estornado (movimentos, lançamentos).

## Autenticação e perfis

- Toda rota sob `/api/v1` exige `Authorization: Bearer <accessToken>` (política padrão), exceto `/auth/login` e `/auth/refresh`.
  Sem sessão → 401; perfil sem permissão → 403 (ProblemDetails sem `code`).
- Endpoint que grava: `.RequireWrite()` (Admin ou Operator). Administração: `Policies.Admin`.
- Dados do próprio usuário em `/api/v1/me` (sem id na rota). Detalhes no ADR 0003 e em `docs/modules/access.md`.

## Listagens

| Parâmetro | Regra |
| --- | --- |
| `page` | começa em 1; menor que 1 → 400 |
| `pageSize` | padrão 20, máximo 100; fora disso → 400 |
| `sort` | `campo` ou `-campo` (decrescente), só da lista fechada do caso de uso; outro → 400 |
| `search` | busca por prefixo (`termo%`), curingas digitados são literais |
| filtros | nomes do contrato (`categoryId`, `active`); ausente = sem filtro |

Resposta: `{ items, page, pageSize, totalCount }`. A ordenação sempre desempata por `id` para a paginação ser estável.
Tabelas grandes de histórico usam **paginação por chave**: `GET ...?cursor=&pageSize=` responde `{ items, nextCursor }`, sem total,
em `page`. `nextCursor` nulo = fim. Referência: `GET /api/v1/stock-movements` (`docs/modules/stock.md`).

## Erros (ProblemDetails, RFC 9457)

Todo erro sai como `application/problem+json` com o campo `code` estável:

| Status | Quando | `code` | Extra |
| --- | --- | --- | --- |
| 400 | validação de entrada ou de regra de domínio | `validation_failed` | `errors: { campo: [mensagens] }` |
| 404 | registro inexistente | `entidade.not_found` | |
| 409 | duplicidade de chave de negócio | `entidade.campo_duplicate` | |
| 409 | versão desatualizada (concorrência otimista) | `concurrency_conflict` | |
| 409 | exclusão de registro em uso | `entidade.in_use` | |
| 409 | ação que só vale uma vez (ex.: estorno repetido) | `entidade.already_<ação>` | |
| 409 | índice único violado numa corrida | `duplicate_key` | |
| 401 | credenciais, conta bloqueada, refresh inválido | `auth.invalid_credentials`, `auth.locked_out`, `auth.invalid_refresh_token` | |
| 401 / 403 | sem sessão / perfil sem permissão | (sem `code`) | |
| 429 | muitas tentativas de login | (sem `code`) | |
| 500 | erro inesperado (logado, sem detalhes na resposta) | `unexpected_error` | |

- As chaves de `errors` são os nomes dos campos do contrato (camelCase), para o front marcar o campo certo.
- Mensagens (`detail`) em pt-BR, prontas para a tela.

## Concorrência otimista

- Toda entidade tem `version` (int), incrementada pelo `ErpDbContext` a cada alteração.
- O cliente envia a `version` que leu no `PUT`. Diferente da atual → 409 `concurrency_conflict`.
- A coluna também é token de concorrência no EF: duas gravações simultâneas não passam juntas.

## Tipos

- Ids `int64`; dinheiro e quantidade `number` (no banco, DECIMAL 18,2 e 18,3); datas ISO 8601 em UTC.
- Campos não anuláveis no C# saem como `required` no OpenAPI (`RequiredPropertiesSchemaTransformer`).
- Números nunca como string (`JsonNumberHandling.Strict`).
- Enums como texto (`JsonStringEnumConverter`): `"Customer"`, `"Dark"`; o cliente gerado tipa como união de strings.

## Contrato e cliente

- `dotnet build` gera `backend/openapi/erp-api.json` (OpenAPI 3.0), que é commitado.
- `npm run api:generate` em `frontend/` gera `src/app/api/` com `ng-openapi-gen`. Nunca editar à mão.
- O CI falha se o contrato ou o cliente commitados não baterem com o código.
