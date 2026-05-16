-- =====================================================================
-- test_concorrencia.sql — sp_getapplock em sp_criar_reserva_sala
-- TC16: executar em 2 sessões SSMS em paralelo.
--
-- Sessão A (correr este bloco primeiro, mas NÃO commitar logo):
-- =====================================================================
USE CoworkingDB;
GO

BEGIN TRANSACTION;

DECLARE @lock_rc INT;
EXEC @lock_rc = sp_getapplock
    @Resource    = 'reserva_recurso_1_2026-06-01',
    @LockMode    = 'Exclusive',
    @LockOwner   = 'Transaction',
    @LockTimeout = 5000;

PRINT CONCAT(N'Sessão A obteve lock (rc=', @lock_rc, N'). Aguardar 15s antes de commitar...');
WAITFOR DELAY '00:00:15';

INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, valor, estado)
VALUES (1, 1, '2026-06-01', '10:00', '12:00', 30.00, 'Pendente');

COMMIT;
PRINT N'Sessão A commitou.';

-- =====================================================================
-- Sessão B (correr em paralelo enquanto A tem o lock):
-- O EXEC sp_criar_reserva_sala vai bloquear até A commitar; quando
-- destrancado, o trigger T1 deteta sobreposição e faz ROLLBACK com 50001.
-- =====================================================================
-- DECLARE @rid INT;
-- EXEC sp_criar_reserva_sala
--      @cliente_id = 2, @recurso_id = 1, @data_reserva = '2026-06-01',
--      @hora_inicio = '10:30', @hora_fim = '11:30',
--      @reserva_id = @rid OUTPUT;
-- PRINT CONCAT(N'Sessão B reserva_id = ', @rid);
