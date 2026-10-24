# Módulo financeiro (lançamentos)

Contas a receber e a pagar. Decisões técnicas no ADR 0005.

## Regras de negócio

- **Lançamento** = uma parcela de uma conta. Tipo: `Receivable` (a receber) ou `Payable` (a pagar).
- **Categoria** obrigatória e do tipo certo: a receber usa categoria de **receita**, a pagar usa
  **despesa** (400 em `categoryId`). Parceiro opcional (cliente ou fornecedor).
- **Parcelamento**: o total vira de 1 a 60 parcelas mensais a partir do primeiro vencimento, uma linha por
  parcela (`installmentNo`/`installmentTotal`). Os centavos que sobram da divisão vão para a primeira
  parcela, para a soma fechar no total (R$ 100 em 3 → 33,34 + 33,33 + 33,33). Vencimento em dia 31 vira o
  último dia dos meses mais curtos.
- **Situação**: `Open`, `Paid` ou `Cancelled`. **Vencido** não é situação gravada: é um lançamento aberto
  com vencimento anterior a hoje, e os dias de atraso são calculados na leitura. "Hoje" é a data em
  Brasília (às 22h de 31/10 o lançamento que vence dia 31 ainda não está vencido).
- **Baixa** (`pay`): data (padrão hoje, nunca no futuro) e valor pago (padrão o valor da parcela; pode ser
  diferente, por juros ou desconto). Só lançamento aberto é baixado.
- **Lançamento baixado não é editado nem cancelado**, só tem a **baixa estornada** (`reverse-payment`):
  volta a aberto, com a data e o motivo do último estorno guardados (`reversedAt`, `reversalReason`).
- **Cancelar**: só lançamento aberto; deixa de contar nos totais e continua no histórico. Nada é excluído.
- Edição só de lançamento aberto e com `version` (409 `concurrency_conflict`); o tipo não muda.
  Ação que o estado não permite → 409 `invalid_state`.
- Categorias: nome único dentro do tipo; excluída só sem lançamentos; com lançamentos, o tipo não muda.
  A migration cria categorias iniciais (Vendas, Serviços, Outras receitas; Fornecedores, Aluguel,
  Salários, Impostos, Energia e água, Outras despesas), que o usuário pode editar ou excluir.
- Leitura: todos os perfis. Lançar, baixar, estornar e cancelar: Admin e Operador.
- Parcelas geradas por vendas (Fase 6) terão `saleId` e somam o total da venda.

## Endpoints

| Rota | Observação |
| --- | --- |
| `GET /api/v1/financial-entries?kind=` | `status` (`Open`, `Overdue`, `Paid`, `Cancelled`), `dueFrom`, `dueTo`, `categoryId`, `partnerId`, `search` (prefixo da descrição), `sort` (`dueDate`, `amount`, `description`, `createdAt`) |
| `GET /api/v1/financial-entries/summary?kind=` | em aberto, vencido, a vencer em 7 dias e baixado no mês (valor e quantidade) |
| `POST /api/v1/financial-entries` | cria a conta já em parcelas; devolve as parcelas |
| `PUT /api/v1/financial-entries/{id}` | só aberto |
| `POST .../{id}/pay`, `.../{id}/reverse-payment`, `.../{id}/cancel` | ciclo de vida (com `version`) |
| `GET/POST/PUT/DELETE /api/v1/financial-categories` | `kind` filtra receita ou despesa |

## Índices e consultas (seed de 250 mil lançamentos, 24/10/2026)

| Índice | Consulta |
| --- | --- |
| `ix_financial_entry_kind_status_due_date_id` | listas por situação ordenadas por vencimento; vencidos; totais do resumo |
| `ix_financial_entry_kind_due_date_id` | lista sem filtro de situação e filtro por período: sem ele, ordenava 149 mil linhas (29 ms) |
| `ix_financial_entry_kind_paid_at` | "baixado no mês" do resumo |
| `ix_financial_entry_sale_id` | parcelas de uma venda (Fase 6) |
| `ux_financial_category_kind_name` | nome único dentro do tipo |

**Contagem sem joins.** A listagem conta e pagina só na tabela `financial_entry` e depois busca categoria e
parceiro dos 20 da página (deferred join). Contar sobre a consulta com os joins levava **215 ms**; sem eles,
**6 ms**.

## Medição (`measure --target finance --requests 150`, API em Docker)

| Cenário | Antes | Depois (p95) |
| --- | --- | --- |
| a receber em aberto | 201 ms | 10,2 ms |
| a pagar vencidas | 32 ms | 8,1 ms |
| a pagar por período | 411 ms | 29,1 ms |
| busca por descrição | 311 ms | 125,6 ms |
| a pagar, página 50 | 427 ms | 44,3 ms |
| resumo a receber / a pagar | 49 / 70 ms | 48,3 / 70,5 ms |
| **total** | **412 ms** | **117,6 ms** (meta 200 ms) |

A busca por descrição é a mais lenta: percorre os lançamentos do tipo filtrando o prefixo. Se crescer, o
candidato é um índice `(kind, description)`.
