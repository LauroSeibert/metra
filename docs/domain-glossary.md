# Glossário do domínio

Termo de negócio (como aparece na tela) → nome no código e no banco. Termo novo entra aqui antes de virar código.

## Produtos

| Termo (pt-BR) | Código (C# / TS) | Banco | Observação |
| --- | --- | --- | --- |
| Produto | `Product` | `product` | Nunca excluído; inativado |
| SKU | `Sku` / `sku` | `product.sku` | Código do produto; único, gravado em maiúsculas; em branco é gerado (`LIV-033488`) |
| Nome | `Name` | `product.name` | Até 200 caracteres |
| Descrição | `Description` | `product.description` | Opcional, até 1000 caracteres |
| Categoria | `ProductCategory` | `product_category` | Nome único; excluída só sem produtos |
| Unidade (de medida) | `Unit` | `unit` | Código único (UN, KG, CX); excluída só sem produtos |
| Preço de custo | `CostPrice` | `product.cost_price` | DECIMAL(18,2), não negativo |
| Preço de venda | `SalePrice` | `product.sale_price` | DECIMAL(18,2), não negativo |
| Margem | calculada na tela | — | (venda − custo) ÷ venda |
| Estoque mínimo | `MinStock` | `product.min_stock` | DECIMAL(18,3); base do alerta de estoque baixo (Fase 4) |
| Ativo / Inativo | `Active` | `product.active` | Inativo não aparece para venda (Fase 6) |

## Parceiros

| Termo (pt-BR) | Código (C# / TS) | Banco | Observação |
| --- | --- | --- | --- |
| Parceiro | `Partner` | `partner` | Cliente, fornecedor ou ambos; nunca excluído, inativado |
| Cliente / Fornecedor / Ambos | `PartnerType.Customer` / `Supplier` / `Both` | `partner.type` (texto) | Filtro de cliente inclui `Both` |
| CPF / CNPJ (documento) | `Document`, `TaxDocument` | `partner.document` | Normalizado (só dígitos e letras); CNPJ pode ser alfanumérico (2026) |
| Tipo de documento | `TaxDocumentKind` (`Cpf`/`Cnpj`) | `partner.document_kind` | Deduzido do tamanho |

## Estoque

| Termo (pt-BR) | Código (C# / TS) | Banco | Observação |
| --- | --- | --- | --- |
| Movimentação de estoque | `StockMovement` | `stock_movement` | Imutável; quantidade com sinal |
| Entrada / Saída / Ajuste / Estorno | `StockMovementType.Entry` / `Exit` / `Adjustment` / `Reversal` | `stock_movement.type` (texto) | Ajuste: informa a contagem, grava a diferença |
| Saldo de estoque | `StockBalance` | `stock_balance` | Um por produto; soma das movimentações |
| Estoque baixo / abaixo do mínimo | `LowStock` | — | Saldo < estoque mínimo |
| Parâmetro da empresa | `AppSetting`, `SettingKeys` | `app_setting` | `stock.allowNegative` (padrão: não) |

## Acesso

| Termo (pt-BR) | Código (C# / TS) | Banco | Observação |
| --- | --- | --- | --- |
| Usuário | `AppUser` (Infrastructure) | `app_user` | E-mail é o login; nunca excluído, inativado |
| Perfil de acesso: Administrador / Operador / Leitura | `Roles.Admin` / `Operator` / `Viewer` | `app_role` | `canWrite` = Admin ou Operador |
| Meu perfil | `/api/v1/me`, `UserProfileDto` | | Nome, tema (`ThemePreference`), itens por página |
| Sessão | `SessionTokensDto` | | Token de acesso (15 min) + refresh (7 dias) |

## Termos transversais

| Termo | Código | Observação |
| --- | --- | --- |
| Versão | `Version` | Concorrência otimista; ver `docs/api-conventions.md` |
| Criado em / Atualizado em | `CreatedAt` / `UpdatedAt` | Sempre UTC |

## Próximas fases (nomes já reservados em `docs/plan.md`)

| Termo | Código |
| --- | --- |
| Venda / item da venda | `Sale` / `SaleItem` |
| Lançamento financeiro | `FinancialEntry` |
| Categoria financeira | `FinancialCategory` |
