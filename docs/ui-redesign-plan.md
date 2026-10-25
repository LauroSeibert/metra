# Plano de Redesign da UI: Angular Material → Spartan + Tailwind v4

Oct 25, 2026 · @Lauro

## Visão geral

Trocar a camada visual do front (`frontend/`) por Spartan UI (estilo shadcn), Tailwind v4 e ícones Lucide, mantendo os tokens `--erp-*` como única fonte de verdade. Sem mudança de regra de negócio, rota, API ou modelo de dados.

Este plano substitui a decisão "Angular Material + CDK" do `docs/plan.md`. O CDK continua; o Material sai na última fase.

Referência visual aprovada: `docs/design/prototype/erp-prototype.html` (abrir no navegador). É referência, não código para copiar.

## Regras para a IA

- Uma fase por vez. Ao terminar, rodar `npm run build` em `frontend/` (e `npm run lint` se existir), relatar o resultado real e esperar confirmação.
- Branch `feat/ui-redesign`, um grupo de commits por fase.
- Sem testes novos, benchmarks ou ADR (mesmo escopo simplificado do `docs/plan.md`).
- Dependências aprovadas: `tailwindcss`, `@tailwindcss/postcss`, `postcss`, `@spartan-ng/cli`, `@spartan-ng/brain`, Lucide. Qualquer outra: perguntar.
- Os nomes de arquivos, tokens e componentes deste plano vêm da descrição do projeto. Conferir no repositório e avisar sobre diferenças.
- Nenhuma cor, raio, sombra ou espaçamento fixo nos componentes. Valor novo: criar o token em `frontend/src/app/styles/tokens.scss` (com valor dark) e só então usar. Proibido valor arbitrário do Tailwind, como `bg-[#0f6e5c]`.
- Não manter paleta paralela: as variáveis do Spartan apontam para `--erp-*`.
- Não alterar lógica de formulários, chamadas à API nem rotas. Só template e estilo.
- Interface em pt-BR e sentence case (sem caixa alta em rótulos); código em inglês.
- O app compila e funciona ao fim de cada fase. Material e Spartan convivem até a fase 9.
- Parar e perguntar: componente Spartan que não cobre o caso, versão maior de pacote, ou conflito entre este plano e o código.

## Arquitetura alvo

| Camada | O que é | Onde |
| --- | --- | --- |
| Tokens | `--erp-*`, claro e `[data-theme='dark']` | `frontend/src/app/styles/tokens.scss` |
| Ponte | Variáveis do Spartan apontando para `--erp-*` e `@theme inline` do Tailwind | CSS global do Tailwind (fase 1) |
| Spartan Brain | Comportamento e acessibilidade | npm `@spartan-ng/brain` |
| Spartan Helm | Estilos copiados e editáveis | `frontend/src/app/shared/ui/helm/` (usar o caminho que o CLI configurar) |
| Wrappers | AppShell, PageHeader, KpiStrip/KpiCard, DataTable, StatusChip, MoneyInput; novos: FieldShell, ActionBar, MarginMeter | `frontend/src/app/shared/ui/` |
| Telas | Importam de `shared/ui/` (primitivos simples como button e badge podem vir direto do Helm) | `frontend/src/app/features/` |

## Especificação visual

| Elemento | Especificação |
| --- | --- |
| Fontes | Inter; JetBrains Mono em SKU, e-mail e códigos; `tabular-nums` em preços e quantidades |
| Título da página | 26px, peso 600, letter-spacing -0.03em; subtítulo 14px cinza |
| Input e select | Altura 38px (pequeno 32px), borda 1px `--erp-border-strong`, raio `md`. Rótulo acima (13px, peso 500). Sem label flutuante e sem espaço reservado abaixo: a linha de dica ou erro só existe quando há texto |
| Foco e erro | Foco: borda primária + anel de 3px a cerca de 28% de opacidade. Erro: borda `--erp-danger` e mensagem abaixo em 12.5px; asterisco de obrigatório em vermelho |
| Botões | Altura 36px (pequeno 32px), raio `md`, peso 500. Variantes: primário (teal), outline, ghost, destrutivo. Só ícone: 32px |
| Switch | Trilho 38x22. Linha com borda: título, texto de apoio e o switch à direita |
| Card | Borda 1px, raio `lg`, sombra `xs`. Cabeçalho de seção: bloco de ícone 36px (fundo accent), título 600, descrição 13px cinza, borda inferior. Corpo com padding 20px e gap 16px |
| Tabela | Cabeçalho 40px, fundo muted, 12.5px cinza, sentence case. Linhas 52px com hover. Números à direita; SKU em mono cinza |
| Badge | Pílula 22px com ponto de 6px. Variantes: ok, off (inativo), warn, danger, outline, code |
| Estoque baixo | Abaixo do mínimo: ponto e número em âmbar |
| Ações da linha | Botão de editar + menu "⋯" (editar, duplicar, ativar/desativar) |
| Estado vazio | Ícone, título, uma frase e uma ação |
| Sidebar | 240px (64px recolhida), itens 34px. Ativo: fundo accent + barra de 3px à esquerda. Rótulos de grupo em sentence case |
| Barra superior | 56px: breadcrumb, botão de comando (`Ctrl K`), badge de ambiente, notificações, tema, usuário |
| Página | Padding 28px 32px, largura máxima 1280px |
| KPIs | Cards clicáveis que filtram a lista; selecionado: fundo accent + linha de 2px primária embaixo |
| Medidor de margem | (preço − custo) / preço. ≥ 40% ok, ≥ 20% warn, abaixo disso danger. Mostra percentual, barra e rótulo (Saudável, Atenção, Baixa) |
| Dialog e toast | Dialog com blur de 4px e raio `xl`, ações à direita. Toast no canto inferior direito após salvar, duplicar e excluir |
| Barra de ações | Sticky no fim dos formulários; mostra "Alterações não salvas" quando há mudança; Cancelar (ghost) e Salvar (primário) |
| Ícones | Lucide, traço 1.75; 16px em tabelas e formulários, 18px na navegação |

## Tokens

Conferir os nomes gerados por `ng g @spartan-ng/cli:ui-theme` e os `--erp-*` reais antes de mapear.

| Spartan | Aponta para |
| --- | --- |
| `--background` | `--erp-bg` |
| `--foreground` | `--erp-text` |
| `--card`, `--popover` | `--erp-surface` |
| `--primary` / `--primary-foreground` | `--erp-primary` (teal `#0f6e5c`; no dark, um teal mais claro) / `--erp-on-primary` |
| `--muted` / `--muted-foreground` | `--erp-surface-muted` / `--erp-text-muted` |
| `--accent` | `--erp-surface-hover` |
| `--border` / `--input` | `--erp-border` / `--erp-border-strong` |
| `--ring` | `--erp-primary` |
| `--destructive` | `--erp-danger` |
| `--radius` | `--erp-radius-md` |

**Tokens novos, se faltarem** (sempre com valor dark):

- Status: `--erp-success`, `--erp-success-soft`, `--erp-warning`, `--erp-warning-soft`, `--erp-danger-soft`.
- Tamanhos: `--erp-control-h` 38px, `--erp-control-h-sm` 32px, `--erp-button-h` 36px, `--erp-row-h` 52px, `--erp-thead-h` 40px, `--erp-topbar-h` 56px, `--erp-sidebar-w` 240px, `--erp-sidebar-w-collapsed` 64px.
- Raios `--erp-radius-sm/md/lg/xl` (cerca de 6, 8, 12 e 16px), sombras `--erp-shadow-xs` e `--erp-shadow-overlay`, e `--erp-focus-ring`.

**Ponte Tailwind v4** (ponto de partida; ajustar ao que o CLI gerar):

```css
@import "tailwindcss";
@import "@spartan-ng/brain/hlm-tailwind-preset.css"; /* confirmar o import na doc do Spartan */

:root {
  --background: var(--erp-bg);
  --foreground: var(--erp-text);
  --card: var(--erp-surface);
  --popover: var(--erp-surface);
  --primary: var(--erp-primary);
  --primary-foreground: var(--erp-on-primary);
  --muted: var(--erp-surface-muted);
  --muted-foreground: var(--erp-text-muted);
  --accent: var(--erp-surface-hover);
  --border: var(--erp-border);
  --input: var(--erp-border-strong);
  --ring: var(--erp-primary);
  --destructive: var(--erp-danger);
  --radius: var(--erp-radius-md);
}

@theme inline {
  --color-background: var(--background);
  --color-foreground: var(--foreground);
  --color-card: var(--card);
  --color-primary: var(--primary);
  --color-primary-foreground: var(--primary-foreground);
  --color-muted: var(--muted);
  --color-muted-foreground: var(--muted-foreground);
  --color-accent: var(--accent);
  --color-border: var(--border);
  --color-input: var(--input);
  --color-ring: var(--ring);
  --color-destructive: var(--destructive);
  --color-success: var(--erp-success);
  --color-warning: var(--erp-warning);
  --font-sans: "Inter", ui-sans-serif, system-ui, sans-serif;
  --font-mono: "JetBrains Mono", ui-monospace, monospace;
}

/* só se algum Helm ainda usar dark: */
@custom-variant dark (&:where([data-theme='dark'], [data-theme='dark'] *));
```

## Fases de implementação

Cada fase só termina quando passa no critério de *Saída*.

**0. Auditoria (somente leitura).**
- Rodar `git status`. Há mudanças não commitadas em `tokens.scss`, `styles.scss` e `docs/design-system.md`: pedir ao Lauro para commitar, depois criar a branch.
- Relatar versões (Node, Angular, Material, CDK, TypeScript). O Angular deve ser 21 ou 22, que o Spartan aceita.
- Listar os `--erp-*` existentes e quais tokens novos faltam.
- Listar usos de Material: `grep -rE "@angular/material|mat-|MatFormField|MatSelect|MatDialog|MatSnackBar|MatMenu|MatDatepicker|MatSlideToggle|mat-icon" frontend/src`.
- Listar telas, rotas e componentes de `shared/ui/`, e diferenças entre `docs/design-system.md` e o SCSS real.
- *Saída:* relatório no chat; Lauro aprova.

**1. Base: Tailwind, Spartan e ponte de tokens (sem mudança visual).**
- Instalar Tailwind v4 pelo guia oficial do Angular. Evitar que o preflight quebre o Material enquanto convivem (usar `@layer` ou escopo, com comentário explicando).
- `npm i -D @spartan-ng/cli`, `ng g @spartan-ng/cli:init`, `ng g @spartan-ng/cli:ui-theme`.
- Criar os tokens que faltam e a ponte da seção *Tokens*.
- *Saída:* build passa e as telas estão idênticas às de antes, em claro e dark. Um elemento de teste com `bg-primary text-primary-foreground` mostra o teal e muda no dark (remover depois).

**2. Primitivos.**
- Instalar Lucide e criar um wrapper `erp-icon` (16px, traço 1.75).
- Adicionar pelo CLI: `button`, `badge`, `input`, `label`, `field` (se existir), `card`, `switch`, `separator`, `avatar`, `skeleton`, `empty`. Ajustar o Helm à *Especificação visual*.
- Criar `FieldShell` (rótulo, controle, dica, erro, `aria-describedby`). Refazer `StatusChip` como badge e `MoneyInput` no novo visual, mantendo API e máscara.
- Manter `erp-primary-action`, `erp-secondary-action` e `erp-danger-action` como aliases dos botões novos.
- Criar a rota temporária `/design-lab` (só dev) com todos os primitivos e estados.
- *Saída:* `/design-lab` bate com o protótipo e o foco é visível em todo controle.

**3. Piloto: Configurações.** Se ficar ruim, parar e relatar antes de seguir.
- Cards Empresa e Regras com cabeçalho de ícone, CNPJ com máscara, desconto com sufixo `%`, linha de switch para estoque negativo, tabela de usuários (avatar, e-mail em mono, badges).
- Manter formulário, validação e chamadas atuais.
- *Saída:* nenhum campo do Material na tela; salva como antes.

**4. Shell e overlays.**
- `AppShell` e `PageHeader` no novo visual (recolher e tema como hoje).
- Adicionar `dialog`, `dropdown-menu`, `command` e toast (`sonner`, se existir). Criar `ToastService.success(title, description?)`; sem toast no Spartan, encapsular o snackbar atual.
- Paleta de comandos em `Ctrl/Cmd + K`: ir para telas, "Novo produto", alternar tema.
- *Saída:* navegação, recolher, tema e paleta funcionam pelo teclado.

**5. Produtos (lista).**
- KPIs clicáveis, filtros (busca com atalho `/`, categoria em combobox, estoque), contador, tabela e menu "⋯" com os endpoints atuais.
- **Manter paginação, ordenação e filtro no servidor.** Não carregar tudo no front.
- Estado vazio ("Limpar filtros"), skeleton e rodapé "Mostrando X–Y de Z".
- *Saída:* mesmo comportamento de hoje, visual do protótipo; a tabela rola na horizontal dentro do card em tela estreita.

**6. Produto (editar e novo).**
- Cards Identificação e Preços e estoque, com combobox de categoria e unidade e `MoneyInput`.
- Card de resumo fixo com `MarginMeter` atualizando enquanto digita; `ActionBar` sticky com "Alterações não salvas"; erros inline com foco no primeiro campo inválido; toast ao salvar. Uma coluna abaixo de 1100px.
- *Saída:* validação, salvar e cancelar iguais aos de hoje.

**7. Unidades de medida.**
- Tabela com badge `code`, contagem de produtos e ações (excluir desativado, com dica, quando a unidade está em uso).
- Dialog de criar e editar com erro inline; Enter salva, Esc cancela.
- *Saída:* dialog com foco preso e navegável por teclado.

**8. Demais telas.**
- Aplicar os mesmos padrões nas telas restantes, reaproveitando `shared/ui/`; componente novo só quando um padrão se repetir.
- Trocar os ícones restantes por Lucide. Datepicker: usar o do Spartan se atender; senão manter o do Material como exceção registrada.
- *Saída:* nenhuma tela fora das exceções importa de `@angular/material`.

**9. Limpeza e documentação.**
- Remover o que sobrou do Material (módulos, tema, fonte de ícones, pacote se não houver exceção), a rota `/design-lab`, os aliases `erp-*-action` e o elemento de teste da fase 1.
- Conferir que não há valores fixos: `grep -rE "#[0-9a-fA-F]{3,8}\b" frontend/src/app` fora de `tokens.scss` não pode achar nada relevante.
- Atualizar `docs/design-system.md` (stack nova, ponte de tokens, a *Especificação visual*, regra para criar componente ou token) e `frontend/CLAUDE.md`. Trocar `docs/design/listagem.png` por capturas novas.
- *Saída:* uma tela nova se monta só com `shared/ui/` e tokens, e o doc explica como.

## Decisões já tomadas

- Spartan + Tailwind v4 + Lucide; Material removido ao final; CDK fica.
- Tokens `--erp-*` são a fonte de verdade; dark mode continua em `[data-theme='dark']`.
- Cor primária continua teal `#0f6e5c`; fontes continuam Inter e JetBrains Mono.
- Dados e paginação continuam no servidor.

## Como começar

Salvar este arquivo como `docs/ui-redesign-plan.md` e o protótipo em `docs/design/prototype/erp-prototype.html`. Enviar ao Claude Code:

```text
Leia docs/ui-redesign-plan.md. Execute somente a Fase 0 (auditoria, sem alterar arquivos) e relate os achados. Espere minha aprovação antes da Fase 1.
```
