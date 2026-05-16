-- =====================================================================
-- Drop de objetos da CoworkingDB
-- Ordem: views/funções/procedures/triggers -> tabelas filho -> tabelas pai
-- =====================================================================
USE CoworkingDB;
GO

-- Roles ---------------------------------------------------------------
-- (drop dos membros antes da role; aqui só largamos a role se vazia)
IF DATABASE_PRINCIPAL_ID('app_cliente') IS NOT NULL DROP ROLE app_cliente;
IF DATABASE_PRINCIPAL_ID('app_staff')   IS NOT NULL DROP ROLE app_staff;
IF DATABASE_PRINCIPAL_ID('app_admin')   IS NOT NULL DROP ROLE app_admin;
GO

-- Views ---------------------------------------------------------------
DROP VIEW IF EXISTS vw_reservas_ativas;
DROP VIEW IF EXISTS vw_ocupacao_recurso;
DROP VIEW IF EXISTS vw_receita_por_plano;
DROP VIEW IF EXISTS vw_receita_mensal;
DROP VIEW IF EXISTS vw_clientes_com_adesao_ativa;
DROP VIEW IF EXISTS vw_pagamentos_pendentes;
DROP VIEW IF EXISTS vw_top_clientes_receita;
DROP VIEW IF EXISTS vw_receita_por_espaco_mes;
DROP VIEW IF EXISTS vw_receita_por_metodo;
DROP VIEW IF EXISTS vw_adesoes_a_expirar;
DROP VIEW IF EXISTS vw_reservas_historico;
DROP VIEW IF EXISTS vw_notificacoes_por_ler;
GO

-- Funções -------------------------------------------------------------
DROP FUNCTION IF EXISTS fn_recurso_disponivel;
DROP FUNCTION IF EXISTS fn_receita_cliente;
DROP FUNCTION IF EXISTS fn_reservas_cliente_periodo;
DROP FUNCTION IF EXISTS fn_taxa_ocupacao_espaco;
DROP FUNCTION IF EXISTS fn_calc_reembolso;
DROP FUNCTION IF EXISTS fn_receita_periodo;
DROP FUNCTION IF EXISTS fn_clientes_inativos;
GO

-- Stored procedures ---------------------------------------------------
DROP PROCEDURE IF EXISTS sp_registar_cliente;
DROP PROCEDURE IF EXISTS sp_criar_adesao;
DROP PROCEDURE IF EXISTS sp_cancelar_adesao;
DROP PROCEDURE IF EXISTS sp_criar_reserva_sala;
DROP PROCEDURE IF EXISTS sp_criar_reserva_posto;
DROP PROCEDURE IF EXISTS sp_criar_reserva_recorrente;
DROP PROCEDURE IF EXISTS sp_cancelar_reserva;
DROP PROCEDURE IF EXISTS sp_cancelar_reserva_com_reembolso;
DROP PROCEDURE IF EXISTS sp_registar_pagamento;
DROP PROCEDURE IF EXISTS sp_relatorio_receita_periodo;
DROP PROCEDURE IF EXISTS sp_adicionar_lista_espera;
DROP PROCEDURE IF EXISTS sp_promover_lista_espera;
DROP PROCEDURE IF EXISTS sp_marcar_notificacao_lida;
DROP PROCEDURE IF EXISTS sp_register_user;
DROP PROCEDURE IF EXISTS sp_login_user;
DROP PROCEDURE IF EXISTS sp_change_password;
DROP PROCEDURE IF EXISTS sp_desativar_utilizador;
GO

-- Triggers ------------------------------------------------------------
DROP TRIGGER IF EXISTS trg_reserva_sem_sobreposicao;
DROP TRIGGER IF EXISTS trg_reserva_horario_espaco;
DROP TRIGGER IF EXISTS trg_reserva_capacidade;
DROP TRIGGER IF EXISTS trg_adesao_ativa_unica;
DROP TRIGGER IF EXISTS trg_adesao_data_fim;
DROP TRIGGER IF EXISTS trg_pagamento_valor_correto;
DROP TRIGGER IF EXISTS trg_reserva_recurso_disponivel;
DROP TRIGGER IF EXISTS trg_pagamento_cliente_consistente;
DROP TRIGGER IF EXISTS trg_adesao_recurso_coerente;
DROP TRIGGER IF EXISTS trg_adesao_preco_snapshot;
DROP TRIGGER IF EXISTS trg_reserva_posto_sem_adesao;
DROP TRIGGER IF EXISTS trg_reserva_horas_coerentes;
DROP TRIGGER IF EXISTS trg_reserva_notificacao;
DROP TRIGGER IF EXISTS trg_reserva_cancelada_notificacao;
DROP TRIGGER IF EXISTS trg_pagamento_confirmado_notificacao;
DROP TRIGGER IF EXISTS trg_lista_espera_unica;
GO

-- Temporal: desligar SYSTEM_VERSIONING antes de dropar as tabelas ----
IF OBJECT_ID('adesao','U') IS NOT NULL
    AND OBJECTPROPERTY(OBJECT_ID('adesao','U'),'TableTemporalType') = 2
    ALTER TABLE adesao SET (SYSTEM_VERSIONING = OFF);
IF OBJECT_ID('reserva','U') IS NOT NULL
    AND OBJECTPROPERTY(OBJECT_ID('reserva','U'),'TableTemporalType') = 2
    ALTER TABLE reserva SET (SYSTEM_VERSIONING = OFF);
IF OBJECT_ID('pagamento','U') IS NOT NULL
    AND OBJECTPROPERTY(OBJECT_ID('pagamento','U'),'TableTemporalType') = 2
    ALTER TABLE pagamento SET (SYSTEM_VERSIONING = OFF);
GO

-- Tabelas (ordem inversa das dependências) ----------------------------
DROP TABLE IF EXISTS notificacao;
DROP TABLE IF EXISTS lista_espera;
DROP TABLE IF EXISTS politica_cancelamento;
DROP TABLE IF EXISTS utilizador;
DROP TABLE IF EXISTS pagamento;
DROP TABLE IF EXISTS pagamento_history;
DROP TABLE IF EXISTS reserva;
DROP TABLE IF EXISTS reserva_history;
DROP TABLE IF EXISTS adesao;
DROP TABLE IF EXISTS adesao_history;
DROP TABLE IF EXISTS posto;
DROP TABLE IF EXISTS sala;
DROP TABLE IF EXISTS recurso;
DROP TABLE IF EXISTS espaco;
DROP TABLE IF EXISTS cliente;
DROP TABLE IF EXISTS plano;
GO
