# ADR 0004: Estoque, saldo e paginação por chave

- **Data:** 2026-10-24
- **Status:** aceito (decisões tomadas pela IA seguindo o plano, para revisão)

## Contexto

A Fase 4 pede movimentações imutáveis, saldo transacional, saldo negativo conforme configuração e
paginação por chave, com a invariante "soma das movimentações = saldo" testada e o histórico de 1 milhão
de linhas dentro da meta. Nenhuma dependência nova.

## Decisões

1. **Um depósito.** Recomendação do plano. `stock_balance` tem uma linha por produto; vários depósitos
   pediriam `warehouse_id` no saldo e no movimento.
2. **Concorrência otimista com nova tentativa.** O saldo usa a coluna `version` como os outros registros.
   Duas movimentações simultâneas do mesmo produto: a segunda recebe conflito de versão, descarta o que
   montou e refaz tudo (até 3 vezes), relendo o saldo. Alternativa descartada: `UPDATE ... SET quantity =
   quantity + x` atômico, que dispensa a versão mas espalharia a regra de saldo negativo pelo SQL.
3. **Ajuste por contagem.** O usuário informa o saldo contado e o sistema grava a diferença: é como o
   inventário acontece na prática e evita o usuário calcular o sinal. Exige motivo.
4. **Estorno referencia o original.** Sem coluna "estornado" no original (ele é imutável): a existência de
   um estorno é conferida pelo índice `(reference_type, reference_id)`, dentro da transação.
5. **Saldo nasce com o produto.** `ProductService.CreateAsync` grava o saldo zero; a migration `AddStock`
   criou o saldo dos produtos que já existiam. O movimento ainda cria o saldo se faltar (defensivo).
6. **Configurações em `app_setting`.** Tabela chave/valor do plano, criada agora porque o saldo negativo
   depende dela. `SettingsReader` lê com padrão seguro (ausente ou inválido → `false`). A tela e as demais
   chaves vêm na Fase 8.
7. **Paginação por chave sem total.** `{ items, nextCursor }`, cursor opaco (data + id em base64url).
   A tela usa "Carregar mais" em vez de paginador numerado.
8. **Seed com SQL parametrizado em lote.** 1 milhão de linhas pelo EF levaria ~20 min; com INSERT de 1.000
   linhas por comando, todos os valores em parâmetros, leva menos de 1 min. Fica restrito ao `Erp.Seed`.
9. **Nomes de usuário na Application.** `IErpDbContext.UserNames` expõe só id e nome do `app_user`, para o
   histórico mostrar quem lançou sem a Application conhecer o Identity. É uma classe com `init` (não um
   record posicional) porque o EF não traduz filtro sobre record construído por construtor.

## Consequências

- Toda movimentação futura (vendas, Fase 6) passa pelo mesmo caminho de saldo e invariante.
- O total de "abaixo do mínimo" custa ~130 ms com 100 mil produtos; ver `docs/modules/stock.md`.
