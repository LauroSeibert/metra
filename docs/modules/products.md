# Módulo de produtos

Módulo de referência: novos módulos copiam sua estrutura (skills `new-endpoint` e `new-feature-screen`).

## Regras de negócio

- O produto é identificado pelo `id`; o SKU é o código de negócio (busca, etiqueta, balcão).
- SKU opcional na entrada, até 40 caracteres, letras, números, `.`, `-` ou `_`, começando por letra ou número.
  É gravado sem espaços nas pontas e em maiúsculas; duplicidade (ignorando caixa) → 409 `product.sku_duplicate`.
- SKU em branco (na criação ou na edição) é gerado: três primeiras letras ou números do nome da categoria, sem
  acento, e o id com ao menos seis dígitos (`Livros`, id 33488 → `LIV-033488`; sem letras → `PRD-...`). Se alguém já
  digitou esse código, ganha sufixo (`LIV-033488-2`). Na criação, o produto é gravado com um SKU provisório
  (`~` + Guid) e o gerado entra na mesma transação; por isso o produto nasce na versão 2.
- Nome obrigatório (até 200); descrição opcional (até 1000; vazia vira `null`).
- Categoria e unidade opcionais (`null`); se informadas, precisam existir (senão 400 nos campos
  `categoryId`/`unitId`). Sem categoria, o SKU gerado usa o prefixo `PRD`.
- Preço de custo, preço de venda e estoque mínimo não negativos, com no máximo 2, 2 e 3 casas decimais.
  Venda abaixo do custo é permitida (margem negativa aparece em vermelho na tela).
- Produto nunca é excluído: é inativado (`POST /products/{id}/deactivate`) e pode ser reativado.
- Categoria (nome único) e unidade (código único) só são excluídas sem produtos (409 `*.in_use`).
- Filtro "estoque baixo" fica para a Fase 4, quando existir `stock_balance`; `min_stock` já é gravado.

## Endpoints

| Rota | Uso |
| --- | --- |
| `GET /api/v1/products` | `search`, `categoryId`, `active`, `sort` (`name`, `sku`, `salePrice`, `createdAt`), `page`, `pageSize` |
| `GET/PUT /api/v1/products/{id}` | detalhe e edição (com `version`) |
| `POST /api/v1/products` | cadastro |
| `POST /api/v1/products/{id}/activate` · `/deactivate` | situação |
| `/api/v1/product-categories`, `/api/v1/units` | CRUD com exclusão protegida e `productCount` |

## Índices e consultas (100 mil produtos, MySQL 9.7)

| Índice | Consulta que o justifica | Plano |
| --- | --- | --- |
| `ux_product_sku` | unicidade do SKU; busca por prefixo de SKU | range scan |
| `ix_product_name` | ordenação padrão por nome; busca por prefixo de nome | index scan com `LIMIT` |
| `ix_product_category_id_name` | filtro por categoria ordenado por nome | index lookup, sem filesort |
| `ix_product_active_name` | filtro ativos/inativos ordenado por nome | index lookup, sem filesort |
| `ix_product_sale_price` | `sort=salePrice` / `-salePrice` | sem ele: filesort de 100 mil linhas (64 ms → 11 ms no p95) |
| `ix_product_created_at` | `sort=createdAt` | mesmo caso da ordenação por preço |
| `ix_product_unit_id` | chave estrangeira | criado pela FK |

### Busca em duas colunas (SKU ou nome)

Testamos três formas para `search` no seed completo. Tempos no MySQL, pior caso entre prefixos de 2 letras:

| Forma | Itens | Contagem | Problema |
| --- | --- | --- | --- |
| `sku LIKE p OR name LIKE p` | 0,3–169 ms | 18 ms (table scan) | com `ORDER BY ... LIMIT`, o MySQL percorre o índice da ordenação lendo linha a linha; lento quando há poucos casamentos |
| `id IN (SELECT ... UNION SELECT ...)` | até 645 ms | — | vira subconsulta dependente |
| **UNION estreito (id + colunas de ordenação) ordenado e paginado na tabela derivada, depois junção** | 0,5–45 ms | 1–6 ms | custo proporcional ao número de casamentos |

Adotada a terceira (`ProductService.ListAsync`). O UNION com linhas inteiras (incluindo `description`) custava 93 ms
para 15 mil casamentos; com a projeção estreita, 15 ms.

### Achado do provider

`MySql.EntityFrameworkCore` 10.0.12 gera `LIKE @p ESCAPE '\'` sem escapar a barra, e o MySQL rejeita o SQL.
Por isso o caractere de escape é `!` (`SearchPattern.EscapeCharacter`). Coberto por
`List_WildcardInSearch_IsTreatedAsLiteral`.

## Medição (critério de saída da Fase 2)

`dotnet run --project backend/src/Erp.Seed -c Release -- measure --requests 200`, API em Docker, 100 mil produtos,
23/10/2026:

| Cenário | p50 | p95 |
| --- | --- | --- |
| primeira página | 10,6 ms | 12,9 ms |
| busca por nome (prefixo de 2 letras) | 33,0 ms | 85,7 ms |
| busca por SKU | 4,7 ms | 18,9 ms |
| filtro categoria | 3,0 ms | 3,8 ms |
| filtro inativos | 3,4 ms | 4,1 ms |
| ordem por preço desc | 9,1 ms | 11,1 ms |
| página 100, 50 itens (offset 4.950) | 93,1 ms | 103,6 ms |
| busca + categoria + ordem | 6,3 ms | 18,5 ms |
| **total** | **8,7 ms** | **94,2 ms** (meta: 200 ms) |

A página profunda é o caso mais lento (offset). Navegar até a página 100 é raro na tela; se virar problema,
aplicar paginação por chave como nas movimentações.
