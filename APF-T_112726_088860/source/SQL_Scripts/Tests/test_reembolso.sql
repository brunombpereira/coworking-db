-- =====================================================================
-- test_reembolso.sql — política de cancelamento
-- TC19, TC20
-- =====================================================================
USE CoworkingDB;
GO

SET NOCOUNT ON;

-- Seed da política se ainda não existir
IF NOT EXISTS (SELECT 1 FROM politica_cancelamento)
BEGIN
    INSERT INTO politica_cancelamento (nome, horas_minimas, perc_reembolso) VALUES
        (N'>=48h: 100%',  48, 100.00),
        (N'>=24h:  50%',  24,  50.00),
        (N'>=0h:    0%',   0,   0.00);
END
GO

PRINT N'--- TC19: cancelar reserva daqui a 3 dias -> 100% reembolso ---';
DECLARE @rid INT, @reembolso DECIMAL(10,2);
EXEC sp_criar_reserva_sala
     @cliente_id = 5, @recurso_id = 2,
     @data_reserva = DATEADD(DAY, 3, CAST(SYSDATETIME() AS DATE)),
     @hora_inicio = '10:00', @hora_fim = '12:00',
     @reserva_id = @rid OUTPUT;
EXEC sp_registar_pagamento 5, 44.00, 'Cartao', NULL, @rid, @pagamento_id = @rid OUTPUT;

-- recapturar reserva_id correto
SELECT TOP 1 @rid = reserva_id FROM reserva
WHERE cliente_id = 5 ORDER BY reserva_id DESC;

EXEC sp_cancelar_reserva_com_reembolso @rid, @valor_reembolso = @reembolso OUTPUT;
PRINT CONCAT(N'reembolso esperado=44.00, obtido=', @reembolso);

PRINT N'--- TC20: cancelar reserva daqui a 6h -> 0% reembolso ---';
DECLARE @rid2 INT, @reembolso2 DECIMAL(10,2);
EXEC sp_criar_reserva_sala
     @cliente_id = 5, @recurso_id = 2,
     @data_reserva = CAST(SYSDATETIME() AS DATE),
     @hora_inicio = CAST(DATEADD(HOUR, 6, SYSDATETIME()) AS TIME),
     @hora_fim    = CAST(DATEADD(HOUR, 7, SYSDATETIME()) AS TIME),
     @reserva_id = @rid2 OUTPUT;

SELECT TOP 1 @rid2 = reserva_id FROM reserva
WHERE cliente_id = 5 ORDER BY reserva_id DESC;

EXEC sp_cancelar_reserva_com_reembolso @rid2, @valor_reembolso = @reembolso2 OUTPUT;
PRINT CONCAT(N'reembolso esperado=0, obtido=', @reembolso2);
GO
