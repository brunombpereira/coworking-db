-- =====================================================================
-- Projeto: Sistema de Gestão de Coworking — APF-T
-- DML (seed) — alinhado com o schema redesenhado
-- Pré-requisito: DDL já executado (CoworkingDB criada de raiz).
-- Para re-seed: executar DDL primeiro para garantir IDENTITY limpo.
-- =====================================================================
SET QUOTED_IDENTIFIER ON;
GO
USE CoworkingDB;
GO

-- planos (5)  →  plano_id = 1..5
INSERT INTO plano (nome_plano, tipo_plano, preco_mensal, duracao_meses, descricao) VALUES
 ('Flex Mensal',     'Flex',     120.00, 1,  'Acesso livre a postos Flex, sem reserva.'),
 ('Flex Trimestral', 'Flex',     330.00, 3,  '3 meses de acesso livre a postos Flex.'),
 ('Fixo Mensal',     'Fixo',     200.00, 1,  'Posto Fixo atribuído ao cliente.'),
 ('Fixo Anual',      'Fixo',    2000.00, 12, '12 meses, posto Fixo atribuído.'),
 ('Privado Mensal',  'Privado',  350.00, 1,  'Posto Privado em sala fechada.');
GO

-- espacos (2)  →  espaco_id = 1, 2
INSERT INTO espaco (nome, morada, telefone, email, hora_abertura, hora_fecho) VALUES
 ('Coworking Aveiro Centro',  'Rua Direita 100, Aveiro', '234111222', 'aveiro@cowork.pt', '08:00', '20:00'),
 ('Coworking Porto Boavista', 'Av. Boavista 500, Porto', '225333444', 'porto@cowork.pt',  '07:00', '22:00');
GO

-- clientes (5)  →  cliente_id = 1..5
INSERT INTO cliente (nome, nif, email, telefone, data_registo) VALUES
 ('Ana Silva',     '123456789', 'ana@example.com',   '912000001', '2026-01-10'),
 ('Bruno Pereira', '102345679', 'bruno@example.com', '912000002', '2026-02-15'),
 ('Carla Mendes',  '234567891', 'carla@example.com', '912000003', '2026-03-01'),
 ('Diogo Santos',  '345678912', 'diogo@example.com', '912000004', '2026-03-12'),
 ('Eva Costa',     '456789123', 'eva@example.com',   '912000005', '2026-04-01');
GO

-- recurso supertype + sala/posto subtypes
-- Inserir um recurso de cada vez, capturar o IDENTITY com SCOPE_IDENTITY().
-- recurso_id resultante:
--   1 = Sala A          (espaco 1)
--   2 = Sala B          (espaco 1)
--   3 = Sala Boavista 1 (espaco 2)
--   4 = AV-F01  Flex    (espaco 1)
--   5 = AV-F02  Flex    (espaco 1)
--   6 = AV-FX1  Fixo    (espaco 1)
--   7 = PT-PV1  Privado (espaco 2)
DECLARE @rid INT;

-- Salas
INSERT INTO recurso (tipo) VALUES ('Sala'); SET @rid = SCOPE_IDENTITY();
INSERT INTO sala (recurso_id, espaco_id, nome, capacidade, preco_hora, estado)
VALUES (@rid, 1, 'Sala A', 8, 15.00, 'Disponivel');

INSERT INTO recurso (tipo) VALUES ('Sala'); SET @rid = SCOPE_IDENTITY();
INSERT INTO sala (recurso_id, espaco_id, nome, capacidade, preco_hora, estado)
VALUES (@rid, 1, 'Sala B', 12, 22.00, 'Disponivel');

INSERT INTO recurso (tipo) VALUES ('Sala'); SET @rid = SCOPE_IDENTITY();
INSERT INTO sala (recurso_id, espaco_id, nome, capacidade, preco_hora, estado)
VALUES (@rid, 2, 'Sala Boavista 1', 6, 18.00, 'Disponivel');

-- Postos: 2 Flex (Aveiro), 1 Fixo (Aveiro), 1 Privado (Porto)
INSERT INTO recurso (tipo) VALUES ('Posto'); SET @rid = SCOPE_IDENTITY();
INSERT INTO posto (recurso_id, espaco_id, codigo, tipo_posto, preco_dia, estado)
VALUES (@rid, 1, 'AV-F01', 'Flex', 12.00, 'Disponivel');

INSERT INTO recurso (tipo) VALUES ('Posto'); SET @rid = SCOPE_IDENTITY();
INSERT INTO posto (recurso_id, espaco_id, codigo, tipo_posto, preco_dia, estado)
VALUES (@rid, 1, 'AV-F02', 'Flex', 12.00, 'Disponivel');

INSERT INTO recurso (tipo) VALUES ('Posto'); SET @rid = SCOPE_IDENTITY();
INSERT INTO posto (recurso_id, espaco_id, codigo, tipo_posto, preco_dia, estado)
VALUES (@rid, 1, 'AV-FX1', 'Fixo', 18.00, 'Disponivel');

INSERT INTO recurso (tipo) VALUES ('Posto'); SET @rid = SCOPE_IDENTITY();
INSERT INTO posto (recurso_id, espaco_id, codigo, tipo_posto, preco_dia, estado)
VALUES (@rid, 2, 'PT-PV1', 'Privado', 25.00, 'Disponivel');
GO

-- adesoes (3)
-- T10 (INSTEAD OF INSERT) preenche preco_acordado se NULL — aqui passamos explicitamente.
-- T9 valida coerência tipo_plano vs tipo_posto.
-- T4 valida adesão ativa única por cliente.
--
-- adesao_id = 1: Ana   (cliente 1) — Flex Mensal    (plano 1), recurso_id=NULL,  Ativa
-- adesao_id = 2: Bruno (cliente 2) — Fixo Mensal    (plano 3), recurso_id=6 (AV-FX1 Fixo), Ativa
-- adesao_id = 3: Carla (cliente 3) — Privado Mensal (plano 5), recurso_id=7 (PT-PV1 Privado), Terminada
INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado)
VALUES (1, 1, NULL, '2026-04-01', 120.00, 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado)
VALUES (2, 3, 6, '2026-04-15', 200.00, 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, data_fim, preco_acordado, estado)
VALUES (3, 5, 7, '2026-02-01', '2026-03-01', 350.00, 'Terminada');
GO

-- reservas (4)
-- T1  (sem sobreposição): Sala A em datas diferentes — OK.
-- T2  (horário espaço): 08:00-20:00 Aveiro, 07:00-22:00 Porto — todas dentro.
-- T3  (capacidade): Sala A cap=8 (4 part.), Sala B cap=12 (8 part.) — OK.
-- T7  (recurso disponível): todos 'Disponivel' — OK.
-- T11 (posto sem adesão): Diogo (cliente 4) sem adesão Flex → day pass OK.
-- T12 (horas coerentes): salas têm horas, posto não tem horas — OK.
--
-- reserva_id = 1: Diogo (cliente 4) — Sala A  (recurso 1), 2026-05-04 14:00-16:00, 30.00
-- reserva_id = 2: Eva   (cliente 5) — Sala A  (recurso 1), 2026-05-06 10:00-12:00, 30.00
-- reserva_id = 3: Diogo (cliente 4) — Sala B  (recurso 2), 2026-05-08 09:00-13:00, 88.00
-- reserva_id = 4: Diogo (cliente 4) — AV-F01  (recurso 4), 2026-05-05 day-pass,   12.00

INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, valor, estado, num_participantes, notas)
VALUES (4, 1, '2026-05-04', '14:00', '16:00', 30.00, 'Confirmada', 4, 'Reunião projeto');
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, valor, estado, num_participantes, notas)
VALUES (5, 1, '2026-05-06', '10:00', '12:00', 30.00, 'Pendente', 3, NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, valor, estado, num_participantes)
VALUES (4, 2, '2026-05-08', '09:00', '13:00', 88.00, 'Confirmada', 8);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, valor, estado)
VALUES (4, 4, '2026-05-05', NULL, NULL, 12.00, 'Confirmada');
GO

-- pagamentos (6)
-- T6  (valor correto): pagamento.valor = adesao.preco_acordado ou reserva.valor.
-- T8  (cliente consistente): pagamento.cliente_id = titular adesão/reserva.
-- ck_pagamento_servico: exactamente um de adesao_id/reserva_id NOT NULL.
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id, data_pagamento) VALUES
 (1, 120.00, 'MBWay',         'Pago', 1, NULL, '2026-04-01'),  -- Ana adesão Flex
 (2, 200.00, 'Transferencia', 'Pago', 2, NULL, '2026-04-15'),  -- Bruno adesão Fixo
 (3, 350.00, 'Cartao',        'Pago', 3, NULL, '2026-02-01'),  -- Carla adesão Privado
 (4,  30.00, 'Cartao',        'Pago', NULL, 1, '2026-05-04'),  -- Diogo reserva Sala A
 (4,  88.00, 'PayPal',        'Pago', NULL, 3, '2026-05-08'),  -- Diogo reserva Sala B
 (4,  12.00, 'Dinheiro',      'Pago', NULL, 4, '2026-05-05'); -- Diogo day pass posto Flex
GO

-- =====================================================================
-- Seed: politica_cancelamento (tiers de reembolso)
-- =====================================================================
INSERT INTO politica_cancelamento (nome, horas_minimas, perc_reembolso) VALUES
 (N'48h ou mais: 100%', 48, 100.00),
 (N'24h-48h: 50%',      24,  50.00),
 (N'<24h: 0%',           0,   0.00);
GO

-- =====================================================================
-- Seed: utilizador (passwords de DEV — TROCAR EM PRODUÇÃO)
-- Pré-requisito: Auth.sql executado.
-- =====================================================================
DECLARE @uid INT;
EXEC sp_register_user N'admin',  N'admin1234',   'Admin',   NULL, @uid OUTPUT;
EXEC sp_register_user N'staff1', N'staff1234',   'Staff',   NULL, @uid OUTPUT;
EXEC sp_register_user N'ana',    N'cliente1234', 'Cliente', 1,    @uid OUTPUT;
EXEC sp_register_user N'bruno',  N'cliente1234', 'Cliente', 2,    @uid OUTPUT;
EXEC sp_register_user N'carla',  N'cliente1234', 'Cliente', 3,    @uid OUTPUT;
EXEC sp_register_user N'diogo',  N'cliente1234', 'Cliente', 4,    @uid OUTPUT;
EXEC sp_register_user N'eva',    N'cliente1234', 'Cliente', 5,    @uid OUTPUT;
GO

PRINT 'Seed inserido com sucesso.';
GO
