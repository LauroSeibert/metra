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

## Termos transversais

| Termo | Código | Observação |
| --- | --- | --- |
| Versão | `Version` | Concorrência otimista; ver `docs/api-conventions.md` |
| Criado em / Atualizado em | `CreatedAt` / `UpdatedAt` | Sempre UTC |

## Próximas fases (nomes já reservados em `docs/plan.md`)

| Termo | Código |
| --- | --- |
| Parceiro (cliente, fornecedor, ambos) | `Partner` |
| Movimentação de estoque | `StockMovement` |
| Saldo de estoque | `StockBalance` |
| Venda / item da venda | `Sale` / `SaleItem` |
| Lançamento financeiro | `FinancialEntry` |
| Categoria financeira | `FinancialCategory` |
