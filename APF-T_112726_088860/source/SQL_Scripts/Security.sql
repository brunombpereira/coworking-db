-- =====================================================================
-- Security — DB roles + GRANTs
-- Estratégia: a aplicação liga-se com um login que pertence a uma das
-- três roles (app_cliente, app_staff, app_admin). Nenhuma role tem
-- acesso direto às tabelas — todas as operações passam por SPs/UDFs.
-- Isto protege contra SQL injection e centraliza a regra de negócio.
-- =====================================================================
USE CoworkingDB;
GO

-- =====================================================================
-- 1) Roles
-- =====================================================================
IF DATABASE_PRINCIPAL_ID('app_cliente') IS NULL CREATE ROLE app_cliente;
IF DATABASE_PRINCIPAL_ID('app_staff')   IS NULL CREATE ROLE app_staff;
IF DATABASE_PRINCIPAL_ID('app_admin')   IS NULL CREATE ROLE app_admin;
GO

-- =====================================================================
-- 2) Negar acesso direto a tabelas (defesa em profundidade)
-- =====================================================================
DENY SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO app_cliente;
DENY SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO app_staff;
-- app_admin pode tudo (a seguir o GRANT cobre o que precisamos).
GO

-- =====================================================================
-- 3) GRANTs por role
-- =====================================================================

-- app_cliente: pode autenticar-se, ver/cancelar as suas reservas,
-- registar-se em lista de espera, ver notificações.
GRANT EXECUTE ON sp_login_user             TO app_cliente;
GRANT EXECUTE ON sp_change_password        TO app_cliente;
GRANT EXECUTE ON sp_criar_reserva_sala     TO app_cliente;
GRANT EXECUTE ON sp_criar_reserva_posto    TO app_cliente;
GRANT EXECUTE ON sp_cancelar_reserva       TO app_cliente;
GRANT EXECUTE ON sp_cancelar_reserva_com_reembolso TO app_cliente;
GRANT EXECUTE ON sp_adicionar_lista_espera TO app_cliente;
GRANT EXECUTE ON sp_marcar_notificacao_lida TO app_cliente;
GRANT SELECT ON vw_reservas_ativas         TO app_cliente;
GRANT SELECT ON vw_notificacoes_por_ler    TO app_cliente;
GRANT SELECT ON vw_adesoes_a_expirar       TO app_cliente;
GO

-- app_staff: tudo do cliente + gestão operacional.
GRANT EXECUTE ON sp_registar_cliente            TO app_staff;
GRANT EXECUTE ON sp_register_user               TO app_staff;
GRANT EXECUTE ON sp_criar_adesao                TO app_staff;
GRANT EXECUTE ON sp_cancelar_adesao             TO app_staff;
GRANT EXECUTE ON sp_criar_reserva_sala          TO app_staff;
GRANT EXECUTE ON sp_criar_reserva_posto         TO app_staff;
GRANT EXECUTE ON sp_criar_reserva_recorrente    TO app_staff;
GRANT EXECUTE ON sp_cancelar_reserva            TO app_staff;
GRANT EXECUTE ON sp_cancelar_reserva_com_reembolso TO app_staff;
GRANT EXECUTE ON sp_registar_pagamento          TO app_staff;
GRANT EXECUTE ON sp_promover_lista_espera       TO app_staff;
GRANT EXECUTE ON sp_relatorio_receita_periodo   TO app_staff;
GRANT EXECUTE ON sp_login_user                  TO app_staff;
GRANT EXECUTE ON sp_change_password             TO app_staff;
GRANT EXECUTE ON sp_marcar_notificacao_lida     TO app_staff;
GO

-- app_staff: SELECT em todas as views de consulta.
GRANT SELECT ON vw_reservas_ativas              TO app_staff;
GRANT SELECT ON vw_ocupacao_recurso             TO app_staff;
GRANT SELECT ON vw_receita_por_plano            TO app_staff;
GRANT SELECT ON vw_receita_mensal               TO app_staff;
GRANT SELECT ON vw_clientes_com_adesao_ativa    TO app_staff;
GRANT SELECT ON vw_pagamentos_pendentes         TO app_staff;
GRANT SELECT ON vw_top_clientes_receita         TO app_staff;
GRANT SELECT ON vw_receita_por_espaco_mes       TO app_staff;
GRANT SELECT ON vw_receita_por_metodo           TO app_staff;
GRANT SELECT ON vw_adesoes_a_expirar            TO app_staff;
GRANT SELECT ON vw_notificacoes_por_ler         TO app_staff;
GRANT SELECT ON vw_reservas_historico           TO app_staff;
GO

-- app_staff: pode invocar UDFs.
GRANT SELECT ON OBJECT::fn_recurso_disponivel       TO app_staff;
GRANT SELECT ON OBJECT::fn_receita_cliente          TO app_staff;
GRANT SELECT ON OBJECT::fn_reservas_cliente_periodo TO app_staff;
GRANT SELECT ON OBJECT::fn_taxa_ocupacao_espaco     TO app_staff;
GRANT SELECT ON OBJECT::fn_calc_reembolso           TO app_staff;
GRANT SELECT ON OBJECT::fn_receita_periodo          TO app_staff;
GRANT SELECT ON OBJECT::fn_clientes_inativos        TO app_staff;
GO

-- app_admin: tudo (DDL + DML direto, para administração).
GRANT CONTROL ON DATABASE::CoworkingDB TO app_admin;
GO

-- =====================================================================
-- 4) Logins/users de exemplo (executar apenas em dev, password trocar!)
-- =====================================================================
-- USE master;
-- CREATE LOGIN cowork_app WITH PASSWORD = 'TROCAR_ESTA_PASSWORD!2026';
-- USE CoworkingDB;
-- CREATE USER cowork_app FOR LOGIN cowork_app;
-- ALTER ROLE app_staff ADD MEMBER cowork_app;
-- A connection string da app passa a ser:
--   Server=.\SQLEXPRESS;Database=CoworkingDB;User Id=cowork_app;Password=...;
GO
