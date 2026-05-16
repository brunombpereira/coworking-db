-- =====================================================================
-- Backup & Restore — CoworkingDB
-- Estratégia recomendada (recovery model FULL):
--   - FULL diário às 02:00
--   - DIFFERENCIAL de 6 em 6 horas
--   - LOG de 15 em 15 minutos (suporta point-in-time recovery)
--
-- Path por omissão: C:\BD\Backups\ — criar pasta antes da 1ª execução.
-- =====================================================================
USE master;
GO

-- Garantir recovery model FULL (necessário para backups de log) -------
ALTER DATABASE CoworkingDB SET RECOVERY FULL;
GO

-- BACKUP FULL ---------------------------------------------------------
BACKUP DATABASE CoworkingDB
TO DISK = N'C:\BD\Backups\CoworkingDB_FULL.bak'
WITH FORMAT,
     INIT,
     NAME = N'CoworkingDB-Full Backup',
     STATS = 10,
     CHECKSUM;
GO

-- BACKUP DIFERENCIAL --------------------------------------------------
BACKUP DATABASE CoworkingDB
TO DISK = N'C:\BD\Backups\CoworkingDB_DIFF.bak'
WITH DIFFERENTIAL,
     INIT,
     NAME = N'CoworkingDB-Diff Backup',
     STATS = 10,
     CHECKSUM;
GO

-- BACKUP DO TRANSACTION LOG ------------------------------------------
BACKUP LOG CoworkingDB
TO DISK = N'C:\BD\Backups\CoworkingDB_LOG.trn'
WITH INIT,
     NAME = N'CoworkingDB-Log Backup',
     STATS = 10,
     CHECKSUM;
GO

-- =====================================================================
-- RESTORE — exemplo de point-in-time recovery
-- (executar com a BD offline / sem outras ligações ativas)
-- =====================================================================
-- Passo 1: restore do FULL com NORECOVERY
-- RESTORE DATABASE CoworkingDB
-- FROM DISK = N'C:\BD\Backups\CoworkingDB_FULL.bak'
-- WITH NORECOVERY, REPLACE;

-- Passo 2: restore do DIFF mais recente com NORECOVERY
-- RESTORE DATABASE CoworkingDB
-- FROM DISK = N'C:\BD\Backups\CoworkingDB_DIFF.bak'
-- WITH NORECOVERY;

-- Passo 3: restore dos LOGs até ao momento desejado
-- RESTORE LOG CoworkingDB
-- FROM DISK = N'C:\BD\Backups\CoworkingDB_LOG.trn'
-- WITH STOPAT = '2026-05-16T14:30:00', RECOVERY;
GO

-- =====================================================================
-- Verificação de integridade do backup (sem restaurar)
-- =====================================================================
RESTORE VERIFYONLY FROM DISK = N'C:\BD\Backups\CoworkingDB_FULL.bak';
GO

-- =====================================================================
-- DBCC CHECKDB — verificação periódica de consistência (semanal)
-- =====================================================================
DBCC CHECKDB ('CoworkingDB') WITH NO_INFOMSGS;
GO
