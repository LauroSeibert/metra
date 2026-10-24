# Frontend (Angular 22)

## Estrutura

- `src/app/core/layout`: barra superior, menu lateral (`nav-items.ts`) e `MainLayout`.
- `src/app/shared/ui`: componentes visuais reutilizáveis (`AppShell`, `Card`).
- `src/app/features/<funcionalidade>`: telas com carregamento sob demanda (`loadComponent`).
- `src/app/styles`: `tokens.scss` (fonte única de cor, raio, espaçamento) e `theme.scss` (Material).

## Padrões

- Componentes standalone, `OnPush`, prefixo `erp-`, signals; nada de `any`.
- Nenhum valor visual fixo em componente: use `var(--erp-*)`. Falta token? Crie em `tokens.scss`.
- Textos pt-BR em `<feature>.strings.ts`, nunca soltos no template.
- `track` em todo `@for`. Breadcrumb vem de `data.breadcrumb` da rota.
- Ícones: Material Symbols Outlined via `<mat-icon>`.

## Comandos

- `npm test` roda uma vez (Vitest); `npm run test:watch` fica observando.
- `npm run lint` checa formatação (Prettier); `npm run format` corrige.
- O build de produção falha se o orçamento de bundle em `angular.json` estourar.
