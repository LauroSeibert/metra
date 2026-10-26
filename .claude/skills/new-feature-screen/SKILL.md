---
name: new-feature-screen
description: Criar tela de listagem e de cadastro no front (Angular) seguindo o módulo de produtos, com DataTable, Signal Forms e cliente OpenAPI.
---

# Nova tela (padrão do módulo de produtos)

Referência viva: `frontend/src/app/features/products/`. Pré-requisito: endpoint pronto e `npm run api:generate` rodado.

## Estrutura da funcionalidade

```text
features/<modulo>/
├── <modulo>.routes.ts      # rotas lazy
├── <modulo>.strings.ts     # todos os textos pt-BR, inclusive mensagens de validação
├── <modulo>-rules.ts       # limites espelhados do domínio (.NET)
├── <x>-list/               # listagem
├── <x>-form/               # cadastro/edição (+ <x>-form.schema.ts)
└── testing/               # dados de exemplo; FakeApi vem de shared/testing/fake-api.ts
```

Registre a rota em `app.routes.ts` com `loadChildren` e `data: { breadcrumb }`, e libere o item em
`core/layout/nav-items.ts`.

## Listagem (copie `product-list/`)

- Filtros em signals iniciados pela URL (`shared/data/list-url-state.ts`) e gravados de volta num `effect`.
- Busca com `debounced(...)`; `page` como `linkedSignal` que volta a 1 quando um filtro muda.
- `resource({ params: query, loader: ({ params }) => api.invoke(listX, params) })`.
- `rows`/`total` com `linkedSignal` para manter a página anterior durante o carregamento.
- Perfil: `canWrite = inject(AuthService).canWrite`; sem ele, esconda criar/inativar e troque o ícone de
  editar por `visibility`. Itens por página padrão: `auth.pageSize()` (preferência do usuário).
- Visual: `docs/design/listagem.png` e `docs/design-system.md` (Padrões de tela > Listagem).
- `<erp-page-header>` com ações à direita (`<a hlmBtn>`; a principal sem `variant`, as demais `variant="outline"`) →
  `<erp-kpi-strip>` com as contagens (as que filtram são `selectable`) → `<erp-data-table>` com os filtros em
  `<section tableToolbar>` (`erp-search-field`, `erp-filter-select`, "Limpar filtros" sempre visível e desabilitado sem
  filtro, `erp-result-count`), `erpCell`/`erpExpandedRow` tipados por `[erpCellOf]="rows()"`, situação em badge e
  `[rowMuted]` para inativos.

## Cadastro (copie `product-form/`)

- Modelo e `schema(...)` em `<x>-form.schema.ts`, com as mesmas regras do domínio e mensagens de `strings`.
- `model = linkedSignal(() => carregado ? toModel(valor) : vazio)`; `form(model, schema)`.
- `submit(form, async () => { ... return toServerErrors(form, toApiProblem(e), { 'x.code_duplicate': 'code' }) })`.
- Perfil Leitura: `form(model, (path) => { apply(path, schema); disabled(path, () => !canWrite()); })` e sem
  botão de salvar. Depois de salvar, a tela continua aberta (cadastro novo navega para `edit/:id`).
- Edição envia `version`; em `concurrency_conflict` mostre o banner com "Recarregar dados".
- Layout: `mx-auto max-w-page`, `erp-form-header` (com `erp-action-bar` e Salvar), seções em `erp-section-card`, erro
  geral em `erp-form-banner` e falha de carga em `erp-load-error`.
- Campos: `<erp-field-shell [label] [error]="fieldError(form.campo())">` + `<input hlmInput [formField]>`; selects com
  `hlm-select`/`hlm-combobox` ligados à mão (`[value]` + `(valueChange)`).

## Antes de entregar

`npm run build` dentro de `frontend/` e uma olhada no navegador (lista, cadastro e erro de validação).
Sem testes novos (escopo simplificado em `docs/plan.md`).
