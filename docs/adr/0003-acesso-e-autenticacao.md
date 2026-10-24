# ADR 0003: Acesso, autenticação e perfis

- **Data:** 2026-10-24
- **Status:** aceito (decisões tomadas pela IA seguindo o plano, para revisão)

## Contexto

A Fase 3 pede login com refresh token, bloqueio após tentativas falhas, três perfis (Admin, Operador,
Leitura) aplicados por políticas na API e rotas protegidas no front, e cada usuário editando só o próprio
perfil. O plano já previa o ASP.NET Core Identity na Infrastructure.

## Decisões

### Dependência nova

| Dependência | Versão | Licença | Motivo |
| --- | --- | --- | --- |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.13 | MIT | Usuários, senha (hash PBKDF2), bloqueio e perfis sobre o EF Core |

Nenhuma biblioteca de JWT: os tokens usam o esquema bearer do próprio ASP.NET Core.

### Tokens

- Esquema `IdentityConstants.BearerScheme` (`AddBearerToken`): token de acesso de **15 min** e refresh de
  **7 dias**, protegidos pelo Data Protection. São opacos (não são JWT) e só esta API os lê.
- `ISessionTokens` (Application) emite e renova; a implementação usa os protetores do esquema, então
  `POST /auth/login` e `/auth/refresh` devolvem um DTO tipado no OpenAPI (`SessionTokensDto`).
- O refresh confere o **security stamp** do usuário. Sair (`/auth/logout`), trocar a senha, mudar o perfil
  de acesso ou inativar a conta troca o stamp e invalida os refresh já emitidos. O token de acesso que já
  saiu continua valendo até expirar (no máximo 15 min).
- Chaves do Data Protection em `DataProtection:KeysPath` (volume `api-keys` no compose), para a sessão
  sobreviver a um restart da API.

Alternativas descartadas: `MapIdentityApi` (expõe cadastro aberto e rotas que o ERP não quer) e JWT com
biblioteca própria (dependência e código a mais sem ganho para uma API única).

### Senha, bloqueio e limite de tentativas

- Senha: 8+ caracteres, com maiúscula, minúscula e número. Mensagens do Identity traduzidas em
  `IdentityMessages`.
- Bloqueio: 5 senhas erradas seguidas bloqueiam por 15 min (`IdentityOptions.Lockout`). E-mail
  desconhecido responde igual a senha errada (401 `auth.invalid_credentials`), para não revelar contas.
- Limite de requisições: 20 por minuto por IP em `/auth/login` e `/auth/refresh` (429). Atrás do nginx o
  IP vem de `X-Forwarded-For`; a API confia no cabeçalho de qualquer origem porque só o nginx a expõe fora
  do desenvolvimento. **Risco:** com a porta 5080 aberta, quem chama a API direto pode forjar o IP.
  Fechar a porta (ou restringir `KnownProxies`) antes de qualquer uso fora da máquina local.

### Perfis e políticas

- Perfis em `Erp.Domain.Access.Roles`: `Admin`, `Operator`, `Viewer` (na tela: Administrador, Operador,
  Leitura).
- Política padrão (fallback): toda rota exige usuário autenticado; `/health*`, `/openapi` e login/refresh
  são anônimos. Sem sessão, até rota inexistente responde 401 (não revela quais rotas existem).
- `RequireWrite()` (Admin ou Operator) em todo endpoint que grava; `Policies.Admin` no grupo `/users`.
- "Meu perfil" (`/me`) não recebe id: o usuário é o do token. O Admin não pode se inativar nem tirar o
  próprio perfil de Admin (409 `user.self_lockout`), para nunca ficar sem administrador.

### Usuários iniciais

`IdentitySeeder` roda depois das migrations: cria os perfis e, com a base sem usuários, o Admin de
`Auth:BootstrapAdmin:Email/Password` (no compose, `ADMIN_EMAIL`/`ADMIN_PASSWORD` do `.env`). Com
`Auth:DemoUsersPassword` (`DEMO_USERS_PASSWORD`), cria também `operador@erp.local` e `leitura@erp.local`
para testar os perfis.

### Front

- `AuthService` guarda os tokens no `localStorage` para a sessão sobreviver a um F5. **Risco:** um XSS
  leria os tokens. Mitigação: Angular escapa o HTML por padrão, não há `innerHTML` com dados do usuário e o
  token de acesso vive 15 min. Alternativa para produção: refresh em cookie `HttpOnly` + `SameSite=Strict`.
- `authInterceptor` põe o token e, num 401, renova a sessão uma vez (requisições simultâneas compartilham o
  mesmo refresh) e repete; refresh recusado leva ao login com `returnUrl` (só caminhos internos).
- Guards (`authGuard`, `guestGuard`, `roleGuard`) e `canWrite()` só melhoram a experiência: esconder um
  botão não é segurança, a API decide.
- Menu da conta com o `popover` nativo do HTML em vez de `MatMenu`: o `MatMenu` somava 29 kB ao bundle
  inicial e estourava o orçamento de 550 kB.

### Outras mudanças de contrato

- Enums saem como texto no JSON (`JsonStringEnumConverter`): `"Customer"`, `"Dark"`. Legível no contrato,
  estável se a ordem do enum mudar, e o cliente gerado tipa como união de strings.
- `ErpDbContext` passou a herdar de `IdentityDbContext`; as tabelas do Identity seguem a convenção de nomes
  (`app_user`, `app_role`, `user_role`...). `app_user` tem `created_at`, `updated_at` e `version` como as
  demais (`IAuditable`); as auxiliares do Identity não têm.

## Consequências

- Todo teste de integração usa um cliente autenticado (`MySqlApiFixture.ClientFor(Roles.X)`).
- O comando `measure` do seed precisa de `ERP_EMAIL` e `ERP_PASSWORD`.
- Novos endpoints de escrita precisam de `.RequireWrite()`; a skill `new-endpoint` e a rule de endpoints
  foram atualizadas.
