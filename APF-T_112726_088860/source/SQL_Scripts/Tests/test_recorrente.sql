-- =====================================================================
-- test_recorrente.sql — TC23
-- Reservas terça (DATEFIRST 7 -> terça = 3), 14:00-16:00 durante 4 semanas
-- =====================================================================
USE CoworkingDB;
GO

EXEC sp_criar_reserva_recorrente
    @cliente_id  = 5,
    @recurso_id  = 3,
    @dia_semana  = 3,
    @hora_inicio = '14:00',
    @hora_fim    = '16:00',
    @data_inicio = '2026-09-01',
    @data_fim    = '2026-09-30',
    @num_participantes = 4;
GO
