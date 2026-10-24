# Módulo de clientes e fornecedores (parceiros)

Segue a estrutura do módulo de produtos (`docs/modules/products.md`).

## Regras de negócio

- Tipo: `Customer` (cliente), `Supplier` (fornecedor) ou `Both` (os dois papéis).
- Nome obrigatório (até 200). E-mail opcional (até 200, gravado em minúsculas, formato validado).
  Telefone opcional (até 20, texto livre).
- Documento obrigatório: CPF (11 dígitos) ou CNPJ (14 posições). Pode vir formatado; é gravado
  normalizado (só dígitos e letras maiúsculas) e o tipo (`documentKind`) é deduzido do tamanho.
- Dígitos verificadores conferidos. Desde julho de 2026 o CNPJ pode ser **alfanumérico**: as 12
  primeiras posições aceitam letras, e no cálculo cada posição vale o código ASCII − 48 (exemplo oficial:
  `12.ABC.345/01DE-35`). Sequências repetidas (`111.111.111-11`) são inválidas.
- Documento único (já normalizado, então pontuação diferente não escapa) → 409 `partner.document_duplicate`.
- Parceiro nunca é excluído (não há `DELETE`): fora de uso, é inativado. O histórico (vendas, contas)
  chega nas fases 5 e 6 e aponta para ele.
- Leitura: todos os perfis. Gravação: Admin e Operador (Viewer recebe 403).

## Endpoints

| Rota | Filtros |
| --- | --- |
| `GET /api/v1/partners` | `search` (prefixo do nome ou do documento, com ou sem pontuação), `type`, `active`, `sort` (`name`, `document`, `createdAt`), `page`, `pageSize` |
| `GET /api/v1/partners/{id}` | |
| `POST /api/v1/partners`, `PUT /api/v1/partners/{id}` | |
| `POST /api/v1/partners/{id}/activate` e `/deactivate` | |

`type=Customer` traz clientes **e** quem é os dois; `type=Supplier` idem para fornecedores;
`type=Both` traz só quem tem os dois papéis.

## Índices (EXPLAIN com 20 mil parceiros, 24/10/2026)

| Índice | Consulta | Plano |
| --- | --- | --- |
| `ux_partner_document` | unicidade; busca por prefixo do documento | range scan |
| `ix_partner_name` | lista padrão (ordem por nome); busca por prefixo do nome; filtros de cliente e fornecedor | covering index scan, para nos 20 primeiros |
| `ix_partner_active_name` | filtro de inativos | covering index lookup |
| `ix_partner_type_name` | filtro `type=Both` (10% da base) | covering index lookup: 0,03 ms contra 0,17 ms percorrendo `ix_partner_name` |
| `ix_partner_created_at` | `sort=createdAt` | evita filesort |

Os filtros de cliente (80% da base) e de fornecedor (30%) não usam `ix_partner_type_name`: o otimizador
prefere percorrer `ix_partner_name` filtrando, e encontra os 20 primeiros em menos de 50 linhas.
A busca em duas colunas usa o mesmo UNION estreito de produtos (ver `docs/modules/products.md`).

## Seed

`seed --profile full` gera 20 mil parceiros (70% clientes, 20% fornecedores, 10% ambos; 5% inativos),
com CPF e CNPJ de números aleatórios e dígitos calculados (parte dos CNPJ alfanuméricos). Rodar o seed numa
base que já tem produtos só completa o que falta.
