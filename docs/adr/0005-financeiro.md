# ADR 0005: Lançamentos financeiros

- **Data:** 2026-10-24
- **Status:** aceito (decisões tomadas pela IA seguindo o plano, para revisão)

## Contexto

A Fase 5 pede contas a pagar e a receber com parcelas, baixa e estorno, atraso calculado pelo vencimento e
o padrão "cartão de resumo + tabela". O plano lista três situações (aberto, pago, cancelado) e diz que o
lançamento baixado "não é editado, só estornado". Nenhuma dependência nova.

## Decisões

1. **Estorno da baixa volta o lançamento a aberto.** Com só três situações no plano, "estornado" é a ação
   sobre a baixa, não uma quarta situação. O lançamento guarda a data e o motivo do **último** estorno
   (`reversed_at`, `reversal_reason`). Alternativa para auditoria completa: uma tabela de baixas, uma linha
   por baixa e estorno — fica para quando houver baixa parcial.
2. **Vencido é calculado**, não gravado: aberto com `due_date` anterior a hoje. Evita rotina noturna para
   "virar" situações e nunca fica desatualizado.
3. **"Hoje" no fuso da empresa** (`BusinessCalendar`, America/Sao_Paulo com reserva para o id do Windows e,
   sem base de fusos, UTC−3 fixo). Vencimento e baixa são `DateOnly` em colunas `DATE`.
4. **Conversor `DateOnly` ↔ `DateTime`** em todas as propriedades `DateOnly`: o provider da Oracle lê `DATE`
   como `DateTime` e falhava ao materializar `DateOnly` (risco de provider previsto no plano).
5. **Parcelas mensais com resto na primeira.** A soma das parcelas sempre fecha no total; o front mostra a
   prévia com o mesmo cálculo (`previewInstallments`).
6. **Contagem sem joins (deferred join)** na listagem; ver `docs/modules/finance.md` (215 ms → 6 ms).
7. **Categorias iniciais na migration.** Para a tela não começar vazia e as vendas (Fase 6) terem a
   categoria "Vendas". São dados editáveis, não regra.
8. **`InvalidStateException` → 409 `invalid_state`**: ação que o estado do registro não permite (editar
   pago, cancelar pago). Fica no Domain para vendas (confirmar o que já foi confirmado) usar o mesmo código.

## Consequências

- Vendas (Fase 6) criam lançamentos `Receivable` com `sale_id` pelo mesmo `FinancialEntry.CreateInstallments`
  e cancelam os abertos ao cancelar a venda.
- Baixa parcial não existe: o valor pago pode ser diferente, mas a baixa é única.
