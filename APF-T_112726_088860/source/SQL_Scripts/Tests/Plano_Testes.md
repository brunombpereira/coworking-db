# Plano de Testes — CoworkingDB

Plano de testes da base de dados. Cobre constraints, triggers, stored procedures e funcionalidades transversais (auth, concorrência, temporal).

## Como correr

```sql
-- Reset completo (opcional)
:r Drop_tables.sql
:r SQL_DDL.sql
:r Indexes.sql
:r Triggers.sql
:r Views.sql
:r User_defined_functions.sql
:r Stored_procedures.sql
:r Auth.sql
:r Temporal_tables.sql
:r SQL_DML.sql

-- Bateria de testes
:r Tests/smoke_triggers.sql           -- T1..T12 (já existente)
:r Tests/test_auth.sql                -- registo/login
:r Tests/test_concorrencia.sql        -- sp_getapplock
:r Tests/test_temporal.sql            -- FOR SYSTEM_TIME
:r Tests/test_reembolso.sql           -- política cancelamento
:r Tests/test_lista_espera.sql        -- adicionar + promover
:r Tests/test_recorrente.sql          -- sp_criar_reserva_recorrente
```

## Casos de teste

| # | Categoria | Caso | Resultado esperado | Script |
|---|---|---|---|---|
| TC01 | Trigger T1 | Inserir 2 reservas sobrepostas na mesma sala | `THROW 50001` | smoke_triggers.sql |
| TC02 | Trigger T2 | Reserva 06:00–08:00 num espaço que abre 08:00 | `THROW 50002` | smoke_triggers.sql |
| TC03 | Trigger T3 | Reserva com 20 participantes em sala capacidade 8 | `THROW 50003` | smoke_triggers.sql |
| TC04 | Trigger T4 | Cliente com adesão ativa cria 2ª ativa | `THROW 50004` | smoke_triggers.sql |
| TC05 | Trigger T5 | INSERT adesão com data_fim NULL | data_fim preenchida automaticamente | smoke_triggers.sql |
| TC06 | Trigger T6 | Pagamento com valor diferente da reserva | `THROW 50005` | smoke_triggers.sql |
| TC07 | Trigger T7 | Reservar recurso em Manutencao | `THROW 50007` | smoke_triggers.sql |
| TC08 | Trigger T8 | Pagamento cuja `cliente_id` ≠ titular reserva | `THROW 50008` | smoke_triggers.sql |
| TC09 | Trigger T9 | Adesão Flex com `recurso_id` atribuído | `THROW 50009` | smoke_triggers.sql |
| TC10 | Trigger T11 | Cliente com adesão Flex Ativa reserva posto Flex no mesmo dia | `THROW 50011` | smoke_triggers.sql |
| TC11 | Trigger T12 | Reserva de posto com horas preenchidas | `THROW 50012` | smoke_triggers.sql |
| TC12 | Auth | sp_register_user com username duplicado | `THROW 52001` | test_auth.sql |
| TC13 | Auth | sp_login_user com password errada | resultset vazio | test_auth.sql |
| TC14 | Auth | sp_login_user com credenciais válidas | retorna utilizador_id, role, cliente_id | test_auth.sql |
| TC15 | Auth | sp_change_password com password atual errada | `THROW 52003` | test_auth.sql |
| TC16 | Concorrência | 2 sessões SSMS inserem reserva no mesmo recurso/horário | uma falha com T1 (a 2ª aguarda lock) | test_concorrencia.sql (manual) |
| TC17 | Temporal | UPDATE adesão e ler FOR SYSTEM_TIME ALL | aparecem 2 versões | test_temporal.sql |
| TC18 | Temporal | Consulta AS OF data anterior à alteração | devolve estado antigo | test_temporal.sql |
| TC19 | Reembolso | Cancelar reserva com >48h antecedência | reembolso = 100% do valor | test_reembolso.sql |
| TC20 | Reembolso | Cancelar reserva <24h | reembolso = 0% | test_reembolso.sql |
| TC21 | Lista espera | Adicionar 2 entradas iguais para o mesmo cliente | 2ª falha `THROW 50016` | test_lista_espera.sql |
| TC22 | Lista espera | Promover entrada cria reserva e atualiza estado | estado='Promovido', reserva criada, notificação enviada | test_lista_espera.sql |
| TC23 | Recorrente | sp_criar_reserva_recorrente terça 14h–16h durante 4 semanas | 4 reservas criadas com sucesso | test_recorrente.sql |
| TC24 | Segurança | app_cliente tenta `SELECT * FROM cliente` | erro de permissão (DENY) | (manual com user app_cliente) |
| TC25 | Segurança | app_cliente executa sp_criar_reserva_sala | sucesso | (manual com user app_cliente) |

## Critérios de aceitação

- Todos os THROWs documentados disparam exatamente uma vez.
- Casos positivos (TC05, TC14, TC17–TC23, TC25) executam sem erro e produzem o efeito esperado.
- Casos manuais (TC16, TC24, TC25) requerem 2 sessões SSMS e/ou login com user dedicado — ver instruções no fim de cada script.
