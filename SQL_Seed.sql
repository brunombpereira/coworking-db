USE CoworkingDB;
GO

-- Planos
INSERT INTO plano (nome_plano, preco_mensal, duracao_meses, descricao) VALUES
('Basic',    50.00, 1, 'Acesso flex, sem sala incluída'),
('Standard', 120.00, 3, 'Posto fixo + 2h sala/mês'),
('Premium',  250.00, 12, 'Posto privado + sala ilimitada');
GO

-- Espaços
INSERT INTO espaco (nome, morada, telefone, email, hora_abertura, hora_fecho) VALUES
('HUB Centro',    'Rua do Comércio 10, Lisboa',   '210000001', 'hub@centro.pt',   '08:00', '22:00'),
('CoWork Porto',  'Av. dos Aliados 55, Porto',     '220000002', 'info@cwporto.pt', '07:00', '21:00'),
('SpaceLab',      'Rua das Flores 3, Braga',       NULL,        NULL,              '09:00', '19:00');
GO

-- Salas
INSERT INTO sala (nome, capacidade, preco_hora, estado, espaco_id) VALUES
('Sala A',      8,  15.00, 'Disponivel',   1),
('Sala Board',  20, 30.00, 'Disponivel',   1),
('Sala Norte',  6,  12.00, 'Disponivel',   2),
('Sala Sul',    10, 18.00, 'Manutencao',   2),
('Sala Única',  4,  10.00, 'Disponivel',   3);
GO

-- Postos de Trabalho
INSERT INTO posto_trabalho (codigo, tipo, preco_hora, estado, espaco_id) VALUES
('HUB-F01', 'Flex',    3.00, 'Disponivel', 1),
('HUB-F02', 'Flex',    3.00, 'Disponivel', 1),
('HUB-X01', 'Fixo',    5.00, 'Disponivel', 1),
('CP-F01',  'Flex',    3.50, 'Disponivel', 2),
('CP-P01',  'Privado', 8.00, 'Disponivel', 2),
('SL-F01',  'Flex',    2.50, 'Disponivel', 3),
('SL-F02',  'Flex',    2.50, 'Inativo',    3);
GO

-- Clientes
INSERT INTO cliente (nome, nif, email, telefone) VALUES
('Ana Silva',       '123456789', 'ana@email.pt',    '912000001'),
('Bruno Costa',     '234567891', 'bruno@email.pt',  '912000002'),
('Carlos Mendes',   '345678912', 'carlos@email.pt', '912000003'),
('Diana Ferreira',  '456789123', 'diana@email.pt',  '912000004'),
('Eduardo Pinto',   '567891234', 'edu@email.pt',    '912000005'),
('Filipa Rocha',    '678912345', 'filipa@email.pt', '912000006'),
('Gabriel Santos',  '789123456', 'gabriel@email.pt','912000007'),
('Helena Cruz',     '891234567', 'helena@email.pt', '912000008');
GO

-- Adesões (data_fim calculada pelo trigger T5 automaticamente)
INSERT INTO adesao (cliente_id, plano_id, data_inicio, estado) VALUES
(1, 1, '2026-01-01', 'Ativa'),
(2, 2, '2026-02-01', 'Ativa'),
(3, 3, '2025-12-01', 'Terminada'),
(4, 1, '2026-04-01', 'Pendente'),
(5, 2, '2026-03-01', 'Ativa');
GO

-- Reservas (inserir uma de cada vez para respeitar os triggers)
INSERT INTO reserva (cliente_id, sala_id, posto_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (1, 1, NULL, '2026-05-10', '09:00', '11:00', 'Confirmada', 30.00, 3, NULL);
GO
INSERT INTO reserva (cliente_id, sala_id, posto_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (2, NULL, 1, '2026-05-10', '08:00', '17:00', 'Confirmada', 27.00, NULL, 'Posto flex dia inteiro');
GO
INSERT INTO reserva (cliente_id, sala_id, posto_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (3, 2, NULL, '2026-05-12', '14:00', '16:00', 'Pendente', 60.00, 15, 'Reunião de equipa');
GO
INSERT INTO reserva (cliente_id, sala_id, posto_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (5, NULL, 4, '2026-05-11', '09:00', '13:00', 'Confirmada', 14.00, NULL, NULL);
GO
INSERT INTO reserva (cliente_id, sala_id, posto_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (6, 1, NULL, '2026-05-10', '13:00', '15:00', 'Cancelada', 30.00, 2, NULL);
GO
INSERT INTO reserva (cliente_id, sala_id, posto_id, data_reserva, hora_inicio, hora_fim, estado, valor, num_participantes, notas)
VALUES (7, 3, NULL, '2026-05-15', '10:00', '12:00', 'Pendente', 24.00, 4, NULL);
GO

-- Pagamentos
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES
(1, 50.00,  'MBWay',        'Pago', 1,    NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES
(2, 120.00, 'Transferencia','Pago', 2,    NULL);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES
(1, 30.00,  'Cartao',       'Pago', NULL, 1);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES
(2, 27.00,  'Dinheiro',     'Pago', NULL, 2);
GO
INSERT INTO pagamento (cliente_id, valor, metodo_pagamento, estado, adesao_id, reserva_id) VALUES
(5, 14.00,  'MBWay',        'Pago', NULL, 4);
GO
