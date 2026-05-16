-- =====================================================================
-- test_lista_espera.sql — TC21, TC22
-- =====================================================================
USE CoworkingDB;
GO

SET NOCOUNT ON;

PRINT N'--- TC21: 2 entradas para mesmo cliente/recurso/data -> 50016 ---';
DECLARE @le1 INT, @le2 INT;
EXEC sp_adicionar_lista_espera 5, 1, '2026-07-01', '10:00', '12:00', @le1 OUTPUT;
PRINT CONCAT(N'1ª entrada lista_espera_id=', @le1);

BEGIN TRY
    EXEC sp_adicionar_lista_espera 5, 1, '2026-07-01', '14:00', '16:00', @le2 OUTPUT;
    PRINT N'FALHA: deveria ter lançado 50016';
END TRY
BEGIN CATCH
    PRINT CONCAT(N'OK: ', ERROR_NUMBER(), N' ', ERROR_MESSAGE());
END CATCH

PRINT N'--- TC22: promover entrada -> cria reserva, atualiza estado, envia notificação ---';
DECLARE @rid INT;
EXEC sp_promover_lista_espera @le1, @rid OUTPUT;
PRINT CONCAT(N'Reserva criada reserva_id=', @rid);

SELECT lista_espera_id, estado, reserva_id FROM lista_espera WHERE lista_espera_id = @le1;
SELECT TOP 5 notificacao_id, tipo, assunto FROM notificacao
WHERE cliente_id = 5 ORDER BY notificacao_id DESC;
GO
