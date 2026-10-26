# Frontend (Angular 22)

## Estrutura

- `src/app/api`: cliente gerado pelo `ng-openapi-gen` (nunca editar; `npm run api:generate`).
- `src/app/core`: layout, `auth/` (sessão, interceptor, guards, tema), `http/` (ProblemDetails → `ApiProblem`,
  erros do servidor no Signal Forms), notificações.
- `src/app/shared/ui`: componentes do projeto (`AppShell`, `PageHeader`, `KpiStrip`, `DataTable`, `SectionCard`,
  `FormHeader`, `SearchField`, `FilterSelect`, `SearchPicker`, `MoneyInput`, `StatusChip`, `ConfirmService`...) e,
  em `helm/`, os componentes do Spartan UI (código nosso). Visual de referência: `features/products/product-list`.
- `src/app/shared/data`: `debounced()` e estado de listagem na URL.
- `src/app/features/<funcionalidade>`: rotas lazy, telas, `*.strings.ts`, `*-rules.ts` e `testing/`.
- `src/app/styles`: `tokens.scss` (fonte única de cor, raio, espaçamento). `src/tailwind.css` liga o Spartan e o
  Tailwind aos tokens.

## Padrões

- Siga `features/products` (skill `new-feature-screen`) e `docs/design-system.md`.
- API só via `inject(Api).invoke(fn, params)`; dados de tela com `resource()`.
- Formulários com Signal Forms; regras espelhadas do servidor em `*-rules.ts` e `*.schema.ts`.
- Componentes standalone, `OnPush`, prefixo `erp-`, signals; nada de `any` (nem `$any` no template).
- Estilo com Tailwind e os tokens; nenhum valor visual fixo em componente. Falta token? Crie em `tokens.scss`.
- Ícones só pelo `erp-icon`; ícone novo se registra em `shared/ui/icon/icons.ts`.
- Textos pt-BR em `<feature>.strings.ts`, nunca soltos no template.

## Armadilhas

- `app.routes.ts` não pode importar o `<modulo>.strings.ts`: rota nova vai em `loadChildren` e o breadcrumb de
  primeiro nível fica em `LAYOUT_STRINGS.breadcrumbs`.
- Folha de estilo de componente: até 12 kB de aviso. No layout (core/layout) as dicas usam `title` nativo.
- `npx ng g @spartan-ng/cli:ui <x>` reinstala `@ng-icons`: troque pelos `erp-icon` e rode
  `npm uninstall @ng-icons/core @ng-icons/lucide`.
- `@container` do Tailwind não vale para o próprio elemento: ponha o `@container` num pai e a `grid` num filho.
- Nome de classe de componente não pode coincidir com utilitário do Tailwind.
- Campos com `[formField]` não aceitam `min`/`required` no HTML: as regras vêm do schema.
- Breadcrumb lê `routeConfig.data.breadcrumb` (filhas de caminho vazio herdariam o do pai).

## Comandos

- `npm start` (proxy de `/api` para http://localhost:5080), `npm run lint`.
- `npm run api:check` falha se o cliente commitado não bater com `backend/openapi/erp-api.json`.
