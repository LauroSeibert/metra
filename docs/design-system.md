# Design system

**Referência visual: a listagem de produtos** (`features/products/product-list`). Identidade sóbria: fundo quase neutro,
cartões planos com borda fina, cor da marca (verde-azulado) só onde importa (ação principal, item ativo, filtro
escolhido) e números em fonte monoespaçada. Há tema claro e escuro.

## Stack

- **Spartan UI** (Brain + Helm): componentes em `frontend/src/app/shared/ui/helm/<componente>/` (o código é nosso;
  edite à vontade). **Tailwind v4** (com preflight) para o estilo. **Lucide** para ícones, pelo `erp-icon`.
- **Tokens**: `frontend/src/app/styles/tokens.scss` é a fonte única de cor, raio, espaçamento, sombra e tipografia
  (`--erp-*`). O tema escuro só redefine cores (`[data-theme='dark']`).
- **Ponte de tokens**: `frontend/src/tailwind.css` aponta as variáveis do Spartan (`--background`, `--primary`,
  `--border`...) para os tokens `--erp-*` e cria utilitários com os tamanhos do projeto. Não existe paleta paralela.
- O Angular Material foi removido. O CDK fica (o Spartan Brain usa).

### Como estilizar

1. Use utilitários do Tailwind e as cores semânticas (`bg-card`, `text-muted-foreground`, `border-border`,
   `text-destructive`, `bg-accent-strong`, `text-success`, `bg-warning-soft`, `text-warning-text`, `bg-danger-soft`).
2. Tamanhos do projeto como utilitários: `h-control` / `h-control-sm` (campos), `h-button`, `h-thead`, `max-w-page`
   (largura dos formulários), `text-label`, `text-caption`, `text-title` + `tracking-title`.
3. Nada de cor, raio, sombra ou espaçamento fixo em componente (`#hex`, `rgb()`, `13px`). Falta um valor? Crie o
   token em `tokens.scss` e, se for usado em classe, o utilitário em `tailwind.css`.
4. Classe de componente própria não pode ter o nome de um utilitário do Tailwind. CSS de componente (`*.scss`) só
   para o que o Tailwind não resolve (grade responsiva com `position: sticky`, por exemplo).

## Tokens

| Grupo | Tokens | Uso |
| --- | --- | --- |
| Marca | `--erp-primary` (#0f6e5c), `--erp-primary-soft`, `--erp-primary-softer`, `--erp-on-primary` | ação principal, item ativo do menu, filtro escolhido |
| Superfícies | `--erp-bg`, `--erp-surface`, `--erp-surface-muted`, `--erp-surface-hover`, `--erp-border`, `--erp-border-strong` | fundo, cartões, cabeçalho de tabela, bordas |
| Texto | `--erp-text`, `--erp-text-muted`, `--erp-text-subtle` | valores; rótulos e linha secundária; desativado |
| Estados | `--erp-success`, `--erp-danger`, `--erp-warning(-soft/-text)`, `--erp-info(-soft/-text)` | situação, erro, aviso |
| Tipografia | `--erp-font-family` (Inter), `--erp-font-mono` (JetBrains Mono), `--erp-font-size-*` | texto; números, SKU, códigos |
| Espaço | `--erp-space-1` a `--erp-space-8` | margens e espaçamentos |
| Forma | `--erp-radius-*`, `--erp-shadow-card` | botões, campos, cartões |
| Estrutura | `--erp-topbar-height`, `--erp-sidenav-width-expanded`, `--erp-control-height`, `--erp-form-max-width` | moldura, campos, formulários |

## Moldura

- **Menu lateral na altura toda**: marca no topo, "Início", grupos em caixa alta (Cadastros, Operação,
  Financeiro), item ativo com fundo suave. No rodapé: Configurações e Recolher.
- **Barra superior** só sobre o conteúdo: trilha ("Cadastros › Produtos"), busca global "Buscar ou ir para…"
  (Ctrl K), selo "Ambiente de demonstração", tema, notificações e o menu do perfil.
- Dicas do layout usam `title` nativo; no resto, `hlmTooltip`.

## Componentes

**Spartan (Helm)**, em `shared/ui/helm/`: button, badge, input, textarea, label, field, card, switch, separator,
avatar, skeleton, empty, dialog, dropdown-menu, command, sonner, popover, select, combobox, input-group, tooltip.
Para adicionar um: `npx ng g @spartan-ng/cli:ui <nome>` e depois trocar os `@ng-icons` do arquivo gerado por
`erp-icon` e rodar `npm uninstall @ng-icons/core @ng-icons/lucide` (o CLI reinstala esses pacotes).

**Do projeto**, em `shared/ui/`:

| Componente | Para quê |
| --- | --- |
| `AppShell` | moldura: menu, barra superior e conteúdo |
| `Icon` (`erp-icon`) | ícone Lucide por nome; registre o ícone novo em `icon/icons.ts` |
| `PageHeader` | título, subtítulo e ações à direita (listagens e telas de configuração) |
| `KpiStrip` | faixa de indicadores num cartão; células `selectable` filtram a lista |
| `DataTable` | tabela em cartão com filtros no slot `[tableToolbar]`, ordenação e paginação no servidor, linha expansível, ações, estados carregando/vazio/erro |
| `SearchField` | busca com lupa, atalho `/` e botão de limpar |
| `FilterSelect` | select de filtro com rótulo embutido ("Categoria  Todas") |
| `DateField`, `SegmentedControl`, `ResultCount` | data nativa, filtro rápido em pílula, contador de resultados |
| `SectionCard` | cartão de seção do formulário (ícone, título, descrição opcional) |
| `FormHeader` | cabeçalho de cadastro: voltar, rótulo, título com selos (`[badges]`) e ações |
| `ActionBar` | "Alterações não salvas" / "Tudo salvo" (só depois de uma edição) e as ações |
| `FormBanner`, `LoadError` | erro geral do formulário (com "Recarregar dados") e falha ao carregar |
| `FieldShell` (+ `fieldError`) | rótulo, dica e mensagem de erro de um campo |
| `MoneyInput` | campo monetário que preenche pelos centavos (1, 0, 0 → R$ 1,00) |
| `SearchPicker` | combobox com busca no servidor; `PartnerPicker` e `ProductPicker` são wrappers |
| `StatusChip` | etiqueta com tom `primary`, `success`, `danger`, `info`, `warning` ou `neutral` |
| `ConfirmService` | `ask({ title, message, confirmLabel, cancelLabel })` devolve `Promise<boolean>` |
| `MarginMeter` | barra de margem do produto |
| `SlashFocus` | diretiva: "/" foca a busca |

Utilitários globais (`src/styles.scss`): `erp-mono` (monoespaçada), `erp-kbd` (tecla de atalho),
`erp-sr-only` (só leitor de tela), `erp-skeleton`.

**Diálogos**: `HlmDialogService.open<Resultado, Contexto>(Componente, { context, contentClass: 'sm:max-w-md' })` e
`firstValueFrom(ref.closed$)`. Dentro do diálogo: `inject(BrnDialogRef)` e `injectBrnDialogContext<T>()`.
Esc cancela e Enter envia.

## Padrões de tela

- **Listagem** (referência: `features/products/product-list`):
  1. `PageHeader` com ações secundárias em contorno e a principal preenchida.
  2. `KpiStrip` com as contagens; as que servem de filtro são `selectable`.
  3. `DataTable` com `<section tableToolbar>`: `erp-search-field`, `erp-filter-select`, "Limpar filtros" (sempre
     visível, desabilitado sem filtro) e o contador à direita.
  4. Colunas: nome com a linha secundária embaixo; código em mono; valores à direita; situação em badge; registro
     inativo com `rowMuted`; ações em ícone.
- **Cadastro** (referência: `features/products/product-form`): coluna estreita (`mx-auto max-w-page`),
  `FormHeader` com `ActionBar` e Salvar, seções em `SectionCard`, resumo em coluna lateral quando houver. Erros por
  campo no `FieldShell`; erro geral em `FormBanner`. Rotas `<modulo>/edit` (novo) e `<modulo>/edit/:id`; ao salvar,
  a tela continua aberta (o cadastro novo passa para `edit/:id`).
- **Configurações e perfil**: `PageHeader` com Salvar à direita (`<button form="id-do-form">`), cartões de mesma
  altura lado a lado.
- **Resumo + tabela** (financeiro): `KpiStrip` com `format: 'currency'` e a tabela abaixo.
- **Histórico longo** (paginação por chave): `DataTable` sem total e botão "Carregar mais".
  Referência: `features/stock/movement-history`.
- **Estados**: carregando (esqueleto, só depois de 250 ms), vazio (ícone + mensagem) e erro (mensagem + "Tentar de
  novo").
- **Telas**: desktop primeiro; abaixo de 720 px a tabela vira lista de cartões.
- **Grade responsiva em formulário**: `@container` num elemento externo e a grade (`grid`) num interno; o
  `@container` não vale para o próprio elemento.

## Bundle

Orçamento de 1 MB de aviso e 2 MB de erro no chunk inicial. Hoje o inicial fica perto de 950 kB.
