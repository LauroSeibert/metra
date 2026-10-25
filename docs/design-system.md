# Design system

**Referência visual: `docs/design/listagem.png`** (listagem de produtos). Identidade sóbria: fundo cinza quase
neutro, cartões brancos planos com borda fina, azul só onde importa (ação principal, item ativo,
coluna ordenada, filtro escolhido) e números em fonte monoespaçada.

Toda cor, raio, espaçamento e sombra vem de `frontend/src/app/styles/tokens.scss`; o tema do Angular Material
(`styles/theme.scss`) aponta para os mesmos tokens. O tema escuro redefine só as cores (`[data-theme='dark']`).

## Tokens

| Grupo | Tokens | Uso |
| --- | --- | --- |
| Marca | `--erp-primary` (#0f6e5c), `--erp-primary-soft`, `--erp-primary-softer`, `--erp-on-primary` | ação principal, item ativo do menu, célula/filtro escolhido, coluna ordenada |
| Superfícies | `--erp-bg`, `--erp-surface`, `--erp-surface-muted`, `--erp-border`, `--erp-brand-mark` | fundo, cartões, cabeçalho de tabela, quadrado da marca |
| Texto | `--erp-text`, `--erp-text-muted`, `--erp-text-subtle` | valores; rótulos e linha secundária; desativado |
| Estados | `--erp-success`, `--erp-danger`, `--erp-warning(-soft/-text)`, `--erp-info(-soft/-text)` | ponto de ativo, "zerado", "baixo", selo de ambiente |
| Tipografia | `--erp-font-family` (Inter), `--erp-font-mono` (JetBrains Mono), `--erp-font-size-*` | texto; números (indicadores, preços, SKU, quantidades, contagens) |
| Espaço | `--erp-space-1` a `--erp-space-8` (4 a 64 px) | margens e espaçamentos |
| Forma | `--erp-radius-sm` (6), `--erp-radius-md` (8), `--erp-radius-card` (10), `--erp-shadow-card` (quase nula) | botões, campos, cartões |
| Estrutura | `--erp-topbar-height` (56), `--erp-sidenav-width-expanded` (232), `--erp-row-height` (46), `--erp-control-height` | moldura e tabela |

Falta um valor? Crie o token primeiro. Nenhum componente usa cor, raio ou espaçamento fixo.

## Moldura

- **Menu lateral na altura toda**: marca no topo (quadrado escuro com "E", nome e subtítulo), "Início" sem título
  de grupo, grupos em caixa alta (Cadastros, Operação, Financeiro), item ativo com fundo azul suave, módulos
  futuros com "Em breve". No rodapé, separados por uma linha: Configurações e Recolher.
- **Barra superior branca** só sobre o conteúdo: trilha com o grupo do menu ("Cadastros › Produtos"), busca global
  "Buscar ou ir para…" (Ctrl K), selo "Ambiente de demonstração", ajuda, notificações e avatar com nome e perfil.
- Dicas do layout usam `title` nativo.

## Componentes (`frontend/src/app/shared/ui/`)

| Componente | Para quê |
| --- | --- |
| `AppShell` | moldura: menu lateral na altura toda, barra superior e conteúdo |
| `PageHeader` | título, subtítulo e ações à direita |
| `KpiStrip` | faixa de indicadores num cartão só, dividido em células; células `selectable` são filtro (a escolhida fica azul suave) |
| `DataTable` | tabela em cartão com os filtros dentro (slot `[tableToolbar]`), ordenação e paginação no servidor, linha expansível, ações no hover, `rowMuted`, rodapé "Mostrando a–b de N" com Anterior/Próxima, estados carregando/vazio/erro, `(rowActivate)` no duplo clique |
| `Card` | cartão branco com borda fina |
| `MoneyInput` | campo monetário (`<erp-money-input [formField]>`) que preenche pelos centavos: 1, 0, 0 → R$ 1,00 |
| `StatusChip` | etiqueta discreta com tom `primary`, `success`, `danger`, `info`, `warning` ou `neutral` |
| `SegmentedControl` | filtro rápido com várias opções (ex.: situação de lançamento) |
| `ConfirmDialog` | confirmação de ação destrutiva |
| `SlashFocus` | diretiva: "/" foca a busca da listagem |

Classes globais (`src/styles.scss`):

| Classe | Uso |
| --- | --- |
| `erp-primary-action`, `erp-secondary-action`, `erp-danger-action` | botões (cantos de 6 px, 38 px de altura) |
| `erp-toolbar` | barra de filtros dentro do `DataTable` (campos de 40 px); `.grow` na busca; `erp-count` na contagem, à direita |
| `erp-filter-label` | rótulo embutido no select (`<span matTextPrefix>`): "Categoria  Todas" |
| `erp-kbd` | tecla de atalho ("/", "Ctrl K") |
| `erp-dot` (`.off`, `.warning`, `.danger`) | situação como ponto: cheio = ativo; vazado + "Inativo" |
| `erp-mono`, `erp-number` | números em monoespaçada |
| `erp-row-actions` + `erp-row-action` (`.danger`, `.success`) | ações da linha: ícones discretos, cor do tom no hover |
| `erp-sr-only` | texto só para leitor de tela |

Ícones de ação na linha: `edit_square` (editar; `visibility` para o perfil Leitura), `pause_circle`/`play_circle`
(inativar/ativar), `delete` (excluir), `history`, `swap_vert`, `payments`, `undo`.

## Padrões de tela

- **Listagem** (referência: `features/products/product-list`):
  1. `PageHeader` com ações secundárias em contorno e a principal preenchida em azul.
  2. `KpiStrip` com as contagens; as que servem de filtro (Todos/Ativos/Inativos) são `selectable`.
  3. `DataTable` com `<section tableToolbar class="erp-toolbar">`: busca com `erpSlashFocus` e `<span matSuffix
     class="erp-kbd">/</span>`, selects com `erp-filter-label` (opção "Todas" com valor sentinela, não `null`,
     senão o select aparece vazio), "Limpar filtros" e a contagem em `erp-count`.
  4. Colunas: nome com a linha secundária embaixo (categoria, tipo); código em mono; valores à direita (as células
     `align: 'end'` já saem em mono); estoque com "baixo"/"zerado" antes do número; situação em `erp-dot`;
     registro inativo com `rowMuted`; ações no hover.
- **Cadastro**: `PageHeader` com "Voltar" → seções em `Card` com grade de 3 colunas → ações no rodapé à direita.
  Erros por campo em `mat-error`; erro geral num banner âmbar no topo. Rotas `<modulo>/edit` (novo) e
  `<modulo>/edit/:id`; ao salvar a tela continua aberta (cadastro novo passa para `edit/:id`).
- **Resumo + tabela** (financeiro): `KpiStrip` com `format: 'currency'` (informativa) e a tabela abaixo.
- **Histórico longo** (paginação por chave): `DataTable` sem total e botão "Carregar mais" abaixo; no fim,
  "Fim do histórico.". Referência: `features/stock/movement-history`.
- **Escolher parceiro / produto**: `<erp-partner-picker [type] [(selected)]>`, `<erp-product-picker [(selected)]>`.
- **Estados**: carregando (barra + linhas fantasma no formato da tabela, só depois de 250 ms), vazio (ícone +
  mensagem) e erro (mensagem + "Tentar de novo").
- **Telas**: desktop primeiro; abaixo de 720 px a tabela vira lista de cartões.

## Bundle

Orçamento folgado (1 MB de aviso). Tokens do Material de formulário entram por `provideMaterialFormDefaults()`
na rota lazy de cada funcionalidade: no `app.config` eles arrastam ~250 kB.
