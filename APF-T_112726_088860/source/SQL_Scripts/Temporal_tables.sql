-- =====================================================================
-- Temporal Tables (SYSTEM_VERSIONED) — CoworkingDB
-- Mantém histórico completo de alterações em adesao, reserva e pagamento.
-- Cada UPDATE/DELETE escreve a versão anterior na tabela <nome>_history,
-- permitindo consultas AS OF, BETWEEN, FROM/TO, CONTAINED IN, ALL.
--
-- Pré-requisito: NÃO podem existir triggers INSTEAD OF nas tabelas.
-- O trigger trg_adesao_preco_snapshot foi removido — a lógica passou
-- para o SP sp_criar_adesao.
-- =====================================================================
USE CoworkingDB;
GO

-- adesao --------------------------------------------------------------
ALTER TABLE adesao
ADD valid_from DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
        CONSTRAINT df_adesao_valid_from DEFAULT SYSUTCDATETIME(),
    valid_to   DATETIME2 GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL
        CONSTRAINT df_adesao_valid_to   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME (valid_from, valid_to);
GO

ALTER TABLE adesao
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.adesao_history));
GO

-- reserva -------------------------------------------------------------
ALTER TABLE reserva
ADD valid_from DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
        CONSTRAINT df_reserva_valid_from DEFAULT SYSUTCDATETIME(),
    valid_to   DATETIME2 GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL
        CONSTRAINT df_reserva_valid_to   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME (valid_from, valid_to);
GO

ALTER TABLE reserva
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.reserva_history));
GO

-- pagamento -----------------------------------------------------------
ALTER TABLE pagamento
ADD valid_from DATETIME2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL
        CONSTRAINT df_pagamento_valid_from DEFAULT SYSUTCDATETIME(),
    valid_to   DATETIME2 GENERATED ALWAYS AS ROW END   HIDDEN NOT NULL
        CONSTRAINT df_pagamento_valid_to   DEFAULT CONVERT(DATETIME2, '9999-12-31 23:59:59.9999999'),
    PERIOD FOR SYSTEM_TIME (valid_from, valid_to);
GO

ALTER TABLE pagamento
    SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = dbo.pagamento_history));
GO

-- =====================================================================
-- Views dependentes de SYSTEM_VERSIONING
-- =====================================================================
-- Definida aqui (e não em Views.sql) porque FOR SYSTEM_TIME exige que a
-- tabela já esteja system-versioned. GRANT em Security.sql.
GO
CREATE OR ALTER VIEW vw_reservas_historico
AS
SELECT
    reserva_id,
    cliente_id,
    recurso_id,
    data_reserva,
    estado,
    valor
FROM reserva
FOR SYSTEM_TIME ALL;
GO

-- =====================================================================
-- Exemplos de consultas temporais (apenas referência — não executam aqui)
-- =====================================================================
-- Estado da tabela num momento passado:
--   SELECT * FROM adesao FOR SYSTEM_TIME AS OF '2026-05-01T00:00:00';
--
-- Histórico de uma adesão específica:
--   SELECT * FROM adesao FOR SYSTEM_TIME ALL
--   WHERE adesao_id = 2 ORDER BY valid_from;
--
-- Diferenças entre dois momentos:
--   SELECT * FROM reserva FOR SYSTEM_TIME BETWEEN '2026-05-01' AND '2026-05-31'
--   WHERE recurso_id = 1;
GO
