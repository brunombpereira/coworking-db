-- =====================================================================
-- Smoke tests dos triggers T1-T12
-- Cada teste em BEGIN TRAN ... ROLLBACK; usa TRY/CATCH para validar erro.
-- =====================================================================
USE CoworkingDB;
GO
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
GO

DECLARE @passed INT = 0, @failed INT = 0;

-- Helper inline: runs a "should fail" probe; expects @expectedError.
-- (SQL Server não tem CREATE PROCEDURE em batch único limpo — repetimos o padrão.)

-- T1: reserva de sala sobreposta -------------------------------------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, valor, estado, num_participantes)
    VALUES (5, 1, '2026-05-04', '15:00', '17:00', 30.00, 'Pendente', 2);
    ROLLBACK;
    SET @failed += 1; PRINT 'T1 FAIL — não disparou 50001';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50001 BEGIN SET @passed += 1; PRINT 'T1 OK (50001)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T1 FAIL — erro inesperado: ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T2: reserva fora do horário do espaço ------------------------------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, valor, estado, num_participantes)
    VALUES (5, 1, '2026-06-01', '06:00', '08:00', 30.00, 'Pendente', 2);
    ROLLBACK;
    SET @failed += 1; PRINT 'T2 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50002 BEGIN SET @passed += 1; PRINT 'T2 OK (50002)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T2 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T3: num_participantes > capacidade ---------------------------------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, valor, estado, num_participantes)
    VALUES (5, 1, '2026-06-02', '10:00', '12:00', 30.00, 'Pendente', 99);
    ROLLBACK;
    SET @failed += 1; PRINT 'T3 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50003 BEGIN SET @passed += 1; PRINT 'T3 OK (50003)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T3 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T4: 2ª adesão Ativa para o mesmo cliente ---------------------------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado)
    VALUES (1, 1, NULL, '2026-06-01', 120.00, 'Ativa');
    ROLLBACK;
    SET @failed += 1; PRINT 'T4 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50004 BEGIN SET @passed += 1; PRINT 'T4 OK (50004)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T4 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T5: data_fim auto-calculada (verificar fill) -----------------------
BEGIN TRAN;
INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado)
VALUES (5, 2, NULL, '2026-07-01', 330.00, 'Pendente');
DECLARE @df DATE = (SELECT data_fim FROM adesao WHERE cliente_id = 5 AND plano_id = 2);
IF @df = '2026-10-01' BEGIN SET @passed += 1; PRINT 'T5 OK (data_fim=2026-10-01)'; END
ELSE BEGIN SET @failed += 1; PRINT 'T5 FAIL — data_fim=' + ISNULL(CAST(@df AS NVARCHAR(20)),'NULL'); END
ROLLBACK;

-- T6: pagamento com valor errado (reserva) ---------------------------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, reserva_id)
    VALUES (4, 999.99, 'Cartao', 'Pago', 1);
    ROLLBACK;
    SET @failed += 1; PRINT 'T6 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50005 BEGIN SET @passed += 1; PRINT 'T6 OK (50005)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T6 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T7: reserva sobre recurso em Manutencao ----------------------------
BEGIN TRY
    BEGIN TRAN;
    UPDATE sala SET estado = 'Manutencao' WHERE recurso_id = 2;
    INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, valor, estado, num_participantes)
    VALUES (5, 2, '2026-06-10', '10:00', '12:00', 44.00, 'Pendente', 2);
    ROLLBACK;
    SET @failed += 1; PRINT 'T7 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50007 BEGIN SET @passed += 1; PRINT 'T7 OK (50007)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T7 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T8: pagamento com cliente_id inconsistente -------------------------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, reserva_id)
    VALUES (1, 30.00, 'Cartao', 'Pago', 1); -- reserva 1 é do cliente 4
    ROLLBACK;
    SET @failed += 1; PRINT 'T8 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50008 BEGIN SET @passed += 1; PRINT 'T8 OK (50008)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T8 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T9.1: adesão Flex com recurso_id NOT NULL --------------------------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado)
    VALUES (5, 1, 4, '2026-06-01', 120.00, 'Pendente');
    ROLLBACK;
    SET @failed += 1; PRINT 'T9.1 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50009 BEGIN SET @passed += 1; PRINT 'T9.1 OK (50009)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T9.1 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T9.2: adesão Fixo sem recurso_id -----------------------------------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado)
    VALUES (5, 3, NULL, '2026-06-01', 200.00, 'Pendente');
    ROLLBACK;
    SET @failed += 1; PRINT 'T9.2 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50009 BEGIN SET @passed += 1; PRINT 'T9.2 OK (50009)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T9.2 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T9.3: adesão Fixo apontando para sala (recurso_id 1 = Sala A) ------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado)
    VALUES (5, 3, 1, '2026-06-01', 200.00, 'Pendente');
    ROLLBACK;
    SET @failed += 1; PRINT 'T9.3 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50009 BEGIN SET @passed += 1; PRINT 'T9.3 OK (50009)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T9.3 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T9.4: adesão Fixo apontando para posto Flex (recurso_id 4) ---------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado)
    VALUES (5, 3, 4, '2026-06-01', 200.00, 'Pendente');
    ROLLBACK;
    SET @failed += 1; PRINT 'T9.4 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50009 BEGIN SET @passed += 1; PRINT 'T9.4 OK (50009)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T9.4 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T9.5: 2ª adesão Fixo no mesmo posto sobreposta ---------------------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado)
    VALUES (5, 3, 6, '2026-04-20', 200.00, 'Pendente');
    ROLLBACK;
    SET @failed += 1; PRINT 'T9.5 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50009 BEGIN SET @passed += 1; PRINT 'T9.5 OK (50009)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T9.5 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T10: snapshot preco_acordado quando NULL ---------------------------
-- (NÃO PASSA — o CHECK NOT NULL impede o NULL chegar à tabela; INSTEAD OF preenche.)
BEGIN TRAN;
DECLARE @sn DECIMAL(10,2);
INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado)
VALUES (5, 1, NULL, '2026-08-01', NULL, 'Pendente');
SET @sn = (SELECT preco_acordado FROM adesao WHERE cliente_id = 5 AND plano_id = 1 AND data_inicio = '2026-08-01');
IF @sn = 120.00 BEGIN SET @passed += 1; PRINT 'T10 OK (snapshot 120.00)'; END
ELSE BEGIN SET @failed += 1; PRINT 'T10 FAIL — preco=' + ISNULL(CAST(@sn AS NVARCHAR(20)),'NULL'); END
ROLLBACK;

-- T11: cliente Flex Ativa + reserva avulsa Flex no mesmo dia ---------
-- Cliente 1 (Ana) tem adesão Flex Ativa de 2026-04-01 a 2026-05-01.
-- Data 2026-04-15 cai dentro do período da adesão.
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO reserva (cliente_id, recurso_id, data_reserva, valor, estado)
    VALUES (1, 4, '2026-04-15', 12.00, 'Pendente');
    ROLLBACK;
    SET @failed += 1; PRINT 'T11 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50011 BEGIN SET @passed += 1; PRINT 'T11 OK (50011)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T11 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T12.1: reserva de sala com horas NULL ------------------------------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO reserva (cliente_id, recurso_id, data_reserva, valor, estado)
    VALUES (5, 1, '2026-06-15', 30.00, 'Pendente');
    ROLLBACK;
    SET @failed += 1; PRINT 'T12.1 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50012 BEGIN SET @passed += 1; PRINT 'T12.1 OK (50012)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T12.1 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

-- T12.2: reserva de posto com horas NOT NULL -------------------------
BEGIN TRY
    BEGIN TRAN;
    INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, valor, estado)
    VALUES (5, 4, '2026-06-15', '10:00', '12:00', 12.00, 'Pendente');
    ROLLBACK;
    SET @failed += 1; PRINT 'T12.2 FAIL';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK;
    IF ERROR_NUMBER() = 50012 BEGIN SET @passed += 1; PRINT 'T12.2 OK (50012)'; END
    ELSE BEGIN SET @failed += 1; PRINT 'T12.2 FAIL — ' + CAST(ERROR_NUMBER() AS NVARCHAR(20)); END
END CATCH

PRINT '---';
PRINT 'Passou: ' + CAST(@passed AS NVARCHAR(10)) + ' / Falhou: ' + CAST(@failed AS NVARCHAR(10));
GO
