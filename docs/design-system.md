# Design system

O visual segue o protótipo do Painel do Parceiro (ver `docs/plan.md`, Design e identidade visual).
Toda cor, raio, espaçamento e sombra vem de `frontend/src/app/styles/tokens.scss`; o tema do Angular Material
(`styles/theme.scss`) aponta para os mesmos tokens.

## Tokens

| Grupo | Tokens | Uso |
| --- | --- | --- |
| Marca | `--erp-primary`, `--erp-primary-soft`, `--erp-on-primary` | barra superior, filtro ativo, links |
| Superfícies | `--erp-bg`, `--erp-surface`, `--erp-surface-muted`, `--erp-border` | fundo, cartões, cabeçalho de tabela |
| Texto | `--erp-text`, `--erp-text-muted` | títulos e valores; rótulos e subtítulos |
| Estados | `--erp-success(-soft)`, `--erp-danger(-soft)`, `--erp-info(-soft/-text)`, `--erp-warning(-soft/-text)` | ação principal, saídas/erros, etiquetas |
| Tipografia | `--erp-font-*`, `--erp-letter-spacing-caps`, `--erp-line-height-body` | Inter; caixa alta nos cabeçalhos |
| Espaço | `--erp-space-1` a `--erp-space-7` (4, 8, 12, 16, 24, 32, 48 px) | margens e espaçamentos |
| Forma | `--erp-radius-sm`, `--erp-radius-card` (16 px), `--erp-radius-pill`, `--erp-shadow-card` | cartões, botões, etiquetas |
| Estrutura | `--erp-topbar-height`, `--erp-sidenav-width`, `--erp-nav-item-size`, `--erp-content-max-width` | moldura da página |

Falta um valor? Crie o token primeiro. Nenhum componente usa cor, raio ou espaçamento fixo.

## Componentes (`frontend/src/app/shared/ui/`)

| Componente | Para quê |
| --- | --- |
| `AppShell` | moldura: barra superior, menu lateral de ícones, conteúdo |
| `PageHeader` | título, subtítulo e ações à direita |
| `Card` | cartão branco arredondado |
| `DataTable` | tabela em cartão: ordenação e paginação no servidor, linha expansível, estados carregando/vazio/erro; `(rowActivate)` no duplo clique da linha |
| `MoneyInput` | campo monetário (`<erp-money-input [formField]>` dentro de `mat-form-field`) que preenche pelos centavos: 1, 0, 0 → R$ 1,00 |
| `StatusChip` | etiqueta em pílula com tom `primary`, `success`, `danger`, `info`, `warning` ou `neutral` |
| `SegmentedControl` | filtro rápido com a opção ativa em azul |
| `ConfirmDialog` | confirmação de ação destrutiva |

Classes globais (`src/styles.scss`): `erp-primary-action` (botão verde da ação principal), `erp-danger-action`,
`erp-list-search`, `erp-number` e `erp-row-actions` + `erp-row-action` (botões de ação da linha; tons `danger` e
`success` no hover).

Ícones de ação na linha: `edit_square` (editar), `pause_circle`/`play_circle` (inativar/ativar), `delete` (excluir).

## Padrões de tela

- **Listagem**: `PageHeader` com ação principal verde → filtros (busca, selects, `SegmentedControl`) → `DataTable`.
  Valores monetários alinhados à direita; status em `StatusChip`; detalhes na linha expandida; duplo clique na
  linha abre a edição.
- **Cadastro**: `PageHeader` com "Voltar" → seções em `Card` com grade de 3 colunas → ações no rodapé à direita.
  Erros por campo em `mat-error`; erro geral num banner âmbar no topo. Rotas `<modulo>/edit` (novo) e
  `<modulo>/edit/:id`; ao salvar a tela continua aberta (cadastro novo passa para `edit/:id`).
- **Histórico longo** (paginação por chave): `DataTable` sem paginador e botão "Carregar mais" abaixo, que acrescenta
  a página seguinte; no fim, "Fim do histórico.". Referência: `features/stock/movement-history`.
- **Resumo + tabela** (financeiro): `erp-kpi-grid` com `<erp-kpi-card format="currency">` acima dos filtros e da tabela.
- **Escolher parceiro**: `<erp-partner-picker [type] [(selected)]>` (`features/partners/partner-picker`).
- **Escolher produto**: `<erp-product-picker [(selected)]>` (`features/products/product-picker`), busca por SKU ou nome.
- **Estados**: toda tela trata carregando (barra + linhas fantasma), vazio (ícone + mensagem) e erro (mensagem + "Tentar de novo").
  O carregando só aparece se passar de `LOADING_DELAY_MS` (250 ms, `delayedFlag`); antes disso as linhas
  fantasma ocupam o espaço invisíveis, para a tela não piscar com respostas rápidas.
- **Telas**: desktop primeiro; telas estreitas ficam fora do escopo inicial.

## Bundle

O orçamento do build (`angular.json`) é 550 kB de aviso e 650 kB de erro para o carregamento inicial.
Tokens do Material de formulário e paginador entram por `provideMaterialFormDefaults()` na rota lazy de cada
funcionalidade: no `app.config` eles arrastam ~250 kB para o bundle inicial.
