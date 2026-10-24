# Frontend (Angular 22)

## Estrutura

- `src/app/api`: cliente gerado pelo `ng-openapi-gen` (nunca editar; `npm run api:generate`).
- `src/app/core`: layout, `auth/` (sessão, interceptor, guards, tema), `http/` (ProblemDetails → `ApiProblem`,
  erros do servidor no Signal Forms), notificações.
- `src/app/shared/ui`: `AppShell`, `PageHeader`, `Card`, `DataTable`, `StatusChip`, `SegmentedControl`, `ConfirmDialog`.
- `src/app/shared/data`: `debounced()` e estado de listagem na URL.
- `src/app/features/<funcionalidade>`: rotas lazy, telas, `*.strings.ts`, `*-rules.ts` e `testing/`.
- `src/app/styles`: `tokens.scss` (fonte única de cor, raio, espaçamento) e `theme.scss` (Material).

## Padrões

- Siga `features/products` (skill `new-feature-screen`).
- API só via `inject(Api).invoke(fn, params)`; dados de tela com `resource()`.
- Formulários com Signal Forms; regras espelhadas do servidor em `*-rules.ts` e `*.schema.ts`.
- Componentes standalone, `OnPush`, prefixo `erp-`, signals; nada de `any` (nem `$any` no template).
- Nenhum valor visual fixo em componente: use `var(--erp-*)`. Falta token? Crie em `tokens.scss`.
- Textos pt-BR em `<feature>.strings.ts`, nunca soltos no template.

## Armadilhas

- `app.routes.ts` não pode importar `provideMaterialFormDefaults`: rota nova com formulário vai em `loadChildren`.
- Orçamento inicial apertado (~548 de 550 kB): nada de `MatMenu` no layout (o menu da conta é `popover` nativo).

- `MAT_FORM_FIELD_DEFAULT_OPTIONS`/`MatPaginatorIntl` no `app.config` arrastam ~250 kB para o bundle inicial:
  use `provideMaterialFormDefaults()` nos `providers` da rota lazy.
- Campos com `[formField]` não aceitam `min`/`required` no HTML: as regras vêm do schema.
- Breadcrumb lê `routeConfig.data.breadcrumb` (filhas de caminho vazio herdariam o do pai).

## Comandos

- `npm start` (proxy de `/api` para http://localhost:5080), `npm test` (Vitest, roda uma vez), `npm run lint`.
- `npm run api:check` falha se o cliente commitado não bater com `backend/openapi/erp-api.json`.
- O build de produção falha se o orçamento de bundle em `angular.json` estourar.
