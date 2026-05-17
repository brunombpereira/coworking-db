# Relatório APF-T — Sistema de Gestão de Coworking

**Autor:** Bruno Pereira (88860, turma 11272)
**Disciplina:** Base de Dados — LECI
**Data:** 2026-05

---

## 1. Introdução

Este projeto implementa o sistema de gestão de um operador de coworking com vários espaços físicos, salas de reunião reserváveis à hora e postos de trabalho atribuídos por adesão ou alugados ao dia. Cobre o ciclo de vida completo de cliente, adesão, reserva e pagamento, e expõe à aplicação C# Windows Forms um conjunto de stored procedures, views e funções que encapsulam toda a regra de negócio.

A entrega APF-E definiu requisitos, DER e esquema relacional. A APF-T concretiza o modelo em SQL Server 2019, com schema, índices, 16 triggers, 8 views de consulta, 7 UDFs e 14 SPs, mais segurança baseada em roles, auditoria via system-versioned tables, e plano de testes documentado.

## 2. Arquitetura

```
┌────────────────────────────────┐
│  CoworkingApp (C# WinForms)    │
│  - Login / Register Form       │
│  - Menus por role              │
│  - Forms entidade + relatórios │
└──────────┬─────────────────────┘
           │ ADO.NET (SqlConnection)
           │ User: cowork_app  → role: app_staff
           ▼
┌────────────────────────────────┐
│  SQL Server 2019 Express       │
│  CoworkingDB                   │
│  - Tabelas + temporal history  │
│  - 16 triggers (regras BN)     │
│  - 14 SPs, 8 views, 7 UDFs     │
│  - Roles: app_cliente / staff  │
│           / admin              │
└────────────────────────────────┘
```

A aplicação nunca executa SQL ad-hoc — todas as operações passam por SPs com `GRANT EXECUTE` à role apropriada. As tabelas têm `DENY` direto para `app_cliente` e `app_staff`. Isto:
- Elimina vetores de SQL injection.
- Centraliza a regra de negócio na BD (a app não pode contornar triggers/validações).
- Permite auditar com `sys.dm_exec_procedure_stats` e logs ao nível da SP.

## 3. Modelo de Dados

### 3.1 Tabelas principais

| Tabela | PK | Resumo |
|---|---|---|
| `cliente` | cliente_id | Identidade fiscal (NIF), email único |
| `plano` | plano_id | Catálogo de planos Flex/Fixo/Privado |
| `espaco` | espaco_id | Edifício/sala física com horário |
| `recurso` | recurso_id | **Supertype** — abstrai sala vs. posto |
| `sala` | recurso_id (FK→recurso) | Subtype com capacidade, preço/hora |
| `posto` | recurso_id (FK→recurso) | Subtype com tipo (Flex/Fixo/Privado), preço/dia |
| `adesao` | adesao_id | Cliente↔Plano com preço snapshot + recurso opcional |
| `reserva` | reserva_id | Cliente↔Recurso para data/horas |
| `pagamento` | pagamento_id | Liga-se a **exactly one** adesão XOR reserva |
| `utilizador` | utilizador_id | Auth — username + hash SHA256 + salt |
| `politica_cancelamento` | politica_id | Tiers de reembolso por antecedência |
| `notificacao` | notificacao_id | Eventos enviados ao cliente |
| `lista_espera` | lista_espera_id | Cliente↔Recurso quando indisponível |

### 3.2 Decisões de modelo

**Recurso supertype.** Sala e posto partilham operações (reservar, ver disponibilidade) mas têm atributos próprios. Em vez de tabela única com colunas nulas, optei pelo supertype `recurso` + subtypes `sala`/`posto` ligados via PK==FK com `ON DELETE CASCADE`. Vantagens: a FK em `reserva.recurso_id` aponta para o supertype, simplificando o trigger T1 de sobreposição. Custo: dois JOINs para obter os detalhes (atenuado por índices).

**Posto via adesão.** O atributo `adesao.recurso_id` (NULL para Flex, NOT NULL para Fixo/Privado) materializa a regra "Fixo/Privado têm posto atribuído". Trigger T9 enforça a coerência tipo_plano↔tipo_posto. Alternativa rejeitada: tabela ponte `adesao_posto` — overhead sem ganho expressivo.

**Pagamento como XOR + snapshot de preço.** A constraint `ck_pagamento_servico` garante que `adesao_id` e `reserva_id` são mutuamente exclusivos e que pelo menos um está preenchido. Em resposta à pergunta do professor *"qual o preço?"*, o `pagamento` carrega ainda uma coluna `preco_servico_snapshot DECIMAL(10,2) NOT NULL`, que fotografa o preço do serviço subjacente (`reserva.valor` ou `adesao.preco_acordado`) no momento da criação. A ligação ao valor cobrado deixa de viver apenas no trigger e passa a ser explícita no schema:
- `ck_pagamento_valor_snapshot CHECK (valor = preco_servico_snapshot)` — o que se paga tem de ser o que se cobrou.
- Trigger T6 — `preco_servico_snapshot` tem de coincidir com o preço actual do serviço no momento da inserção.
- Trigger T8 — `cliente_id` tem de ser o titular do serviço.

Esta redundância controlada protege o histórico contra futuras alterações de preço (alinhada com `adesao.preco_acordado`) e dá ao auditor uma única coluna para responder à pergunta acima.

## 4. Normalização

Todas as tabelas estão em **BCNF**:

| Tabela | Dependências funcionais não-triviais | Análise |
|---|---|---|
| `cliente` | `cliente_id → nome, nif, email, ...`; `nif → cliente_id`; `email → cliente_id` | 3NF: todas saem da chave. BCNF: nif e email são candidate keys (UNIQUE), por isso à esquerda da DF — OK. |
| `plano` | `plano_id → nome_plano, tipo_plano, preco_mensal, ...`; `nome_plano → plano_id` | BCNF. |
| `espaco` | `espaco_id → nome, morada, ...`; `nome → espaco_id` | BCNF. |
| `sala` | `recurso_id → espaco_id, nome, capacidade, ...`; `(espaco_id, nome) → recurso_id` | BCNF. |
| `posto` | análogo a sala | BCNF. |
| `adesao` | `adesao_id → cliente_id, plano_id, ...` | 3NF/BCNF. Notar: `preco_acordado` é snapshot — duplica `plano.preco_mensal` no momento do INSERT mas justifica-se porque o preço pode mudar e a adesão tem de preservar o valor histórico (data temporal). Esta "redundância controlada" é decisão consciente. |
| `reserva` | `reserva_id → cliente_id, recurso_id, data, horas, ...` | BCNF. |
| `pagamento` | `pagamento_id → cliente_id, valor, preco_servico_snapshot, ...` | BCNF. `preco_servico_snapshot` é redundância controlada (snapshot do preço do serviço no momento do pagamento — paralelo a `adesao.preco_acordado`). |

Não existe violação 3→BCNF porque as únicas DFs não-triviais saem ou de PKs ou de UNIQUE keys (candidate keys), pelo que toda DF tem lado esquerdo superkey.

## 5. Segurança

### 5.1 Autenticação
- Tabela `utilizador` com `password_hash VARBINARY(64)` (SHA-256) e `salt VARBINARY(16)` aleatório por utilizador (`CRYPT_GEN_RANDOM`).
- SP `sp_login_user` retorna resultset vazio em qualquer falha (não distingue username inválido vs. password errada) — evita user-enumeration.
- `sp_change_password` exige password atual.

### 5.2 Autorização
Três roles:

| Role | Permissões |
|---|---|
| `app_cliente` | Login, ver/criar/cancelar as suas reservas, lista de espera, notificações |
| `app_staff` | Tudo do cliente + registar clientes, criar/cancelar adesões, registar pagamentos, promover lista de espera, relatórios |
| `app_admin` | `CONTROL` sobre a BD (DDL + DML direto) |

`DENY` explícito nas tabelas para `app_cliente`/`app_staff` — só SPs/views. A app liga-se como `cowork_app` (member de `app_staff`).

### 5.3 SQL injection
Inexistente por construção: a app só invoca SPs parametrizadas via `SqlCommand.Parameters.Add(...)`. Não há `string.Format` em SQL nem `EXEC (@sql)` na BD.

## 6. Concorrência

O trigger T1 (`trg_reserva_sem_sobreposicao`) faz `IF EXISTS` antes de aceitar o INSERT. Em isolamento `READ COMMITTED` (default), dois `INSERT`s concorrentes em sessões diferentes podem ambos passar o IF antes de qualquer COMMIT — race condition: ambas as reservas ficam gravadas.

**Solução adotada:** `sp_getapplock` nos SPs `sp_criar_reserva_sala` e `sp_criar_reserva_posto`, com resource string `'reserva_recurso_<id>_<data>'`. O lock é exclusive e libertado no fim da transação (`@LockOwner = 'Transaction'`). Sessões concorrentes para o mesmo recurso/data serializam-se; sessões para recursos diferentes não bloqueiam.

**Alternativa considerada:** `ALLOW_SNAPSHOT_ISOLATION ON` + `SET TRANSACTION ISOLATION LEVEL SERIALIZABLE`. Rejeitada porque sobrecarrega todas as queries (não só as de reserva) e degrada throughput.

Ver `Tests/test_concorrencia.sql` para reprodução manual em duas sessões SSMS.

## 7. Auditoria — Temporal Tables

`adesao`, `reserva` e `pagamento` são `SYSTEM_VERSIONED`. Cada `UPDATE`/`DELETE` escreve a versão anterior em `<tabela>_history` com `valid_from`/`valid_to` automáticos. Permite:

- Reconstituir o estado da BD em qualquer momento: `SELECT * FROM reserva FOR SYSTEM_TIME AS OF '2026-05-01';`
- Auditar quem alterou o quê (cruzando com logs de `sys.dm_exec_sessions`).
- Suportar relatórios de evolução (revenue mensal histórico imutável).

Pré-requisito: nenhuma tabela `SYSTEM_VERSIONED` pode ter trigger `INSTEAD OF`. O trigger original `trg_adesao_preco_snapshot` foi removido e a lógica de snapshot do preço migrou para `sp_criar_adesao`.

## 8. Performance e Planos de Execução

Índices criados (ver `Indexes.sql`):

| Índice | Coluna(s) | Query que otimiza |
|---|---|---|
| `idx_reserva_recurso` | (recurso_id, data_reserva, hora_inicio, hora_fim) | Trigger T1: scan de sobreposições |
| `idx_pagamento_adesao` (filtered) | adesao_id WHERE NOT NULL | Trigger T6: validação valor |
| `idx_pagamento_reserva` (filtered) | reserva_id WHERE NOT NULL | Trigger T6: validação valor |
| `idx_adesao_cliente` | (cliente_id, estado) | Trigger T4: adesão ativa única |
| `idx_reserva_cliente` | (cliente_id, estado) | Listagem "as minhas reservas" |
| `idx_pagamento_cliente` | (cliente_id, estado) | `fn_receita_cliente`, vw_top_clientes |
| `idx_adesao_recurso` (filtered) | recurso_id WHERE NOT NULL | Trigger T9: posto via adesão |

**Análise no SSMS:**
```sql
SET STATISTICS IO ON; SET STATISTICS TIME ON;
SELECT * FROM reserva r WHERE r.recurso_id = 1 AND r.data_reserva = '2026-05-04';
-- Antes do índice: Table Scan (~N pages)
-- Depois:           Index Seek (~3 pages)
```

Filtered indexes em `pagamento` reduzem o tamanho em 50% (a maioria dos pagamentos liga só a adesão XOR reserva).

## 9. Plano de Testes

Ver `SQL_Scripts/Tests/Plano_Testes.md`. 25 casos cobrindo:
- Cada um dos 12 THROWs dos triggers (TC01–TC11).
- Auth: register/login/change_password (TC12–TC15).
- Concorrência manual em 2 sessões (TC16).
- Temporal: FOR SYSTEM_TIME ALL e AS OF (TC17–TC18).
- Reembolso por política (TC19–TC20).
- Lista de espera + promoção (TC21–TC22).
- Reservas recorrentes (TC23).
- Segurança: permissões por role (TC24–TC25).

## 10. Interface — Fase 3: Card-list redesign

Após a fase 2 (componentes base + tema indigo/slate descrita em `apft_submit.md`), todos os 12 UserControls foram refactorados para um padrão uniforme inspirado em Linear/Notion. O `DataGridView` foi substituído por listas de **cards horizontais**, que são mais navegáveis e visualmente ricas.

### Padrão de UC

Cada UC segue a mesma estrutura via `TableLayoutPanel` root:

```
┌─ Title (48 px) ──────────────────────────────────────┐
├─ Toolbar — ModernCard (72 px) ───────────────────────┤
│ [Filtros chip à esquerda]              [+ Acção →]   │
├─ KPIs — 3 ou 4 ModernCards (108 px) ─────────────────┤
│ [📊 KPI1]   [✓ KPI2]   [€ KPI3]   [⏱ KPI4]           │
├─ Lista — ScrollableList (Fill) ──────────────────────┤
│ ┌────────────────────────────────────────────────┐   │
│ │ ⬤ Avatar  Nome bold              Valor  Status │   │
│ │           Subline meta           Acções ✏ 🗑   │   │
│ └────────────────────────────────────────────────┘   │
│ ┌────────────────────────────────────────────────┐   │
│ │ ...                                            │   │
│ └────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────┘
```

Decisão arquitectural: o **outer** do conteúdo de cada tab é um `Panel(PageBg)`, não um `ModernCard(CardBg)`, para que os cards interiores tenham contraste visual contra o background mais escuro do parent.

### Componentes reutilizáveis novos

| Componente | Responsabilidade |
|---|---|
| `TabButton` | Tab com underline accent + `AutoSize` por texto |
| `StatusPill` | Pill `Filled` ou `Dot`; helper `MeasureDotWidth` (via `Graphics.MeasureString`, mais preciso que `TextRenderer.MeasureText` para fonts bold pequenos) |
| `ModernCombo` | Wrapper visual de `ComboBox` com bg `FieldBg` rounded |
| `ModernSelect` | Dropdown custom com popup `Form` borderless + `FocusablePanel` paintado à mão (substitui `ComboBox` cuja chrome branca não respeitava o tema dark) |
| `ModernDateField` + `ModernCalendar` | Campo data + popup calendar custom em pt-PT (substitui `DateTimePicker`/`MonthCalendar` nativos Windows-style) |
| `SegmentedControl` | Picker `[A | B | C]` com pill accent no segmento activo |
| `ScrollableList` | `Panel` scrollable com scrollbar dark + thumb arrastável (substitui o `AutoScroll` nativo que mostrava scrollbar branca) |
| `ToggleChip` | Chip on/off para filtros booleanos |

### Bugs notáveis resolvidos durante o redesign

- **Vírgula clipada nos preços** (`200,00 €` parecia `200.00 €`). Labels com `Height=22` e `Font 12pt bold` cortavam o descender da vírgula. Padronizado para `Height ≥ 28` em todos os preços.
- **Bars empilhadas no mesmo X** no chart de Receita Mensal. `Series.IsXValueIndexed = true` força index incremental por ponto em vez de o framework tentar parsear o X string como número (e falhar → tudo a `X=0`).
- **Bordas brancas do `ComboBox` nativo** que não respeitavam o tema dark — resolvido criando o `ModernSelect` from scratch.
- **Pills com último char cortado** (`"Terminada"` → `"Terminad"`). `TextRenderer.MeasureText` subestima 1–3 px o glyph real vs `Graphics.MeasureString` (engine GDI+). Standardizado via `StatusPill.MeasureDotWidth` que usa o último + buffer.
- **Win32 handle exhaustion** com seed expandido (~290 reservas × ~15 controls por card = 4350+ handles num único UC). Limit do processo ≈ 10000. Resolvido com `MaxRender = 80` em cada UC + label "+ X mais antigos não mostrados", e `TOP 200` nas queries.
- **Popup do detalhe de notificação fechava-se sozinho** ao abrir — `Form.Deactivate` disparava durante o `Show()` por race com foco. Substituído por `IMessageFilter` que detecta `WM_LBUTTONDOWN` fora de `Form.Bounds`.

### Métricas

- 13 commits dedicados a esta fase (`feature/*-redesign` branches, merge `--no-ff` a cada UC concluído).
- ~3500 linhas novas (componentes + redesigns) contra ~1800 removidas (DataGridViews + toolbar antiga).
- 9 novos ficheiros de componentes (`TabButton.cs`, `StatusPill.cs`, `ModernCombo.cs`, `ModernSelect.cs`, `ModernDateField.cs`, `ModernCalendar.cs`, `SegmentedControl.cs`, `ScrollableList.cs`, `ToggleChip.cs`).

### Seed expandido para popular charts

`SQL_DML.sql` ganhou um bloco final (preserva o seed base original) com:
- 15 clientes adicionais (total 20)
- 25 adesões históricas via `INSERT` literal (20 `Terminada` + 5 `Ativa/Pendente`)
- ~72 reservas de sala via `WHILE` loop (1 quarta-feira/semana × 72 semanas), com rotação determinística de clientes/recursos e slots horários para evitar T1 (sobreposição)
- ~9 day passes de posto Flex (Diogo/Eva alternados, ~bimestrais — evita T11)
- Pagamentos auto-gerados via `INSERT ... SELECT` sobre `adesao` e `reserva` (snapshot=valor para passar T6)
- 15 utilizadores novos

Cobertura: 18 meses (Jan/2025 → Mai/2026), suficiente para os gráficos mostrarem tendências mensais sem sobrecarregar a UI.

### Ordem de execução dos scripts SQL

A ordem é crítica porque alguns SPs dependem de outros. Documentada em `SQL_Scripts/README.md`:

`SQL_DDL` → `User_defined_functions` → `Triggers` → `Views` → **`Auth`** → `Stored_procedures` → `Temporal_tables` → `Indexes` → `Security` → `SQL_DML`

A nota importante: **`Auth.sql` *antes* de `Stored_procedures.sql`** porque o `sp_registar_cliente_completo` (em Stored_procedures) chama o `sp_register_user` (em Auth). E `Security.sql` por último porque os `GRANT` precisam que os objects já existam.

## 11. Conclusão

O sistema cobre o domínio com 9 tabelas core + 4 de extensão (auth, política, notificação, lista de espera), com regras enforçadas a 3 níveis: constraints SQL (cardinalidade/domínio), triggers (regras transversais), SPs (concorrência + lógica composta). A camada de segurança via roles + SPs torna a aplicação resistente a SQL injection by design, e a auditoria via temporal tables dá-nos histórico para sempre sem trigger adicional.

**Limitações conhecidas:**
- Notificações ficam só na tabela; envio efetivo de email exigiria SQL Agent + Database Mail configurados fora do scope da disciplina.
- O cálculo de reembolso assume que a política está seedada com tiers cobrindo 0h em diante; falta UI para gerir tiers.
- O sistema não suporta multi-tenancy (uma só CoworkingDB serve um operador).

**Próximos passos naturais:** dashboard de KPIs em SSRS, UI de gestão de políticas, integração com gateway de pagamento real.

---

## Anexo A. Rastreabilidade Requisito → Implementação

Mapa de cobertura entre o `Requisitos.md` (APF-E) e o schema/SQL/app implementado.
Para cada item, lista-se a implementação concreta — útil para auditoria do
professor e como suporte à apresentação oral.

**Convenções:**
`T1..T16` = triggers; `sp_*` = stored procedures; `vw_*` = views; `fn_*` = funções;
`UcX` = UserControl C#; CHECK/UNIQUE/FK = constraints declarativas.

### A.1 Requisitos Funcionais

| RF | Descrição | Implementação |
|---|---|---|
| RF1 | Registar clientes | Tabela `cliente`; SP `sp_registar_cliente`; `UcClientes` (CRUD) |
| RF2 | Dados de identificação e contacto | Colunas `cliente.nome, nif, email, telefone, data_registo`; UNIQUE em `nif` e `email`; CHECK `nif NOT LIKE '%[^0-9]%'` |
| RF3 | Registar planos | Tabela `plano`; UNIQUE `nome_plano`; CHECK `tipo_plano ∈ {Flex,Fixo,Privado}`; `UcPlanos` |
| RF4 | Associar clientes a planos via adesões | Tabela `adesao` (FK `cliente_id` + FK `plano_id`); SP `sp_criar_adesao`; `UcAdesoes` |
| RF5 | Manter histórico de adesões | `adesao.estado ∈ {Pendente,Ativa,Suspensa,Cancelada,Terminada}`; **SYSTEM_VERSIONING** → tabela `adesao_history`; view `vw_clientes_com_adesao_ativa` |
| RF6 | Registar espaços físicos | Tabela `espaco`; UNIQUE `nome`; CHECK `hora_fecho > hora_abertura`; `UcEspacos` |
| RF7 | Salas em cada espaço | Tabela `sala` (FK `espaco_id`); UNIQUE `(espaco_id, nome)`; `UcEspacos` (tab Salas) |
| RF8 | Postos em cada espaço | Tabela `posto` (FK `espaco_id`); UNIQUE `(espaco_id, codigo)`; `UcEspacos` (tab Postos) |
| RF9 | Reservar salas | `sp_criar_reserva_sala` (com `sp_getapplock`); T2/T3/T12 validam horário/capacidade/coerência; `UcReservas` |
| RF10 | Reservar postos | `sp_criar_reserva_posto` (com `sp_getapplock`); T11/T12 validam compatibilidade com adesão; `UcReservas` |
| RF11 | Histórico de reservas de um cliente | UDF `fn_reservas_cliente_periodo(@cli, @ini, @fim)`; view `vw_reservas_historico` (FOR SYSTEM_TIME ALL); `UcRelatorios` |
| RF12 | Consultar ocupação de salas/postos | View `vw_ocupacao_recurso`; UDF `fn_taxa_ocupacao_espaco(@esp, @data)`; UDF `fn_recurso_disponivel(...)`; `UcDashboard`, `UcEstatisticas` |
| RF13 | Registar pagamentos | Tabela `pagamento`; SP `sp_registar_pagamento` (com snapshot); `UcPagamentos` |
| RF14 | Histórico de pagamentos por cliente | UDF `fn_receita_cliente(@cli)`; view `vw_top_clientes_receita`; **SYSTEM_VERSIONING** → `pagamento_history`; `UcRelatorios` |
| RF15 | Controlar estado de reservas e adesões | Colunas `estado` em ambas (CHECK enum); SPs `sp_cancelar_reserva`, `sp_cancelar_reserva_com_reembolso`, `sp_cancelar_adesao`; trigger T5 calcula `data_fim` |

### A.2 Requisitos Não Funcionais

| RNF | Descrição | Implementação |
|---|---|---|
| RNF1 | Integridade e consistência | FKs em todas as relações; 16 triggers; SPs com `SET XACT_ABORT ON` + `sp_getapplock` para concorrência; CHECK em domínios de enum |
| RNF2 | Unicidade NIF e email | UNIQUE em `cliente.nif` e `cliente.email` (constraints declarativas — falham com erro 2627/2601) |
| RNF3 | Consulta fácil de disponibilidade | UDF `fn_recurso_disponivel`; view `vw_ocupacao_recurso`; index em `(recurso_id, data_reserva)` |
| RNF4 | Crescimento de dados | `Indexes.sql` cobre FKs e colunas de busca frequente; views agregadas (`vw_receita_mensal`, `vw_receita_por_plano`) suportam paginação |
| RNF5 | Clareza do modelo | DER + ER actualizados (`presentation/DER.md`, `presentation/EsquemaRelacional.md`); §3 do relatório justifica cada decisão |

### A.3 Regras de Negócio

| RN | Descrição | Implementação |
|---|---|---|
| RN1 | Cliente pode ter 0..N adesões | Cardinalidade FK em `adesao.cliente_id` (1:N). **Desvio:** T4 (`trg_adesao_ativa_unica`) restringe a **uma única adesão Ativa simultânea** — decisão consciente discutida em §3.2 (evita conflitos de faturação) |
| RN2 | Adesão pertence a 1 cliente e 1 plano | FK NOT NULL em `adesao.cliente_id` e `adesao.plano_id` |
| RN3 | Plano associado a vários clientes ao longo do tempo | Cardinalidade N:1 em `adesao` → `plano`; view `vw_receita_por_plano` agrega histórico |
| RN4 | Espaço inclui várias salas | FK `sala.espaco_id` (N:1) com `ON DELETE NO ACTION` (não permite apagar espaço com salas) |
| RN5 | Espaço disponibiliza vários postos | Analogamente, `posto.espaco_id` (N:1) com `ON DELETE NO ACTION` |
| RN6 | Cada sala pertence a 1 espaço | FK NOT NULL `sala.espaco_id` |
| RN7 | Cada posto pertence a 1 espaço | FK NOT NULL `posto.espaco_id` |
| RN8 | Cliente pode reservar várias salas | Cardinalidade FK `reserva.cliente_id` (N:1) |
| RN9 | Cliente pode reservar vários postos | Mesma — tabela `reserva` é unificada (correção #2 do prof.) |
| RN10 | Reserva de sala refere-se a 1 sala | FK `reserva.recurso_id`; T12 (`trg_reserva_horas_coerentes`) exige `hora_inicio/fim NOT NULL` quando recurso é sala |
| RN11 | Reserva de posto refere-se a 1 posto | FK `reserva.recurso_id`; T12 exige horas NULL; T11 evita colisão com adesão Flex/Fixo/Privado activa |
| RN12 | Sala sem reservas sobrepostas | T1 (`trg_reserva_sem_sobreposicao`) + `sp_criar_reserva_sala` com `sp_getapplock` por `(recurso_id, data)` para evitar race condition |
| RN13 | Posto sem reservas sobrepostas | Mesmo T1 + `sp_criar_reserva_posto` (dia inteiro: compara só data, não horas) |
| RN14 | Cliente pode efectuar vários pagamentos | Cardinalidade FK `pagamento.cliente_id` (N:1) |
| RN15 | Cada pagamento associado a 1 cliente | FK NOT NULL `pagamento.cliente_id`; T8 (`trg_pagamento_cliente_consistente`) valida que `cliente_id` é o titular do serviço pago |

### A.4 Correcções do Professor

| Correção | Implementação |
|---|---|
| **#1 — "Qual o preço?" do Pagamento** | Coluna `pagamento.preco_servico_snapshot DECIMAL(10,2) NOT NULL`; CHECK `ck_pagamento_valor_snapshot (valor = preco_servico_snapshot)`; trigger T6 (`trg_pagamento_valor_correto`) valida que `preco_servico_snapshot` coincide com `reserva.valor` / `adesao.preco_acordado`; SP `sp_registar_pagamento` faz o snapshot a partir do serviço. Justificação em §3.2 |
| **#2 — Reserva como entidade associativa** | Tabela única `reserva` (em vez de `reserva_sala` + `reserva_posto` separadas) com FK para `recurso` (supertype de `sala`/`posto`). M:N entre `cliente` e `recurso` materializada com atributos próprios (`data_reserva, hora_*, valor, estado, num_participantes, notas`). T12 enforça as semânticas diferentes consoante o tipo de recurso |

### A.5 Features adicionais (não pedidas pelos requisitos)

| Feature | Motivação | Implementação |
|---|---|---|
| Autenticação por roles | Sem login, não há como demonstrar a separação Admin/Staff/Cliente | Tabela `utilizador` (SHA-256 + salt); SPs `sp_register_user`, `sp_login_user`, `sp_change_password`; 3 roles em `Security.sql` (`app_admin`, `app_staff`, `app_cliente`) com `DENY` directo a tabelas + `GRANT EXECUTE` por SP |
| Política de cancelamento com reembolso | Reflectir prática real de coworking | Tabela `politica_cancelamento` (tiers por antecedência); SP `sp_cancelar_reserva_com_reembolso` aplica a tier de maior `horas_minimas ≤ antecedência` |
| Notificações | Feedback ao cliente sobre eventos relevantes | Tabela `notificacao`; triggers T13/T14/T15 emitem automaticamente (criar/cancelar reserva, confirmar pagamento); view `vw_notificacoes_por_ler`; `UcNotificacoes` |
| Lista de espera | Recurso indisponível não deve perder o cliente | Tabela `lista_espera`; SPs `sp_adicionar_lista_espera`, `sp_promover_lista_espera` (cria reserva + actualiza estado + notifica) |
| Reservas recorrentes | Use case comum em coworking | SP `sp_criar_reserva_recorrente` (cria N reservas por dia da semana, com try/catch individual para reportar falhas) |
| Auditoria temporal | Recuperar histórico sem schema adicional | `SYSTEM_VERSIONING = ON` em `adesao`, `reserva`, `pagamento` → tabelas `_history` auto-mantidas; view `vw_reservas_historico` (FOR SYSTEM_TIME ALL) |
| Self-registration de cliente | Visitantes têm de poder criar conta sem intervenção do staff | SP `sp_registar_cliente_completo` cria `cliente` + `utilizador` (role=Cliente) numa transacção atómica. Reusa `sp_register_user` para o hash. Validações: password ≥ 8, username único, NIF/email únicos via UNIQUE constraint. `FormRegister` com auto-login imediato após sucesso |
| Gestão de utilizadores pelo admin | Admin tem de poder criar contas Staff/Admin, resetar passwords e desactivar contas comprometidas | SPs `sp_admin_create_user` (any role, com cliente_id quando Cliente), `sp_admin_reset_password` (sem exigir password antiga), `sp_admin_toggle_user_active` (soft-delete). View `vw_utilizadores_listagem` (utilizador + nome do cliente associado). `UcUtilizadores` visível só a Session.IsAdmin |
| Página de perfil | UX padrão de apps modernas | `UcPerfil` lê dados do utilizador autenticado + cliente associado; permite alterar password via `sp_change_password` (exige password actual) |
