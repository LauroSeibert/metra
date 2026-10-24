# Módulo de estoque

Um único depósito (decisão do plano; vários depósitos ficam como extensão futura). Decisões técnicas no
ADR 0004.

## Regras de negócio

- **Movimentação é imutável**: só é inserida (`stock_movement`), nunca alterada nem excluída. Correção é
  um **estorno**: um novo movimento com a quantidade oposta, que referencia o original
  (`reference_type = 'StockMovement'`). Cada movimento é estornado no máximo uma vez (409
  `stock_movement.already_reversed`) e um estorno não pode ser estornado (400).
- Quantidade gravada com sinal: entrada positiva, saída negativa. Até 3 casas decimais.
- Tipos manuais:
  - **Entrada** (`Entry`): quantidade maior que zero; custo unitário opcional.
  - **Saída** (`Exit`): quantidade maior que zero, gravada negativa.
  - **Ajuste** (`Adjustment`): a quantidade informada é a **contagem** (o novo saldo); o sistema grava a
    diferença para o saldo atual. Contagem igual ao saldo é recusada (nada a ajustar) e o motivo é
    obrigatório.
  - **Estorno** (`Reversal`): só pela ação de estornar (`POST /stock-movements/{id}/reverse`).
  - A partir da Fase 6, vendas geram os próprios movimentos (`reference_type = 'Sale'`).
- **Saldo** (`stock_balance`): uma linha por produto, criada com zero junto com o produto. Atualizado na
  mesma transação do movimento, com concorrência otimista pela versão; se outra gravação mudou o saldo no
  meio, a operação inteira é refeita (até 3 tentativas). Saldo nunca é editado direto.
- **Invariante**: para todo produto, a soma das quantidades das movimentações é igual ao saldo. Testada sob
  concorrência em `backend/tests/Erp.IntegrationTests/Stock/StockEndpointsTests.cs` (critério de saída da
  Fase 4) e conferida no seed completo (0 divergências em 100 mil produtos).
- **Saldo negativo** conforme a configuração `stock.allowNegative` (`app_setting`; padrão `false`).
  Sem permissão, saída, ajuste ou estorno que deixaria o saldo abaixo de zero → 400 em `quantity`
  ("Saldo insuficiente: disponível X"). A tela de configurações entra na Fase 8.
- Data do movimento (`occurredAt`): opcional, padrão agora; não pode ser no futuro (tolerância de 5 min).
- Leitura: todos os perfis. Lançar e estornar: Admin e Operador.

## Endpoints

| Rota | Observação |
| --- | --- |
| `GET /api/v1/stock-balances` | `search` (prefixo de SKU ou nome), `categoryId`, `lowStock`, `sort` (`name`, `sku`, `quantity`), paginação comum |
| `GET /api/v1/stock-movements` | `productId`, `type`, `from`, `to` (exclusivo), `cursor`, `pageSize`; **paginação por chave** |
| `GET /api/v1/stock-movements/{id}` | inclui `reversedById` |
| `POST /api/v1/stock-movements` | lançamento manual |
| `POST /api/v1/stock-movements/{id}/reverse` | estorno |

`GET /api/v1/products` ganhou `lowStock` e o campo `stockQuantity` (saldo atual).

### Paginação por chave

A resposta é `{ items, nextCursor }`, sem total (contar 1 milhão de linhas é o que fica caro). Ordem:
`occurred_at DESC, id DESC`. O cursor é a data e o id do último item, em base64url; a página seguinte
pede `occurred_at < x OR (occurred_at = x AND id < y)`. Cursor inválido → 400 em `cursor`.

## Índices (EXPLAIN ANALYZE com 1 milhão de movimentações, 24/10/2026)

| Índice | Consulta | Plano e tempo no MySQL |
| --- | --- | --- |
| `ix_stock_movement_occurred_at_id` | histórico geral e páginas seguintes | covering index scan (reverse), 0,26 ms; com cursor no meio da tabela, range scan, 0,74 ms |
| `ix_stock_movement_product_id_occurred_at_id` | histórico de um produto | covering index lookup (reverse), 0,01 ms |
| `ix_stock_movement_reference_type_reference_id` | "já foi estornado?" e, na Fase 6, movimentos de uma venda | covering index lookup, 0,003 ms |
| `ix_stock_movement_user_id` | chave estrangeira para `app_user` | exigido pelo MySQL |
| `ux_stock_balance_product_id` | uma linha por produto; junção do saldo | unique lookup |

## Medição (`measure --target stock --requests 150`, API em Docker, 24/10/2026)

| Cenário | p50 | p95 |
| --- | --- | --- |
| histórico geral | 1,9 ms | 2,5 ms |
| histórico de um produto | 1,9 ms | 2,2 ms |
| histórico até uma data (página profunda) | 5,0 ms | 8,2 ms |
| produto + tipo saída | 1,8 ms | 2,2 ms |
| saldo, primeira página | 54,4 ms | 57,8 ms |
| saldo abaixo do mínimo | 118,2 ms | 126,4 ms |
| saldo por categoria | 13,6 ms | 14,5 ms |
| produtos com estoque baixo | 138,0 ms | 144,0 ms |

Meta de 200 ms atendida. Os filtros de "abaixo do mínimo" são os mais caros: comparam saldo e mínimo de
100 mil produtos para contar o total. Se crescerem, a saída é guardar no saldo uma coluna calculada
"abaixo do mínimo" (atualizada junto com o saldo e quando o mínimo muda) e indexá-la.

## Seed

`seed --profile full` gera 1 milhão de movimentações (10 por produto, nos últimos dois anos, sem nunca
deixar o saldo negativo) com INSERT parametrizado de várias linhas, em menos de 1 minuto, e recalcula os
saldos como a soma das movimentações.
