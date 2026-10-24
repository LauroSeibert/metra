---
paths:
  - "frontend/src/**/*.ts"
  - "frontend/src/**/*.html"
  - "frontend/src/**/*.scss"
---

# Regras do front

- Chamadas à API só pelo cliente gerado: `inject(Api).invoke(listProducts, params)`. Nunca `HttpClient` direto nem tipos escritos à mão.
- Dados de tela com `resource()`; listagens mantêm as linhas anteriores com `linkedSignal` enquanto recarregam.
- Toda tela trata carregando, vazio e erro (o `DataTable` já faz isso para tabelas).
- Formulários com Signal Forms (`form`, `schema`, `[formField]`, `submit`); erros do servidor com `toServerErrors`.
  Não use `min`/`max`/`required` no HTML de campos com `[formField]`: vêm do schema.
- Texto da interface em `<feature>.strings.ts`; cores, raios e espaços só via `var(--erp-*)`.
- Tokens do Material de formulário/paginador vão em `provideMaterialFormDefaults()` na rota lazy, nunca no `app.config`
  (arrasta ~250 kB para o bundle inicial).
- Receita completa: skill `new-feature-screen`. Módulo de referência: `src/app/features/products`.
