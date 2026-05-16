-- =====================================================================
-- test_temporal.sql — SYSTEM_VERSIONING em adesao
-- TC17, TC18
-- =====================================================================
USE CoworkingDB;
GO

SET NOCOUNT ON;
PRINT N'--- TC17: 2 versões depois de UPDATE ---';

DECLARE @snapshot_before DATETIME2 = SYSUTCDATETIME();

UPDATE adesao SET estado = 'Suspensa' WHERE adesao_id = 2;
WAITFOR DELAY '00:00:01';
UPDATE adesao SET estado = 'Ativa'    WHERE adesao_id = 2;

SELECT 'Versão atual' AS fase, adesao_id, estado FROM adesao WHERE adesao_id = 2
UNION ALL
SELECT 'Histórico',    adesao_id, estado
FROM   adesao FOR SYSTEM_TIME ALL
WHERE  adesao_id = 2
ORDER BY fase;
GO

PRINT N'--- TC18: AS OF antes da alteração ---';
-- Usar uma data anterior ao primeiro UPDATE (último seed estava 'Ativa').
SELECT adesao_id, estado, preco_acordado
FROM   adesao FOR SYSTEM_TIME AS OF '2026-05-15T00:00:00'
WHERE  adesao_id = 2;
GO
