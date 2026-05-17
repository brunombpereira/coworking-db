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

## 10. Conclusão

O sistema cobre o domínio com 9 tabelas core + 4 de extensão (auth, política, notificação, lista de espera), com regras enforçadas a 3 níveis: constraints SQL (cardinalidade/domínio), triggers (regras transversais), SPs (concorrência + lógica composta). A camada de segurança via roles + SPs torna a aplicação resistente a SQL injection by design, e a auditoria via temporal tables dá-nos histórico para sempre sem trigger adicional.

**Limitações conhecidas:**
- Notificações ficam só na tabela; envio efetivo de email exigiria SQL Agent + Database Mail configurados fora do scope da disciplina.
- O cálculo de reembolso assume que a política está seedada com tiers cobrindo 0h em diante; falta UI para gerir tiers.
- O sistema não suporta multi-tenancy (uma só CoworkingDB serve um operador).

**Próximos passos naturais:** dashboard de KPIs em SSRS, UI de gestão de políticas, integração com gateway de pagamento real.
