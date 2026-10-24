# Plano de Implementação: ERP Base para Demonstração de CLAUDE.md

Oct 23, 2026 · @Lauro

## Visão geral

O projeto é um ERP de demonstração para pequenas empresas de comércio, construído para servir de laboratório: mostrar, com medição, como organizar contexto para IA (`CLAUDE.md`, rules, skills) em um sistema grande.

**Objetivos**

1. Simular um ERP realista, com cadastros, listagens, vendas, estoque, financeiro, dashboard e perfil de usuário.
2. Ser performático com volume realista: 100 mil produtos e 1 milhão de movimentações de estoque gerados por seed.
3. Servir de base para o artigo: as mesmas tarefas executadas com e sem contexto, comparando custo e qualidade.
4. Ser implementável por IA em sessões curtas, com padrões repetíveis e verificáveis.

**Fora de escopo** (para manter o projeto viável): emissão fiscal (NF-e), multiempresa, integrações bancárias, aplicativo mobile nativo e conciliação contábil.

**Quem lê este documento:** você, para decidir a stack e o escopo, e a IA que for implementar, para seguir as regras da seção *Boas práticas obrigatórias*.

## Os arquivos CLAUDE.md ficam salvos no repositório?

Sim: o `CLAUDE.md` do projeto é feito para ser versionado no git e compartilhado com todo o time. Só os arquivos pessoais ficam de fora, segundo a [documentação oficial](https://code.claude.com/docs/en/memory).

| Arquivo ou pasta | Vai para o git? | Para que serve |
| --- | --- | --- |
| `CLAUDE.md` na raiz (ou `.claude/CLAUDE.md`) | Sim | Instruções do projeto para todos: arquitetura, comandos, convenções |
| `CLAUDE.md` dentro de subpastas | Sim | Regras de um módulo, carregadas quando Claude lê arquivos dali |
| `.claude/rules/*.md` | Sim | Regras por tema, opcionalmente limitadas a caminhos (`paths`) |
| `.claude/skills/` | Sim | Receitas do projeto, como "criar endpoint" ou "criar tela" |
| `.claude/settings.json` | Sim | Permissões e hooks que valem para o time |
| `CLAUDE.local.md` | Não, entra no `.gitignore` | Preferências pessoais só deste projeto |
| `.claude/settings.local.json` | Não, entra no `.gitignore` | Ajustes pessoais, como exclusões locais |
| `~/.claude/CLAUDE.md` | Não, fica fora do repositório | Preferências pessoais para todos os projetos |
| Memória automática em `~/.claude/projects/<projeto>/memory/` | Não, é local da máquina | Anotações que o próprio Claude faz |

Para este projeto, a decisão é versionar tudo o que é compartilhado. Assim, quem clonar o repositório para reproduzir o artigo já recebe o mesmo contexto, e a diferença entre "com contexto" e "sem contexto" pode ser demonstrada apagando ou restaurando esses arquivos.

Duas observações práticas: o `CLAUDE.md` é contexto e não uma regra imposta, então o que não pode falhar deve virar *hook*; e a documentação recomenda manter cada arquivo abaixo de 200 linhas.

## Stack recomendada

A recomendação é Angular 22 no front, .NET 10 LTS na API e MySQL 9.7 LTS no banco, com Angular Material em vez de PrimeNG e o provider oficial da Oracle para o MySQL. Versões verificadas em 23/10/2026. O projeto é deliberadamente simples: poucas tecnologias, um único caminho de acesso a dados e nenhum serviço além da API e do banco.

| Camada | Escolha | Observação |
| --- | --- | --- |
| Framework do front | Angular 22.x, componentes standalone e signals | O Angular passou a ter [um major por ano](https://angular.love/angular-new-release-cycle-release-schedule-change-explained): o 22 tem suporte ativo até jun/2027 e LTS até jun/2028 |
| Biblioteca de UI | Angular Material + CDK, na mesma versão do Angular | Tema próprio inspirado no protótipo do Painel do Parceiro (ver *Design e identidade visual*). A partir da v22, o [PrimeNG deixou de ser open source](https://dev.to/playfulprogramming-angular/ng-news-2617-primengs-new-licensing-and-a2ui-for-angular-4eik) |
| Gráficos | Apache ECharts | Escolha final na seção *Decisões em aberto* |
| Cliente HTTP | Cliente TypeScript gerado a partir do OpenAPI | Os tipos do front nunca são escritos à mão |
| Testes do front | Runner padrão do Angular CLI + Playwright para e2e | Cobertura mínima definida em *Critérios de pronto e medição* |
| Runtime da API | .NET 10 LTS (patch 10.0.13, de 13/10/2026) | [Suporte até 14/11/2028](https://dotnet.microsoft.com/en-us/platform/support/policy). O .NET 8 e o 9 saem de suporte em 10/11/2026 |
| Estilo da API | ASP.NET Core Minimal APIs, OpenAPI nativo e erros em ProblemDetails | Um arquivo de endpoints por funcionalidade |
| Acesso a dados | EF Core 10 em tudo, inclusive no dashboard | Um único caminho de acesso a dados; consultas pesadas usam projeção para DTO e índices conferidos com `EXPLAIN` |
| Provider MySQL | `MySql.EntityFrameworkCore` 10.x (oficial da Oracle) | Um pacote cobre o EF Core 8, 9 e 10 e evita o conflito de versões do [Pomelo](https://startdebugging.net/de/2026/10/fix-nu1608-nu1107-version-conflict-pomelo-entityframeworkcore-mysql-ef-core-10/), que segue sem build para o EF Core 10. Regra única: a versão maior do pacote é sempre igual à do EF Core (10) |
| Banco | MySQL 9.7 LTS, InnoDB, `utf8mb4` | [LTS lançado em 21/04/2026](https://fedoraproject.org/wiki/Changes/MySQL_9.7). O plugin `mysql_native_password` foi removido; usar `caching_sha2_password`. Imagem fixa `mysql:9.7` no Compose; plano B: 8.4 LTS |
| Autenticação | ASP.NET Core Identity, JWT de acesso curto e refresh token em cookie `HttpOnly` | Autorização por políticas, com três perfis: Admin, Operador e Leitura |
| Observabilidade | Logging padrão do .NET (`ILogger`) e health checks | Sem bibliotecas extras |
| Testes da API | xUnit, Testcontainers com MySQL real e Bogus para dados | Nada de banco em memória: o comportamento do MySQL é parte do teste |
| Infra | Docker Compose (MySQL, API, front) e GitHub Actions | Um comando sobe o ambiente completo |

**Ponto de atenção do provider.** A tradução de consultas do provider da Oracle difere da do Pomelo, e buscas por prefixo podem deixar de usar o índice. A regra é usar `EF.Functions.Like` de forma explícita e conferir o plano com `EXPLAIN`. O provider se configura com `UseMySQL(connectionString)`, sem informar a versão do servidor. O PrimeNG tem um fork MIT (OpenNG), mas ele ainda é jovem demais para ser a base de uma demonstração.

## Por que essa stack

O critério principal é previsibilidade: quanto menos decisões em aberto, menos a IA diverge entre sessões, e isso importa tanto para o ERP quanto para a demonstração do `CLAUDE.md`.

| Decisão | Escolhido | Alternativas | Motivo e trade-off |
| --- | --- | --- | --- |
| Front | Angular | React + Vite + TanStack, Blazor | O Angular traz rotas, formulários, injeção de dependência e HTTP no próprio framework, o que reduz as escolhas. React tem mais bibliotecas de grid, mas exige decidir roteamento, estado e formulários. Blazor mantém tudo em C#, mas é menos usual para grids com muitas linhas |
| UI | Angular Material + CDK | PrimeNG v22+, OpenNG | Material acompanha a versão do Angular e é livre de licença comercial. O custo: a tabela é mais básica, então filtros, paginação no servidor e linhas expansíveis são componentes próprios, que viram padrões documentados |
| API | Minimal APIs | Controllers | Menos cerimônia e um arquivo por funcionalidade. Controllers seriam igualmente válidos; o importante é adotar só um estilo |
| Dados | EF Core em tudo | EF Core + Dapper, só Dapper | Um único caminho de acesso a dados é mais simples de documentar e de a IA seguir. Se uma consulta do dashboard não atingir a meta, o primeiro passo é ajustar projeção e índice |
| Arquitetura | Monolito modular | Microsserviços | Um único deploy e um banco bastam para o objetivo. Serviços extras, como um de auditoria, ficam de fora para manter o projeto simples e o foco no contexto da IA |
| Banco | MySQL | PostgreSQL, MongoDB | Vendas, estoque e financeiro dependem de transações e de integridade entre tabelas, que o modelo relacional entrega com menos código. O MongoDB foi avaliado e descartado; PostgreSQL com Npgsql seria a troca de menor risco |
| Repositório | Monorepo | Um repositório por projeto | Um único `CLAUDE.md` raiz e um índice de documentação conseguem descrever o sistema inteiro, e uma mudança de contrato (API e front) cabe em um só commit |

## Design e identidade visual

O visual segue o protótipo do Painel do Parceiro: barra superior azul, menu lateral só de ícones, fundo lavanda claro, cartões brancos de cantos bem arredondados e etiquetas em tons pastel. As cores abaixo foram medidas no protótipo e viram *tokens*, para que nenhuma tela use cor ou espaçamento solto.

| Token | Valor | Uso |
| --- | --- | --- |
| `--erp-primary` | `#2F4FD8` | Barra superior, opção ativa dos filtros, links e ícones ativos |
| `--erp-primary-soft` | `#E0E6FF` | Etiquetas azuis e destaques suaves |
| `--erp-bg` | `#EEF1F7` | Fundo da página |
| `--erp-surface` | `#FFFFFF` | Cartões, menu lateral e linhas de tabela |
| `--erp-surface-muted` | `#EEF3FB` | Cabeçalho de tabela |
| `--erp-text` | `#141A3A` | Títulos e valores |
| `--erp-text-muted` | `#4A5272` | Subtítulos e cabeçalhos de tabela |
| `--erp-success` | `#087A3A` | Botão de ação principal e valores de entrada |
| `--erp-danger` | `#B3361A` | Saídas e valores negativos |
| `--erp-info` | `#5BE0F7` | Botões secundários; a etiqueta ciana usa fundo `#DDF6FC` e texto `#0E5368` |
| `--erp-warning` | `#FFE14D` | Pílula de destaque; a etiqueta âmbar usa fundo `#FFF1D6` e texto `#7A4A0E` |

**Padrões de layout do protótipo, que o ERP repete:**

- **Estrutura da página:** barra superior com *breadcrumb* à esquerda e atalhos e perfil à direita; menu lateral estreito só com ícones; conteúdo com título, subtítulo e botões de ação alinhados à direita.
- **Cartão de resumo:** valor grande, a ação principal ao lado, um divisor e métricas com rótulo em caixa alta, com entradas em verde e saídas em vermelho.
- **Tabelas dentro de cartões:** cabeçalho pequeno em caixa alta e cinza, valores monetários à direita, linhas expansíveis por seta e etiquetas pastel por tipo de registro.
- **Filtros rápidos:** controle segmentado, com a opção ativa em azul.
- **Forma e espaço:** cartões com cantos bem arredondados (cerca de 16 px, medido a olho), botões e etiquetas em formato de pílula, sombras suaves e bastante espaço em branco.
- **Tipografia:** sans-serif geométrica (Inter), títulos em negrito e números grandes nos resumos.
- **Telas:** desktop primeiro; telas estreitas ficam fora do escopo inicial.

**Como isso vira código:**

- Os tokens ficam em `frontend/src/app/styles/tokens.scss`, e o tema do Angular Material aponta para eles. O detalhamento vive em `docs/design-system.md`.
- Componentes compartilhados em `shared/ui/`: `AppShell`, `PageHeader`, `KpiCard`, `StatusChip`, `SegmentedControl` e `DataTable` com linha expansível.
- As telas de estoque e financeiro reaproveitam o padrão "cartão de resumo + tabela" do protótipo; vendas e produtos usam a tabela com filtros.
- Regra para a IA: nenhuma cor, raio ou espaçamento fixo nos componentes. Toda mudança visual começa nos tokens, e toda tela nova é montada com os componentes de `shared/ui/`.

## Escopo funcional

O ERP tem oito módulos que se conectam por eventos de negócio reais: confirmar uma venda baixa o estoque e gera contas a receber, e é essa cadeia que torna a simulação convincente.

| Módulo | Telas | Regras de negócio |
| --- | --- | --- |
| Acesso e perfil | Login, meu perfil, troca de senha, preferências (tema claro/escuro, itens por página) | Sessão com refresh token; bloqueio após tentativas falhas; cada usuário edita só o próprio perfil |
| Produtos | Listagem com busca, filtros (categoria, status, estoque baixo) e ordenação; cadastro e edição; categorias e unidades | SKU único; preço de venda não negativo; produto com movimentação é inativado, nunca excluído |
| Clientes e fornecedores | Listagem e cadastro de parceiros, com tipo (cliente, fornecedor ou ambos) | Documento (CPF/CNPJ) único e validado; parceiro com histórico não é excluído |
| Vendas | Listagem com filtro por período, status e cliente; nova venda com itens, desconto e forma de pagamento; detalhe da venda | Rascunho é editável; confirmar baixa o estoque e gera as parcelas a receber; cancelar estorna ambos; desconto máximo vem da configuração |
| Estoque | Saldo por produto; histórico de movimentações; lançamento manual de entrada, saída e ajuste | Movimentação é imutável (correção é um novo movimento de estorno); saldo negativo conforme configuração |
| Lançamentos financeiros | Contas a pagar e a receber com filtros por vencimento e status; novo lançamento; baixa; categorias financeiras | Parcelamento gera uma linha por parcela; atraso é calculado pela data de vencimento; lançamento baixado não é editado, só estornado |
| Dashboard | Faturamento do mês contra o mês anterior, vendas por dia nos últimos 30 dias, 10 produtos mais vendidos, estoque abaixo do mínimo, contas a vencer em 7 dias, fluxo de caixa projetado | Leitura apenas; dados com cache curto |
| Configurações | Dados da empresa, parâmetros (estoque negativo, desconto máximo) e usuários | Somente o perfil Admin acessa |

**Perfis de acesso:** Admin (tudo, inclusive configurações e usuários), Operador (cadastros, vendas, estoque e financeiro) e Leitura (somente consulta), aplicados por políticas de autorização na API e por rotas protegidas no front.

**Volume do seed**, para a demonstração de performance ser honesta: 100 mil produtos, 20 mil parceiros, 200 mil vendas, 1 milhão de movimentações de estoque e 400 mil lançamentos financeiros, gerados por um comando reproduzível.

## Arquitetura e estrutura do repositório

A arquitetura é um monolito modular em monorepo: a API tem quatro camadas com dependência em uma só direção, e o front espelha as mesmas funcionalidades.

```text
erp-demo/
├── CLAUDE.md                  # mapa do sistema, comandos, regras globais
├── .claude/
│   ├── rules/                 # regras por tema ou caminho
│   ├── skills/                # receitas: novo endpoint, nova tela, migration
│   └── settings.json          # permissões e hooks do time
├── docs/
│   ├── architecture.md        # camadas e fluxos principais
│   ├── domain-glossary.md     # termos de negócio e seus nomes no código
│   ├── design-system.md       # tokens, layout e componentes visuais
│   ├── api-conventions.md     # rotas, paginação, erros
│   ├── modules/               # regras de negócio, um arquivo por módulo
│   └── adr/                   # decisões de arquitetura datadas
├── backend/
│   ├── CLAUDE.md
│   ├── Directory.Build.props  # nullable, analisadores, warnings como erro
│   ├── Directory.Packages.props   # versões centralizadas
│   ├── src/
│   │   ├── Erp.Domain/        # entidades e regras, sem dependências externas
│   │   ├── Erp.Application/   # casos de uso, DTOs, validação
│   │   ├── Erp.Infrastructure/# EF Core, migrations, Identity
│   │   ├── Erp.Api/           # endpoints por funcionalidade, DI, auth, OpenAPI
│   │   └── Erp.Seed/          # gerador de dados com Bogus
│   └── tests/
│       ├── Erp.UnitTests/
│       └── Erp.IntegrationTests/   # Testcontainers com MySQL
├── frontend/
│   ├── CLAUDE.md
│   └── src/app/
│       ├── core/              # auth, interceptors, layout, guards
│       ├── styles/            # tokens de design e tema do Material
│       ├── shared/            # componentes de UI reutilizáveis
│       └── features/          # products, partners, sales, stock,
│                              # finance, dashboard, settings, profile
├── docker-compose.yml
└── .github/workflows/         # build, testes e verificação do contrato OpenAPI
```

**Regras de dependência (API):**

1. `Domain` não depende de nenhum outro projeto nem de pacotes de infraestrutura.
2. `Application` depende só de `Domain`.
3. `Infrastructure` implementa as interfaces definidas em `Application`.
4. `Api` é a única camada que conhece todas as outras e monta a injeção de dependência.

**Convenções de nomes:**

- Código, rotas e colunas em inglês; textos da interface em pt-BR; o glossário liga cada termo de negócio ao nome no código (por exemplo, "lançamento" é `FinancialEntry`).
- Rotas no plural e em *kebab-case*, sob `/api/v1`: `/api/v1/products`, `/api/v1/stock-movements`.
- Tabelas em `snake_case` no singular (`sale_item`); chaves primárias `BIGINT`, nunca GUID aleatório.
- Cada funcionalidade tem a mesma estrutura nos dois lados: `features/sales/` no front corresponde a `Sales/` na API, o que torna o padrão previsível para a IA.

## Modelo de dados

O modelo gira em torno de dois registros que nunca são apagados: a movimentação de estoque e o lançamento financeiro. Todo o resto pode ser inativado ou recalculado a partir deles.

| Entidade | Campos principais | Regras de integridade |
| --- | --- | --- |
| `product` | sku, name, category\_id, unit\_id, cost\_price, sale\_price, min\_stock, active | `sku` único; categoria e unidade com chave estrangeira |
| `product_category`, `unit` | name, code | Nome único; não excluir se houver produto |
| `partner` | type (cliente, fornecedor, ambos), name, document, email, phone, active | `document` único e validado |
| `stock_balance` | product\_id, quantity, version | Uma linha por produto; atualizada na mesma transação da movimentação |
| `stock_movement` | product\_id, type, quantity, unit\_cost, occurred\_at, reference\_type, reference\_id, user\_id, note | Somente inserção; a soma por produto deve igualar `stock_balance.quantity` |
| `sale` | number, partner\_id, status (rascunho, confirmada, cancelada), discount, total, issued\_at, confirmed\_at | Só `rascunho` é editável; número sequencial sem lacunas |
| `sale_item` | sale\_id, product\_id, quantity, unit\_price, discount, total | Preço copiado do produto no momento da venda; quantidade maior que zero |
| `financial_category` | name, kind (receita ou despesa) | Nome único por tipo |
| `financial_entry` | kind (a receber ou a pagar), category\_id, partner\_id, sale\_id, description, amount, due\_date, paid\_at, paid\_amount, status (aberto, pago, cancelado), installment\_no, installment\_total | Lançamento pago só é estornado; parcelas de uma venda somam o total da venda |
| `app_setting` | key, value | Chave única; valores validados na leitura |

**Convenções de banco válidas para todas as tabelas:**

- Valores monetários em `DECIMAL(18,2)` e quantidades em `DECIMAL(18,3)`; nunca `FLOAT` ou `DOUBLE`.
- Datas em UTC, em `DATETIME(6)`, com `created_at` e `updated_at` em toda tabela.
- Chaves estrangeiras sempre declaradas; exclusão física só em tabelas sem histórico.
- Coluna `version` para concorrência otimista nas tabelas editadas por várias telas.
- Índices definidos junto com a consulta que os justifica, por exemplo `stock_movement (product_id, occurred_at DESC)`, `financial_entry (status, due_date)` e `sale (status, issued_at)`.
- Todo índice novo é conferido com `EXPLAIN` nos 100 mil produtos e 1 milhão de movimentações do seed.

## Boas práticas obrigatórias

Estas regras valem para qualquer IA ou pessoa que implementar o projeto, e cada uma é verificável por build, teste ou revisão. Se um pedido pontual conflitar com elas, a IA deve apontar o conflito e perguntar antes de seguir.

### Processo de trabalho

- Antes de codar, ler o `CLAUDE.md` da pasta onde vai mexer e os termos usados em `docs/domain-glossary.md`.
- Seguir o padrão do módulo de referência (produtos) em vez de criar estrutura nova; se o padrão não servir, propor a mudança antes.
- Trabalhar uma tarefa por sessão e por PR, e começar por um plano quando a mudança atravessa API e front.
- Não inventar regra de negócio: o que não estiver em *Escopo funcional* ou no glossário vira pergunta.
- Não adicionar dependência sem registrar um ADR e conferir a licença (o caso do PrimeNG é o exemplo do que evitar).
- Nunca editar arquivos gerados (cliente OpenAPI) nem migrations já commitadas.
- Terminar toda tarefa rodando build, testes e lint, e informar o resultado real, inclusive falhas.

### API em .NET

- `Nullable` habilitado e warnings tratados como erro em todos os projetos.
- Código assíncrono de ponta a ponta, repassando `CancellationToken` de endpoints a consultas.
- Endpoints finos: validam a entrada, chamam um caso de uso e devolvem um DTO. Regra de negócio fica em `Domain` e `Application`.
- Nunca expor entidade do EF na API; sempre DTO.
- Erros em `ProblemDetails` (RFC 9457) com código estável: 400 para validação, 404 para não encontrado e 409 para conflito de concorrência.
- Toda listagem é paginada no servidor, com limite máximo de 100 itens por página.
- Operações que tocam várias tabelas (confirmar e cancelar venda) rodam em uma única transação explícita.
- Em listagens, projetar com `Select` para DTO e usar `AsNoTracking`; sem `Include` em cascata.
- SQL manual só em último caso, sempre parametrizado e nunca concatenado.
- Datas sempre em UTC; o relógio entra por `TimeProvider` para permitir testes.
- Logs estruturados sem senha, token ou documento completo.

### Banco de dados (MySQL)

- Alterações de esquema somente por migration versionada, com nome descritivo; `DROP` exige aprovação explícita.
- Todo índice novo vem com a consulta que o justifica e o resultado do `EXPLAIN`.
- Chave primária `BIGINT`; nunca GUID aleatório.
- Offset serve para listas pequenas; movimentações de estoque e outras tabelas grandes usam paginação por chave (*keyset*).
- Dinheiro e quantidade em `DECIMAL`, nunca ponto flutuante; texto em `utf8mb4`.
- O saldo de estoque é atualizado na mesma transação da movimentação, com concorrência otimista.

### Front em Angular

- Componentes standalone, `strict` ligado no TypeScript e nenhum `any`.
- O cliente HTTP vem sempre do OpenAPI. Mudou a API, regenera o cliente no mesmo commit.
- Rotas por funcionalidade com carregamento sob demanda; blocos pesados, como gráficos, em `@defer`.
- Listagens com paginação, filtro e ordenação no servidor, busca com *debounce* e `track` em todo `@for`.
- Um único padrão de formulário no projeto (ver *Decisões em aberto*); a validação do front espelha a do servidor, que é a autoridade.
- Toda tela trata três estados: carregando, vazio e erro.
- Textos da interface em pt-BR reunidos por funcionalidade, sem strings soltas nos templates.
- Orçamento de tamanho de bundle configurado no build, para a regressão aparecer no CI. Cores, raios e espaçamentos vêm só dos tokens de design; nenhum valor visual fixo nos componentes.

### Segurança

- Autorização sempre no servidor; *guards* do front servem só para a experiência de uso.
- Senhas tratadas apenas pelo Identity; tokens e segredos nunca vão para log nem para o repositório.
- CORS restrito à origem do front, HTTPS em qualquer ambiente fora do desenvolvimento local e limite de tentativas no login.
- Segredos em variáveis de ambiente ou *user-secrets*; o repositório traz só um `.env.example` sem valores reais.
- Dados do seed são 100% sintéticos (Bogus). CPF e CNPJ de teste são gerados por algoritmo e nunca vêm de pessoas ou empresas reais.

### Testes

- Regras de domínio têm testes unitários; endpoints e transações críticas têm testes de integração com MySQL real (Testcontainers).
- Teste de invariante: a soma das movimentações de cada produto é igual ao saldo.
- Correção de bug começa por um teste que falha e reproduz o problema.
- E2E com Playwright para três fluxos: login, nova venda e baixa de lançamento.
- Testes não dependem de ordem de execução nem do relógio real.

### Git e entrega

- *Conventional Commits* e branches `feat/` e `fix/`; PR pequeno, com o que mudou e como testar.
- O CI bloqueia o merge se falhar build, teste, lint ou a conferência de que o cliente OpenAPI commitado bate com a API.
- Nunca commitar `CLAUDE.local.md`, segredos, `bin/`, `obj/` ou `node_modules/`.

### Quando parar e perguntar

- Mudança de regra de negócio, de contrato público da API ou de esquema destrutivo.
- Nova dependência ou mudança de versão maior de qualquer pacote.
- Teste que continua falhando depois de duas tentativas sem causa clara.
- Conflito entre este documento e o pedido recebido.

## Estratégia de contexto para IA

O contexto é dividido em camadas, e cada informação entra na camada mais barata em que ainda funciona. A [documentação do Claude Code](https://code.claude.com/docs/en/memory) trata o `CLAUDE.md` como contexto e não como regra imposta, e recomenda mantê-lo abaixo de 200 linhas.

| Camada | Entra no contexto | O que colocar neste projeto |
| --- | --- | --- |
| `CLAUDE.md` raiz | Sempre | Mapa do sistema, comandos, regras globais, onde ler mais |
| `CLAUDE.md` por pasta (`backend/`, `frontend/`) | Quando Claude lê arquivos dali | Padrões e armadilhas específicas daquela parte |
| `.claude/rules/` com `paths` | Quando Claude abre arquivos que casam com o padrão | Regras de API, de front e de migrations |
| `.claude/skills/` | Descrição sempre; o corpo só quando relevante | Receitas: novo endpoint, nova tela, nova migration |
| `docs/` | Só se Claude ler o arquivo | Glossário, convenções de API, regras por módulo, ADRs |
| Hooks | Executam fora do contexto | O que não pode falhar: formatação, arquivos gerados |
| Subagents | Contexto próprio, devolvem um resumo | Explorar um módulo; revisar um diff contra as regras |

**Detalhe que muda o custo:** um `@arquivo.md` dentro do `CLAUDE.md` [carrega o arquivo inteiro no início da sessão](https://code.claude.com/docs/en/memory), então não economiza contexto. Para docs grandes, o `CLAUDE.md` cita o caminho em texto simples e diz quando ler.

**Exemplo de `CLAUDE.md` raiz** (cerca de 30 linhas):

```markdown
# ERP Demo

Simulação de ERP para pequenas empresas de comércio.
Monorepo: `backend/` (.NET 10, MySQL 9.7) e `frontend/` (Angular 22).

## Mapa
- `backend/src/Erp.Domain`: entidades e regras de negócio
- `backend/src/Erp.Application`: casos de uso e DTOs
- `backend/src/Erp.Infrastructure`: EF Core e migrations
- `backend/src/Erp.Api`: endpoints por funcionalidade
- `frontend/src/app/features`: uma pasta por funcionalidade

## Comandos
- Ambiente: `docker compose up -d`
- API: `dotnet run --project backend/src/Erp.Api`
- Testes da API: `dotnet test backend`
- Front: `npm start`, `npm test` e `npm run e2e` dentro de `frontend/`
- Regenerar cliente OpenAPI: `npm run api:generate` dentro de `frontend/`
- Seed completo: `dotnet run --project backend/src/Erp.Seed -- --profile full`

## Regras globais
- Código em inglês, interface em pt-BR; termos de negócio em `docs/domain-glossary.md`.
- Siga o módulo de referência: produtos (`backend/src/Erp.Api/Products` e `frontend/src/app/features/products`).
- Mudou a API? Regenere o cliente no mesmo commit.
- Visual: siga `docs/design-system.md` e use só os tokens de `frontend/src/app/styles/`.
- Ao terminar, rode build, testes e lint e relate o resultado real.

## Leia só o que for tocar
- Convenções de API: `docs/api-conventions.md`
- Regras de negócio por módulo: `docs/modules/` (um arquivo por módulo)
- Decisões de arquitetura: `docs/adr/`

## Pare e pergunte
Regra de negócio nova, mudança de contrato público, esquema destrutivo ou nova dependência.
```

**Exemplo de regra com escopo por caminho**, que só entra quando Claude abre arquivos da API:

```markdown
---
paths:
  - "backend/src/Erp.Api/**/*.cs"
---

# Regras de endpoints
- Endpoint fino: valida, chama um caso de uso e devolve DTO.
- Erros em ProblemDetails; 409 para conflito de concorrência.
- Toda listagem é paginada no servidor, com limite de 100 itens.
```

**Skills previstas**, com descrições curtas porque ficam sempre carregadas:

1. `new-endpoint`: criar ou alterar um endpoint seguindo o padrão do módulo de produtos.
2. `new-feature-screen`: criar tela de listagem e de cadastro no front a partir do template.
3. `new-migration`: alterar o esquema com migration, índice justificado e `EXPLAIN`.
4. `seed-and-measure`: gerar o seed completo e medir uma consulta.

**Hooks previstos:** impedir edição de arquivos gerados (cliente OpenAPI) e rodar o formatador depois de cada edição. A documentação indica hooks para tudo o que precisa acontecer sempre, porque eles [executam independentemente da decisão do Claude](https://code.claude.com/docs/en/memory).

**Manutenção:** o `CLAUDE.md` cresce quando a IA erra a mesma coisa pela segunda vez, não de antemão. Revisar a cada fase, remover o que ficou obsoleto e conferir com `/context` se os arquivos foram carregados. Comentários HTML em bloco (`<!-- -->`) são removidos antes de ir ao contexto, então servem para notas de manutenção sem custo.

## Fases de implementação

A ordem segue as dependências do negócio: produtos primeiro, como módulo de referência, e vendas por último entre os módulos, porque dependem de estoque e financeiro. Cada fase só termina quando passa no seu critério de saída.

1. **Fundação.** Repositório, `docker-compose.yml` com MySQL 9.7, solução .NET com as quatro camadas, projeto Angular com os tokens de design e a estrutura base (barra superior, menu lateral e cartões) no estilo do protótipo, CI básico e os `CLAUDE.md` mínimos. *Saída:* ambiente sobe com um comando, a tela base abre e os testes vazios passam no CI.
2. **Produtos, o módulo de referência.** API, front, testes e seed de 100 mil produtos, com OpenAPI e cliente gerado, mais os componentes `PageHeader`, `DataTable` e `StatusChip`. Extrair daqui as skills `new-endpoint` e `new-feature-screen`, o `docs/api-conventions.md` e o `docs/design-system.md`. *Saída:* listagem paginada com p95 abaixo de 200 ms no seed completo.
3. **Acesso e parceiros.** Login, perfil do usuário, políticas por perfil, clientes e fornecedores com validação de CPF e CNPJ. *Saída:* testes de autorização cobrindo os três perfis.
4. **Estoque.** Movimentações imutáveis, saldo transacional e paginação por chave. *Saída:* invariante "soma das movimentações é igual ao saldo" testada, e histórico com 1 milhão de linhas respondendo dentro da meta.
5. **Lançamentos financeiros.** Contas a pagar e a receber, parcelas, baixa e estorno, com o padrão "cartão de resumo + tabela" do protótipo. *Saída:* testes de integração do ciclo aberto, pago e estornado.
6. **Vendas.** Rascunho, confirmação e cancelamento, integrados a estoque e financeiro. *Saída:* teste de integração da cadeia completa: confirmar baixa o estoque e gera as parcelas, cancelar desfaz ambos.
7. **Dashboard.** Consultas com EF Core, cache curto e carregamento com `@defer`. *Saída:* dashboard abaixo de 500 ms no seed completo.
8. **Configurações.** Parâmetros da empresa e usuários. *Saída:* um parâmetro alterado, como o estoque negativo, muda o comportamento da venda.
9. **Endurecimento.** Revisão de índices com `EXPLAIN`, orçamento de bundle, checklist de segurança, e2e dos três fluxos e README. *Saída:* CI verde com todas as verificações obrigatórias.
10. **Experimento e artigo.** Criar uma branch por cenário a partir do commit final, removendo ou restaurando os arquivos de contexto, e rodar o protocolo da seção *Critérios de pronto e medição*, registrando os resultados.

## Como começar

Faltam quatro passos para começar: instalar as ferramentas, criar o repositório, entregar o primeiro pedido ao Claude Code e conferir o critério de saída da Fase 1.

**1. Instalar as ferramentas** (Windows):

| Item | Versão | Como conferir |
| --- | --- | --- |
| .NET SDK | 10.x | `dotnet --version` |
| Node.js | LTS 24 a partir da 24.15.0, ou 22.22.3 em diante | `node -v` |
| Docker Desktop | Atual | `docker --version` |
| Git | Atual | `git --version` |
| Claude Code | Atual; exige conta Pro, Max, Team, Enterprise ou Console | `claude --version` |
| VS Code | Atual, com C# Dev Kit e Angular Language Service | Abrir uma pasta com `.cs` e um `.ts` |

O [Angular 22.0 exige Node ^22.22.3, ^24.15.0 ou ^26.0.0 e TypeScript 6.0](https://next.angular.dev/reference/versions); o TypeScript vem como dependência do projeto e não é instalado à parte. O Claude Code instala no PowerShell com `irm https://claude.ai/install.ps1 | iex` ou com `winget install Anthropic.ClaudeCode`, conforme a [documentação de instalação](https://code.claude.com/docs/en/setup.md). Não é preciso instalar MySQL (o Docker cuida), nem Angular CLI ou TypeScript globais.

**2. Criar o repositório.** Criar um repositório vazio `erp-demo` no GitHub, clonar, abrir um terminal na pasta e salvar este documento, exportado em Markdown, como `docs/plan.md`.

**3. Primeiro pedido ao Claude Code.** No terminal, dentro da pasta, rodar `claude` e enviar:

```text
Leia docs/plan.md, mas apenas as seções Visão geral, Stack recomendada, Design e identidade visual, Arquitetura e estrutura do repositório e Boas práticas obrigatórias. Implemente somente a Fase 1 das Fases de implementação. Antes de escrever código, apresente um plano de execução e espere minha aprovação. Ao final, rode build e testes e informe o resultado real.
```

**4. Conferir a saída da Fase 1.** `docker compose up -d` sobe o MySQL; `dotnet test backend` e `npm test` passam; a tela base (menu lateral, barra superior e uma página vazia no estilo do protótipo) abre no navegador; o CI fica verde.

## Critérios de pronto e medição

Uma tarefa só está pronta quando cumpre a lista abaixo, e o projeto só serve de demonstração se o ganho do contexto for medido com o mesmo protocolo todas as vezes.

**Definição de pronto (vale para toda tarefa):**

- [ ] Build sem warnings, testes e lint verdes, com o resultado informado.
- [ ] Regra nova ou alterada tem teste; bug corrigido tem teste que falhava antes.
- [ ] Contrato da API mudou: OpenAPI e cliente do front regenerados no mesmo commit.
- [ ] Migration revisada, com índice justificado por `EXPLAIN` quando houver consulta nova.
- [ ] `docs/` e ADR atualizados se uma decisão ou regra mudou.
- [ ] Metas de performance da tabela abaixo continuam atendidas.

**Metas de performance** (hipóteses iniciais, a validar na Fase 2 com o seed completo; p95 medido localmente com 100 mil produtos e 1 milhão de movimentações):

| Operação | Meta (p95) |
| --- | --- |
| Listagem paginada de produtos, com busca ou filtro | 200 ms |
| Histórico de movimentações de um produto (paginação por chave) | 200 ms |
| Confirmar uma venda com 10 itens | 300 ms |
| Dashboard completo | 500 ms |

**Protocolo do experimento** para o artigo. As mesmas tarefas rodam em cenários diferentes, cada um em uma branch criada a partir do commit final do projeto:

| Cenário | Contexto disponível |
| --- | --- |
| A | Nenhum arquivo de contexto (sem `CLAUDE.md`, `.claude/` nem `docs/`) |
| B | Apenas o `CLAUDE.md` raiz |
| C | `CLAUDE.md` raiz e por pasta, rules com `paths`, skills e hooks |
| D | Igual a C, mas com a documentação importada por `@` em vez de citada por caminho |

| Tarefa | Descrição |
| --- | --- |
| T1 | Criar o cadastro de marcas seguindo o padrão do módulo de produtos |
| T2 | Adicionar o campo "código de barras" ao produto, da migration à tela |
| T3 | Corrigir um bug plantado: cancelar uma venda não estorna o estoque |
| T4 | Criar o relatório de estoque crítico com consulta otimizada |

**O que registrar em cada execução:** tokens e custo (`/cost`), arquivos lidos, tentativas até os testes passarem, correções manuais necessárias e aderência às regras (checklist de revisão). Cada combinação roda pelo menos três vezes, porque o resultado varia, e com o prompt de cada tarefa fixo em um arquivo. Registrar também o modelo e a versão do Claude Code usados.

## Decisões em aberto e riscos

Restam seis decisões a fechar e cinco riscos a vigiar; o banco (MySQL) e o provider (Oracle) já estão decididos.

| Decisão | Opções | Recomendação | Fechar até |
| --- | --- | --- | --- |
| Versão do MySQL | 9.7 LTS ou 8.4 LTS | 9.7; voltar ao 8.4 se a imagem ou o provider tiverem problema | Fase 1 |
| Formulários | Signal Forms (estáveis no Angular 22) ou Reactive Forms | Testar Signal Forms com o Material no módulo de produtos; sem gambiarras, adotar, senão Reactive Forms | Fase 2 |
| Gerador do cliente OpenAPI | NSwag, `ng-openapi-gen`, `openapi-typescript` | Escolher o que gera serviços Angular tipados e roda por script npm | Fase 2 |
| Idioma do código | Inglês ou português | Inglês, com o glossário ligando os termos de negócio | Fase 1 |
| Biblioteca de gráficos | Apache ECharts ou Chart.js | ECharts, por desempenho com muitos pontos | Fase 7 |
| Depósitos | Um só ou vários | Um só; vários depósitos ficam como extensão futura | Fase 4 |

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| Consultas traduzidas de forma diferente pelo provider da Oracle | Busca por prefixo sem índice e lentidão | `EF.Functions.Like` explícito, `EXPLAIN` em toda consulta nova e testes com MySQL real; plano B: PostgreSQL com Npgsql |
| Documentação desatualizada engana a IA | Erros sutis e repetidos | Docs curtas, índices gerados por script e revisão a cada fase |
| Escopo grande demais | Projeto não termina | Fases com critério de saída; dashboard e configurações podem ser cortados sem quebrar o resto |
| Resultado do experimento varia com modelo e versão | Conclusões frágeis | Mínimo de três execuções, versões registradas, prompts e dados publicados |
| Ferramentas mudam (Claude Code, Angular) | Artigo envelhece | Citar versão e data em cada afirmação e linkar a documentação oficial |

## Fontes

Consultadas em 23/10/2026. As informações sobre o Pomelo e sobre o PrimeNG vêm de fontes secundárias recentes; vale reconfirmar as duas antes de começar.

- [How Claude remembers your project](https://code.claude.com/docs/en/memory): `CLAUDE.md`, rules, imports e memória automática
- [Advanced setup (Claude Code)](https://code.claude.com/docs/en/setup.md): instalação e requisitos do Claude Code
- [.NET Support Policy](https://dotnet.microsoft.com/en-us/platform/support/policy): versões e datas de suporte do .NET
- [Version compatibility (Angular)](https://next.angular.dev/reference/versions): Node e TypeScript exigidos pelo Angular 22
- [Angular New Release Cycle](https://angular.love/angular-new-release-cycle-release-schedule-change-explained): ciclo anual e janela de suporte do Angular 22
- [Ng-News 26/17: PrimeNG's New Licensing](https://dev.to/playfulprogramming-angular/ng-news-2617-primengs-new-licensing-and-a2ui-for-angular-4eik): mudança de licença do PrimeNG e fork OpenNG
- [Fix: NU1608 / NU1107 entre Pomelo e EF Core 10](https://startdebugging.net/de/2026/10/fix-nu1608-nu1107-version-conflict-pomelo-entityframeworkcore-mysql-ef-core-10/): estado do Pomelo e do provider da Oracle para EF Core 10
- [Changes/MySQL 9.7 (Fedora)](https://fedoraproject.org/wiki/Changes/MySQL_9.7): data de lançamento do MySQL 9.7 LTS e remoção do `mysql_native_password`
