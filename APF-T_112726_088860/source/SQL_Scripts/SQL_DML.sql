USE CoworkingDB;
GO

-- =====================================================================
-- Idempotência — limpar dados e repor contadores de identidade
-- =====================================================================
DELETE FROM pagamento;
DELETE FROM reserva;
DELETE FROM adesao;
DELETE FROM posto_trabalho;
DELETE FROM sala;
DELETE FROM recurso;
DELETE FROM cliente;
DELETE FROM espaco;
DELETE FROM plano;
DBCC CHECKIDENT ('pagamento', RESEED, 0);
DBCC CHECKIDENT ('reserva',   RESEED, 0);
DBCC CHECKIDENT ('adesao',    RESEED, 0);
DBCC CHECKIDENT ('recurso',   RESEED, 0);
DBCC CHECKIDENT ('cliente',   RESEED, 0);
DBCC CHECKIDENT ('espaco',    RESEED, 0);
DBCC CHECKIDENT ('plano',     RESEED, 0);
GO

-- =====================================================================
-- Planos
-- =====================================================================
INSERT INTO plano (nome_plano, preco_mensal, duracao_meses, descricao) VALUES
('Basic',      50.00,  1,  'Acesso flex ao espaço, sem posto incluído'),
('Standard',  120.00,  3,  'Posto fixo + 2h de sala por mês'),
('Premium',   250.00,  12, 'Posto privado + sala ilimitada + 24h acesso'),
('Day Pass',   15.00,  1,  'Acesso de um dia, sem compromisso'),
('Startup',   180.00,  6,  'Até 3 postos flex + sala 10h/mês');
GO

-- =====================================================================
-- Espaços
-- =====================================================================
INSERT INTO espaco (nome, morada, telefone, email, hora_abertura, hora_fecho) VALUES
('HUB Centro',   'Rua do Comércio 10, Lisboa',  '210000001', 'hub@centro.pt',    '08:00', '22:00'),
('CoWork Porto', 'Av. dos Aliados 55, Porto',   '220000002', 'info@cwporto.pt',  '07:00', '21:00'),
('SpaceLab',     'Rua das Flores 3, Braga',     '253000003', 'info@spacelab.pt', '09:00', '19:00');
GO

-- =====================================================================
-- Recursos — supertype (inserir salas primeiro para IDs 1-9, postos 10-23)
-- =====================================================================
INSERT INTO recurso (tipo) VALUES
('Sala'),  -- 1  Sala A
('Sala'),  -- 2  Sala Board
('Sala'),  -- 3  Sala Criativa
('Sala'),  -- 4  Sala Podcast
('Sala'),  -- 5  Sala Norte
('Sala'),  -- 6  Sala Sul
('Sala'),  -- 7  Sala Executiva
('Sala'),  -- 8  Sala Única
('Sala'),  -- 9  Sala Lab
('Posto'), -- 10 HUB-F01
('Posto'), -- 11 HUB-F02
('Posto'), -- 12 HUB-F03
('Posto'), -- 13 HUB-X01
('Posto'), -- 14 HUB-X02
('Posto'), -- 15 HUB-P01
('Posto'), -- 16 CP-F01
('Posto'), -- 17 CP-F02
('Posto'), -- 18 CP-X01
('Posto'), -- 19 CP-P01
('Posto'), -- 20 CP-P02
('Posto'), -- 21 SL-F01
('Posto'), -- 22 SL-F02
('Posto'); -- 23 SL-X01
GO

-- =====================================================================
-- Salas (subtype — recurso_id é FK, sem IDENTITY)
-- =====================================================================
INSERT INTO sala (recurso_id, nome, capacidade, preco_hora, estado, espaco_id) VALUES
-- HUB Centro (espaco_id=1)
(1, 'Sala A',         8,  15.00, 'Disponivel', 1),
(2, 'Sala Board',    20,  30.00, 'Disponivel', 1),
(3, 'Sala Criativa',  6,  18.00, 'Disponivel', 1),
(4, 'Sala Podcast',   4,  20.00, 'Manutencao', 1),
-- CoWork Porto (espaco_id=2)
(5, 'Sala Norte',     6,  12.00, 'Disponivel', 2),
(6, 'Sala Sul',      10,  18.00, 'Disponivel', 2),
(7, 'Sala Executiva', 8,  25.00, 'Disponivel', 2),
-- SpaceLab (espaco_id=3)
(8, 'Sala Única',     4,  10.00, 'Disponivel', 3),
(9, 'Sala Lab',      12,  22.00, 'Disponivel', 3);
GO

-- =====================================================================
-- Postos de Trabalho (subtype — recurso_id é FK, tipo_posto em vez de tipo)
-- =====================================================================
INSERT INTO posto_trabalho (recurso_id, codigo, tipo_posto, preco_hora, estado, espaco_id) VALUES
-- HUB Centro (espaco_id=1)
(10, 'HUB-F01', 'Flex',    3.00, 'Disponivel', 1),
(11, 'HUB-F02', 'Flex',    3.00, 'Disponivel', 1),
(12, 'HUB-F03', 'Flex',    3.00, 'Disponivel', 1),
(13, 'HUB-X01', 'Fixo',    5.00, 'Disponivel', 1),
(14, 'HUB-X02', 'Fixo',    5.00, 'Disponivel', 1),
(15, 'HUB-P01', 'Privado', 8.00, 'Disponivel', 1),
-- CoWork Porto (espaco_id=2)
(16, 'CP-F01',  'Flex',    3.50, 'Disponivel', 2),
(17, 'CP-F02',  'Flex',    3.50, 'Disponivel', 2),
(18, 'CP-X01',  'Fixo',    5.50, 'Disponivel', 2),
(19, 'CP-P01',  'Privado', 8.00, 'Disponivel', 2),
(20, 'CP-P02',  'Privado', 8.00, 'Inativo',    2),
-- SpaceLab (espaco_id=3)
(21, 'SL-F01',  'Flex',    2.50, 'Disponivel', 3),
(22, 'SL-F02',  'Flex',    2.50, 'Disponivel', 3),
(23, 'SL-X01',  'Fixo',    4.50, 'Disponivel', 3);
GO

-- =====================================================================
-- Clientes (20 clientes)
-- =====================================================================
INSERT INTO cliente (nome, nif, email, telefone) VALUES
('Ana Silva',       '123456789', 'ana.silva@email.pt',      '912000001'),
('Bruno Costa',     '234567891', 'bruno.costa@email.pt',    '912000002'),
('Carlos Mendes',   '345678912', 'carlos.mendes@email.pt',  '912000003'),
('Diana Ferreira',  '456789123', 'diana.ferreira@email.pt', '912000004'),
('Eduardo Pinto',   '567891234', 'edu.pinto@email.pt',      '912000005'),
('Filipa Rocha',    '678912345', 'filipa.rocha@email.pt',   '912000006'),
('Gabriel Santos',  '789123456', 'gabriel@email.pt',        '912000007'),
('Helena Cruz',     '891234567', 'helena.cruz@email.pt',    '912000008'),
('Igor Martins',    '901234578', 'igor.martins@email.pt',   '912000009'),
('Joana Pereira',   '102345679', 'joana.p@email.pt',        '912000010'),
('Kevin Lopes',     '112345670', 'kevin.lopes@email.pt',    '912000011'),
('Leonor Vieira',   '122345671', 'leonor.v@email.pt',       '912000012'),
('Miguel Carvalho', '132345672', 'miguel.c@email.pt',       '912000013'),
('Natacha Fonseca', '142345673', 'natacha.f@email.pt',      '912000014'),
('Oscar Teixeira',  '152345674', 'oscar.t@email.pt',        '912000015'),
('Paula Barbosa',   '162345675', 'paula.b@email.pt',        '912000016'),
('Ricardo Neves',   '172345676', 'ricardo.n@email.pt',      '912000017'),
('Sofia Almeida',   '182345677', 'sofia.a@email.pt',        '912000018'),
('Tiago Monteiro',  '192345678', 'tiago.m@email.pt',        '912000019'),
('Vera Gomes',      '202345679', 'vera.g@email.pt',         '912000020');
GO

-- =====================================================================
-- Adesões — data_fim calculada automaticamente pelo trigger T5
-- Regra T4: só uma Ativa por cliente ao mesmo tempo
-- =====================================================================
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (1,  1, '2026-04-01', 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (2,  2, '2026-03-01', 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (4,  1, '2026-04-15', 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (5,  2, '2026-02-01', 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (6,  3, '2026-01-01', 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (7,  5, '2026-03-15', 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (9,  1, '2026-04-20', 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (10, 2, '2026-02-15', 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (11, 3, '2026-01-10', 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (13, 4, '2026-05-01', 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (15, 5, '2026-03-01', 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (17, 2, '2026-04-01', 'Ativa');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (19, 1, '2026-05-01', 'Ativa');
GO
-- Adesões terminadas (histórico)
INSERT INTO adesao (cliente_id, plano_id, data_inicio, data_fim, estado) VALUES (3,  3, '2025-09-01', '2026-09-01', 'Terminada');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, data_fim, estado) VALUES (8,  2, '2025-11-01', '2026-02-01', 'Terminada');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, data_fim, estado) VALUES (12, 1, '2025-12-01', '2026-01-01', 'Terminada');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, data_fim, estado) VALUES (14, 4, '2026-01-10', '2026-02-10', 'Terminada');
GO
-- Adesões pendentes / canceladas
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (16, 1, '2026-05-03', 'Pendente');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES (18, 2, '2026-05-04', 'Pendente');
GO
INSERT INTO adesao (cliente_id, plano_id, data_inicio, data_fim, estado) VALUES (20, 3, '2025-10-01', '2026-10-01', 'Cancelada');
GO

-- =====================================================================
-- Reservas — inserir uma a uma (triggers T1/T2/T3/T7 validam)
-- recurso_id: salas=1-9, postos=10-23
-- =====================================================================

-- === Maio 2026 — reservas futuras ===
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (1,  1,  '2026-05-06', '09:00', '11:00', 'Confirmada', 30.00,  3,    'Reunião de projeto');
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (2,  10, '2026-05-06', '08:00', '17:00', 'Confirmada', 27.00,  NULL, 'Posto flex dia inteiro');
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (5,  16, '2026-05-06', '09:00', '13:00', 'Confirmada', 14.00,  NULL, NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (6,  2,  '2026-05-07', '14:00', '17:00', 'Confirmada', 90.00,  12,   'Workshop design');
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (7,  5,  '2026-05-07', '10:00', '12:00', 'Pendente',   24.00,  4,    NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (9,  21, '2026-05-08', '09:00', '18:00', 'Confirmada', 22.50,  NULL, NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (10, 3,  '2026-05-08', '11:00', '13:00', 'Confirmada', 36.00,  5,    'Apresentação cliente');
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (11, 15, '2026-05-09', '08:00', '17:00', 'Confirmada', 64.00,  NULL, 'Posto privado semana');
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (13, 7,  '2026-05-09', '09:00', '12:00', 'Pendente',   75.00,  6,    'Entrevistas');
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (15, 1,  '2026-05-12', '10:00', '12:00', 'Confirmada', 30.00,  3,    NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (17, 13, '2026-05-12', '09:00', '14:00', 'Confirmada', 25.00,  NULL, NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (1,  3,  '2026-05-13', '14:00', '16:00', 'Pendente',   36.00,  4,    'Demo produto');
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (6,  18, '2026-05-14', '08:00', '17:00', 'Confirmada', 49.50,  NULL, NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (4,  6,  '2026-05-15', '13:00', '15:00', 'Confirmada', 36.00,  8,    'Formação interna');
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (19, 22, '2026-05-15', '09:00', '13:00', 'Pendente',   10.00,  NULL, NULL);
GO
-- Canceladas (para histórico)
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (3,  1,  '2026-05-06', '13:00', '15:00', 'Cancelada',  30.00,  2,    NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (8,  5,  '2026-05-08', '14:00', '16:00', 'Cancelada',  24.00,  3,    NULL);
GO

-- === Abril 2026 — reservas passadas (Concluidas) ===
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (1,  1,  '2026-04-10', '09:00', '11:00', 'Concluida',  30.00,  3,    NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (2,  11, '2026-04-10', '08:00', '17:00', 'Concluida',  27.00,  NULL, NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (5,  2,  '2026-04-11', '14:00', '17:00', 'Concluida',  90.00,  10,   'Reunião trimestral');
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (6,  15, '2026-04-14', '08:00', '17:00', 'Concluida',  64.00,  NULL, NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (10, 3,  '2026-04-15', '10:00', '12:00', 'Concluida',  36.00,  5,    NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (11, 13, '2026-04-16', '09:00', '13:00', 'Concluida',  20.00,  NULL, NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (7,  6,  '2026-04-17', '13:00', '15:00', 'Concluida',  36.00,  7,    NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (9,  21, '2026-04-21', '09:00', '17:00', 'Concluida',  20.00,  NULL, NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (13, 7,  '2026-04-22', '09:00', '12:00', 'Concluida',  75.00,  6,    NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (15, 19, '2026-04-24', '08:00', '17:00', 'Concluida',  64.00,  NULL, NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (17, 5,  '2026-04-25', '11:00', '13:00', 'Concluida',  24.00,  4,    NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (2,  2,  '2026-04-28', '14:00', '18:00', 'Concluida',  120.00, 18,   'Evento comunidade');
GO

-- === Março 2026 — reservas passadas ===
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (6,  2,  '2026-03-05', '09:00', '12:00', 'Concluida',  90.00,  15,   'Hackathon');
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (11, 15, '2026-03-10', '08:00', '17:00', 'Concluida',  64.00,  NULL, NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (5,  3,  '2026-03-12', '14:00', '16:00', 'Concluida',  36.00,  5,    NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (10, 16, '2026-03-18', '09:00', '13:00', 'Concluida',  14.00,  NULL, NULL);
GO
INSERT INTO reserva (cliente_id, recurso_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (1,  1,  '2026-03-20', '10:00', '12:00', 'Concluida',  30.00,  3,    NULL);
GO

-- =====================================================================
-- Pagamentos
-- Adesão IDs: 1-13 = Ativas, 14-17 = Terminadas, 18-19 = Pendentes, 20 = Cancelada
-- Reserva IDs: 1-17 = Maio, 18-29 = Abril, 30-34 = Março
-- =====================================================================

-- Pagamentos de adesões ativas
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (1,  50.00,  'MBWay',        'Pago',     1,  NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (2,  120.00, 'Transferencia','Pago',     2,  NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (4,  50.00,  'MBWay',        'Pago',     3,  NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (5,  120.00, 'Cartao',       'Pago',     4,  NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (6,  250.00, 'Transferencia','Pago',     5,  NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (7,  180.00, 'Cartao',       'Pago',     6,  NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (9,  50.00,  'PayPal',       'Pago',     7,  NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (10, 120.00, 'MBWay',        'Pago',     8,  NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (11, 250.00, 'Transferencia','Pago',     9,  NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (13, 15.00,  'MBWay',        'Pago',     10, NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (15, 180.00, 'Cartao',       'Pago',     11, NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (17, 120.00, 'MBWay',        'Pago',     12, NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (19, 50.00,  'Dinheiro',     'Pendente', 13, NULL);
GO

-- Pagamentos de adesões terminadas (histórico)
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (3,  250.00, 'Transferencia','Pago',     14, NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (8,  120.00, 'MBWay',        'Pago',     15, NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (12, 50.00,  'Dinheiro',     'Pago',     16, NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (14, 15.00,  'MBWay',        'Pago',     17, NULL);
GO

-- Pagamentos de reservas concluídas (Abril e Março)
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (1,  30.00,  'MBWay',        'Pago', NULL, 18);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (2,  27.00,  'PayPal',       'Pago', NULL, 19);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (5,  90.00,  'Cartao',       'Pago', NULL, 20);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (6,  64.00,  'Transferencia','Pago', NULL, 21);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (10, 36.00,  'MBWay',        'Pago', NULL, 22);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (11, 20.00,  'Dinheiro',     'Pago', NULL, 23);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (7,  36.00,  'Cartao',       'Pago', NULL, 24);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (9,  20.00,  'MBWay',        'Pago', NULL, 25);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (13, 75.00,  'Transferencia','Pago', NULL, 26);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (15, 64.00,  'Cartao',       'Pago', NULL, 27);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (17, 24.00,  'MBWay',        'Pago', NULL, 28);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (2,  120.00, 'Transferencia','Pago', NULL, 29);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (6,  90.00,  'Cartao',       'Pago', NULL, 30);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (11, 64.00,  'Transferencia','Pago', NULL, 31);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (5,  36.00,  'MBWay',        'Pago', NULL, 32);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (10, 14.00,  'Dinheiro',     'Pago', NULL, 33);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (1,  30.00,  'MBWay',        'Pago', NULL, 34);
GO

-- Pagamentos de reservas confirmadas (futuras — pagas antecipadamente)
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (1,  30.00,  'MBWay',        'Pago', NULL, 1);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (2,  27.00,  'Dinheiro',     'Pago', NULL, 2);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (5,  14.00,  'MBWay',        'Pago', NULL, 3);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (6,  90.00,  'Cartao',       'Pago', NULL, 4);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (9,  22.50,  'MBWay',        'Pago', NULL, 6);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (10, 36.00,  'Transferencia','Pago', NULL, 7);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (11, 64.00,  'Cartao',       'Pago', NULL, 8);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (15, 30.00,  'MBWay',        'Pago', NULL, 10);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (17, 25.00,  'MBWay',        'Pago', NULL, 11);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (6,  49.50,  'Transferencia','Pago', NULL, 13);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES (4,  36.00,  'Cartao',       'Pago', NULL, 14);
GO
