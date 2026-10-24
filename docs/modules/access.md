# Módulo de acesso e perfil

Decisões técnicas no ADR 0003. Aqui ficam as regras que a tela e os testes cobrem.

## Perfis

| Perfil | Código | Pode |
| --- | --- | --- |
| Administrador | `Admin` | tudo, inclusive administrar usuários (e, na Fase 8, configurações) |
| Operador | `Operator` | ler e gravar cadastros, vendas, estoque e financeiro |
| Leitura | `Viewer` | só consultar; telas mostram os cadastros em modo leitura |

Matriz testada em `backend/tests/Erp.IntegrationTests/Access/AuthorizationTests.cs` (critério de saída
da Fase 3).

## Sessão

- `POST /api/v1/auth/login` → `{ accessToken, expiresIn, refreshToken }`. Acesso: 15 min; refresh: 7 dias.
- `POST /api/v1/auth/refresh` troca o refresh por tokens novos; `POST /api/v1/auth/logout` invalida os
  refresh do usuário.
- 5 senhas erradas seguidas → conta bloqueada por 15 min (401 `auth.locked_out`). O Admin destrava
  redefinindo a senha. E-mail desconhecido e senha errada respondem igual (401 `auth.invalid_credentials`).
- 20 tentativas por minuto por IP em login e refresh (429).
- Usuário inativo não entra nem renova a sessão.

## Meu perfil (`/api/v1/me`)

- Cada usuário lê e edita só o próprio perfil: nome (até 100), tema (`Light`/`Dark`) e itens por página
  (10, 20, 50 ou 100). E-mail e perfil de acesso só o Admin muda.
- Edição com `version` (409 `concurrency_conflict` se desatualizada).
- Troca de senha exige a senha atual (400 em `currentPassword` se errada) e encerra as outras sessões.
- Itens por página vira o padrão das listagens quando a URL não traz `pageSize`.

## Usuários (`/api/v1/users`, só Admin)

- Listar (busca por prefixo do nome ou e-mail, filtros de perfil e situação), criar (e-mail único → 409
  `user.email_duplicate`), editar nome, perfil e situação, e redefinir senha (também destrava a conta).
- Usuário nunca é excluído, só inativado. O Admin não pode se inativar nem tirar o próprio perfil de Admin
  (409 `user.self_lockout`). A tela de usuários entra na Fase 8 (Configurações).
