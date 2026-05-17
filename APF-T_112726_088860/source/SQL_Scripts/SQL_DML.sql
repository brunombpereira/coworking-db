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
-- T6  (snapshot correto): preco_servico_snapshot = adesao.preco_acordado ou reserva.valor.
-- ck_pagamento_valor_snapshot: pagamento.valor = preco_servico_snapshot.
-- T8  (cliente consistente): pagamento.cliente_id = titular adesão/reserva.
-- ck_pagamento_servico: exactamente um de adesao_id/reserva_id NOT NULL.
INSERT INTO pagamento (cliente_id, valor, preco_servico_snapshot, metodo_pagamento, estado, adesao_id, reserva_id, data_pagamento) VALUES
 (1, 120.00, 120.00, 'MBWay',         'Pago', 1,    NULL, '2026-04-01'),  -- Ana adesão Flex
 (2, 200.00, 200.00, 'Transferencia', 'Pago', 2,    NULL, '2026-04-15'),  -- Bruno adesão Fixo
 (3, 350.00, 350.00, 'Cartao',        'Pago', 3,    NULL, '2026-02-01'),  -- Carla adesão Privado
 (4,  30.00,  30.00, 'Cartao',        'Pago', NULL, 1,    '2026-05-04'),  -- Diogo reserva Sala A
 (4,  88.00,  88.00, 'PayPal',        'Pago', NULL, 3,    '2026-05-08'),  -- Diogo reserva Sala B
 (4,  12.00,  12.00, 'Dinheiro',      'Pago', NULL, 4,    '2026-05-05'); -- Diogo day pass posto Flex
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

PRINT 'Seed base inserido.';
GO

-- =====================================================================
-- Seed expandido: dados históricos cobrindo ~18 meses até hoje.
-- Objetivo: alimentar gráficos/estatísticas com dados realistas.
-- =====================================================================

-- Clientes adicionais (cliente_id 6..20) -----------------------------
INSERT INTO cliente (nome, nif, email, telefone, data_registo) VALUES
 ('Filipe Ramos',    '500000001', 'filipe@example.com',    '913000001', '2025-01-15'),
 ('Gabriela Lima',   '500000002', 'gabriela@example.com',  '913000002', '2025-02-04'),
 ('Hugo Marques',    '500000003', 'hugo@example.com',      '913000003', '2025-03-10'),
 ('Inês Carvalho',   '500000004', 'ines@example.com',      '913000004', '2025-04-22'),
 ('João Tavares',    '500000005', 'joao@example.com',      '913000005', '2025-05-30'),
 ('Luísa Antunes',   '500000006', 'luisa@example.com',     '913000006', '2025-06-12'),
 ('Manuel Faria',    '500000007', 'manuel@example.com',    '913000007', '2025-07-08'),
 ('Nuno Almeida',    '500000008', 'nuno@example.com',      '913000008', '2025-08-15'),
 ('Olívia Pinto',    '500000009', 'olivia@example.com',    '913000009', '2025-09-01'),
 ('Pedro Soares',    '500000010', 'pedro@example.com',     '913000010', '2025-09-20'),
 ('Rita Costa',      '500000011', 'rita@example.com',      '913000011', '2025-10-05'),
 ('Sofia Neves',     '500000012', 'sofia@example.com',     '913000012', '2025-11-11'),
 ('Tiago Brito',     '500000013', 'tiago@example.com',     '913000013', '2025-12-03'),
 ('Vanessa Cruz',    '500000014', 'vanessa@example.com',   '914000001', '2026-01-08'),
 ('Xavier Lopes',    '500000015', 'xavier@example.com',    '914000002', '2026-02-19');
GO

-- =====================================================================
-- Adesões históricas: várias terminadas + algumas activas.
-- T4 garante max 1 Ativa por cliente — controlamos via estado.
-- =====================================================================

-- Histórico Flex (terminadas) — vários clientes ao longo do ano
INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, data_fim, preco_acordado, estado) VALUES
 (6,  1, NULL, '2025-01-15', '2025-02-15', 120.00, 'Terminada'),
 (6,  1, NULL, '2025-02-15', '2025-03-15', 120.00, 'Terminada'),
 (6,  2, NULL, '2025-03-15', '2025-06-15', 330.00, 'Terminada'),
 (7,  1, NULL, '2025-02-04', '2025-03-04', 120.00, 'Terminada'),
 (7,  1, NULL, '2025-03-04', '2025-04-04', 120.00, 'Terminada'),
 (8,  3, 6,    '2025-03-10', '2025-04-10', 200.00, 'Terminada'),
 (8,  3, 6,    '2025-04-10', '2025-05-10', 200.00, 'Terminada'),
 (9,  1, NULL, '2025-04-22', '2025-05-22', 120.00, 'Terminada'),
 (10, 5, 7,    '2025-05-30', '2025-06-30', 350.00, 'Terminada'),
 (10, 5, 7,    '2025-06-30', '2025-07-30', 350.00, 'Terminada'),
 (11, 1, NULL, '2025-06-12', '2025-07-12', 120.00, 'Terminada'),
 (12, 3, 6,    '2025-07-08', '2025-08-08', 200.00, 'Terminada'),
 (13, 1, NULL, '2025-08-15', '2025-09-15', 120.00, 'Terminada'),
 (14, 2, NULL, '2025-09-01', '2025-12-01', 330.00, 'Terminada'),
 (15, 1, NULL, '2025-09-20', '2025-10-20', 120.00, 'Terminada'),
 (16, 5, 7,    '2025-10-05', '2025-11-05', 350.00, 'Terminada'),
 (17, 1, NULL, '2025-11-11', '2025-12-11', 120.00, 'Terminada'),
 (18, 3, 6,    '2025-12-03', '2026-01-03', 200.00, 'Terminada'),
 (19, 1, NULL, '2026-01-08', '2026-02-08', 120.00, 'Terminada'),
 (20, 1, NULL, '2026-02-19', '2026-03-19', 120.00, 'Terminada');
GO

-- Adesões activas/pendentes recentes (5)
INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, preco_acordado, estado) VALUES
 (6,  1, NULL,'2026-04-01', 120.00, 'Ativa'),
 (10, 5, 7,   '2026-04-15', 350.00, 'Ativa'),
 (15, 1, NULL,'2026-04-20', 120.00, 'Ativa'),
 (17, 3, 6,   '2026-05-01', 200.00, 'Pendente'),
 (20, 2, NULL,'2026-05-10', 330.00, 'Ativa');
GO

-- =====================================================================
-- Reservas históricas: gerador via WHILE loop, distribuídas em 18 meses.
-- Slots horários sem sobreposição (T1).
-- Postos Flex day-pass por clientes sem adesão Flex (T11).
-- =====================================================================
DECLARE @data DATE = '2025-01-06';        -- 1ª segunda-feira de 2025
DECLARE @fim  DATE = '2026-05-17';
DECLARE @cli  INT;
DECLARE @rec  INT;
DECLARE @hi   TIME;
DECLARE @hf   TIME;
DECLARE @val  DECIMAL(10,2);
DECLARE @part INT;
DECLARE @i    INT = 0;

WHILE @data <= @fim
BEGIN
    -- 4 reservas de Sala por semana (ter,qua,qui,sex)
    DECLARE @offset INT = 1;
    WHILE @offset <= 4
    BEGIN
        SET @i  = @i + 1;
        SET @cli = ((@i * 7) % 20) + 1;             -- clientes 1..20 (rotação)
        SET @rec = ((@i * 3) % 3) + 1;              -- salas 1..3 (Sala A, B, Boavista)
        -- Slot horário rotativo: 09-11, 11-13, 14-16, 16-18
        SET @hi = CASE @offset
                    WHEN 1 THEN '09:00' WHEN 2 THEN '11:00'
                    WHEN 3 THEN '14:00' WHEN 4 THEN '16:00' END;
        SET @hf = CASE @offset
                    WHEN 1 THEN '11:00' WHEN 2 THEN '13:00'
                    WHEN 3 THEN '16:00' WHEN 4 THEN '18:00' END;
        -- Valor baseado no recurso (sala): 15/22/18 €/h * 2h
        SET @val = CASE @rec WHEN 1 THEN 30.00 WHEN 2 THEN 44.00 ELSE 36.00 END;
        SET @part = ((@i * 11) % 6) + 2;            -- 2..7 participantes (cabe em todas)

        -- 90% Confirmada, 7% Pendente, 3% Cancelada
        DECLARE @estado VARCHAR(20) =
            CASE WHEN @i % 33 = 0 THEN 'Cancelada'
                 WHEN @i % 13 = 0 THEN 'Pendente'
                 ELSE 'Confirmada' END;

        INSERT INTO reserva (cliente_id, recurso_id, data_reserva,
                             hora_inicio, hora_fim, valor, estado, num_participantes)
        VALUES (@cli, @rec, DATEADD(DAY, @offset, @data),
                @hi, @hf, @val, @estado, @part);

        SET @offset = @offset + 1;
    END;
    SET @data = DATEADD(DAY, 7, @data);
END;
PRINT CONCAT('Reservas de sala criadas: ', @i);
GO

-- Day passes de Posto Flex (cliente sem adesão Flex naquele dia)
DECLARE @data DATE = '2025-01-15';
DECLARE @fim  DATE = '2026-05-15';
DECLARE @cli  INT;
DECLARE @rec  INT;
DECLARE @i    INT = 0;

WHILE @data <= @fim
BEGIN
    SET @i = @i + 1;
    -- Alternar entre clientes 4 (Diogo) e 5 (Eva) que não têm adesão Flex
    SET @cli = CASE WHEN @i % 2 = 0 THEN 4 ELSE 5 END;
    SET @rec = CASE WHEN @i % 2 = 0 THEN 4 ELSE 5 END;     -- AV-F01 ou AV-F02

    INSERT INTO reserva (cliente_id, recurso_id, data_reserva,
                         hora_inicio, hora_fim, valor, estado)
    VALUES (@cli, @rec, @data, NULL, NULL, 12.00, 'Confirmada');

    SET @data = DATEADD(DAY, 14, @data);    -- 1 a cada 2 semanas
END;
PRINT CONCAT('Day passes posto criados: ', @i);
GO

-- =====================================================================
-- Pagamentos: 1 por adesão Ativa/Terminada + 1 por reserva Confirmada.
-- Estado='Pago' para a maior parte. data_pagamento = data_reserva (ou
-- data_inicio para adesões).
-- =====================================================================

-- Pagamentos das adesões (todas as Ativa/Terminada/Pendente)
INSERT INTO pagamento (cliente_id, valor, preco_servico_snapshot,
                       metodo_pagamento, estado, adesao_id, reserva_id, data_pagamento)
SELECT a.cliente_id,
       a.preco_acordado,
       a.preco_acordado,
       CASE (a.adesao_id % 5)
         WHEN 0 THEN 'MBWay'
         WHEN 1 THEN 'Cartao'
         WHEN 2 THEN 'Transferencia'
         WHEN 3 THEN 'PayPal'
         ELSE 'Dinheiro'
       END,
       CASE WHEN a.estado = 'Pendente' THEN 'Pendente' ELSE 'Pago' END,
       a.adesao_id,
       NULL,
       a.data_inicio
FROM adesao a
WHERE a.adesao_id > 3                                 -- skipar as 3 do seed base
  AND NOT EXISTS (SELECT 1 FROM pagamento pg WHERE pg.adesao_id = a.adesao_id);
PRINT CONCAT('Pagamentos de adesão: ', @@ROWCOUNT);
GO

-- Pagamentos das reservas (Confirmadas, exceptuando as 4 do seed base)
INSERT INTO pagamento (cliente_id, valor, preco_servico_snapshot,
                       metodo_pagamento, estado, adesao_id, reserva_id, data_pagamento)
SELECT r.cliente_id,
       r.valor,
       r.valor,
       CASE (r.reserva_id % 5)
         WHEN 0 THEN 'MBWay'
         WHEN 1 THEN 'Cartao'
         WHEN 2 THEN 'Transferencia'
         WHEN 3 THEN 'PayPal'
         ELSE 'Dinheiro'
       END,
       'Pago',
       NULL,
       r.reserva_id,
       r.data_reserva
FROM reserva r
WHERE r.estado = 'Confirmada'
  AND r.reserva_id > 4
  AND NOT EXISTS (SELECT 1 FROM pagamento pg WHERE pg.reserva_id = r.reserva_id);
PRINT CONCAT('Pagamentos de reserva: ', @@ROWCOUNT);
GO

-- =====================================================================
-- Utilizadores adicionais para os clientes novos (passwords DEV)
-- =====================================================================
DECLARE @uid INT;
EXEC sp_register_user N'filipe',   N'cliente1234', 'Cliente', 6,  @uid OUTPUT;
EXEC sp_register_user N'gabriela', N'cliente1234', 'Cliente', 7,  @uid OUTPUT;
EXEC sp_register_user N'hugo',     N'cliente1234', 'Cliente', 8,  @uid OUTPUT;
EXEC sp_register_user N'ines',     N'cliente1234', 'Cliente', 9,  @uid OUTPUT;
EXEC sp_register_user N'joao',     N'cliente1234', 'Cliente', 10, @uid OUTPUT;
EXEC sp_register_user N'luisa',    N'cliente1234', 'Cliente', 11, @uid OUTPUT;
EXEC sp_register_user N'manuel',   N'cliente1234', 'Cliente', 12, @uid OUTPUT;
EXEC sp_register_user N'nuno',     N'cliente1234', 'Cliente', 13, @uid OUTPUT;
EXEC sp_register_user N'olivia',   N'cliente1234', 'Cliente', 14, @uid OUTPUT;
EXEC sp_register_user N'pedro',    N'cliente1234', 'Cliente', 15, @uid OUTPUT;
EXEC sp_register_user N'rita',     N'cliente1234', 'Cliente', 16, @uid OUTPUT;
EXEC sp_register_user N'sofia',    N'cliente1234', 'Cliente', 17, @uid OUTPUT;
EXEC sp_register_user N'tiago',    N'cliente1234', 'Cliente', 18, @uid OUTPUT;
EXEC sp_register_user N'vanessa',  N'cliente1234', 'Cliente', 19, @uid OUTPUT;
EXEC sp_register_user N'xavier',   N'cliente1234', 'Cliente', 20, @uid OUTPUT;
GO

PRINT 'Seed expandido inserido com sucesso.';
GO
