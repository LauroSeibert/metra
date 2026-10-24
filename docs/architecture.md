# Arquitetura

Monolito modular em monorepo. A API tem quatro camadas com dependência em uma só direção, e o front espelha as mesmas funcionalidades.

```text
Erp.Api ──► Erp.Application ──► Erp.Domain
   │               ▲
   └──► Erp.Infrastructure
```

1. `Domain` não depende de nenhum outro projeto nem de pacotes de infraestrutura.
2. `Application` depende só de `Domain`.
3. `Infrastructure` implementa as interfaces definidas em `Application`.
4. `Api` é a única camada que conhece todas as outras e monta a injeção de dependência.

As regras 1 a 3 são verificadas por `backend/tests/Erp.UnitTests/LayerDependencyTests.cs`. A `Application` usa as abstrações do EF Core (`IErpDbContext`), nunca o provider (ADR 0002).

## Fluxo de execução

- `docker compose` sobe `mysql` (9.7, utf8mb4), `api` (porta 5080) e `web` (nginx, porta 8080).
- O nginx serve o Angular e repassa `/api/` para a API, então o navegador fala com um único host.
- Saúde: `/health` (processo vivo) e `/health/ready` (banco acessível).

## Front

- `core/`: layout, e futuramente auth, interceptors e guards.
- `shared/ui/`: componentes visuais sem regra de negócio.
- `features/`: uma pasta por funcionalidade, com a mesma divisão da API.
- `styles/`: tokens de design e tema do Angular Material.
