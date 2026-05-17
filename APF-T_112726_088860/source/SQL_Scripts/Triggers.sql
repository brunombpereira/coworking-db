-- =====================================================================
-- Triggers da CoworkingDB
-- T1..T16 — regras de negócio que ultrapassam CHECK constraints simples
-- (T10 foi removido; lógica migrada para sp_criar_adesao por
-- incompatibilidade com SYSTEM_VERSIONING).
-- =====================================================================
USE CoworkingDB;
GO

-- T1: sem sobreposição de reservas ------------------------------------
-- Sala (com horas) compara janelas; Posto (sem horas) compara só data
CREATE TRIGGER trg_reserva_sem_sobreposicao
ON reserva AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN reserva r ON r.recurso_id   = i.recurso_id
                      AND r.reserva_id  <> i.reserva_id
                      AND r.data_reserva = i.data_reserva
        WHERE i.estado <> 'Cancelada'
          AND r.estado <> 'Cancelada'
          AND (
              (i.hora_inicio IS NOT NULL AND r.hora_inicio IS NOT NULL
               AND i.hora_inicio < r.hora_fim AND i.hora_fim > r.hora_inicio)
              OR (i.hora_inicio IS NULL OR r.hora_inicio IS NULL)
          )
    )
    OR EXISTS (
        SELECT 1
        FROM inserted i1
        JOIN inserted i2 ON i1.recurso_id   = i2.recurso_id
                        AND i1.reserva_id   < i2.reserva_id
                        AND i1.data_reserva = i2.data_reserva
        WHERE i1.estado <> 'Cancelada'
          AND i2.estado <> 'Cancelada'
          AND (
              (i1.hora_inicio IS NOT NULL AND i2.hora_inicio IS NOT NULL
               AND i1.hora_inicio < i2.hora_fim AND i1.hora_fim > i2.hora_inicio)
              OR (i1.hora_inicio IS NULL OR i2.hora_inicio IS NULL)
          )
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, 'Já existe uma reserva sobreposta para o mesmo recurso e período.', 1;
    END
END;
GO

-- T2: reserva de sala dentro do horário do espaço ---------------------
CREATE TRIGGER trg_reserva_horario_espaco
ON reserva AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN recurso rc ON i.recurso_id = rc.recurso_id
        LEFT JOIN sala  s ON rc.recurso_id = s.recurso_id
        LEFT JOIN posto p ON rc.recurso_id = p.recurso_id
        JOIN espaco e ON e.espaco_id = COALESCE(s.espaco_id, p.espaco_id)
        WHERE i.hora_inicio IS NOT NULL
          AND (i.hora_inicio < e.hora_abertura OR i.hora_fim > e.hora_fecho)
          AND i.estado <> 'Cancelada'
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50002, 'A reserva está fora do horário de funcionamento do espaço.', 1;
    END
END;
GO

-- T3: num_participantes <= capacidade da sala ------------------------
CREATE TRIGGER trg_reserva_capacidade
ON reserva AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN sala s ON i.recurso_id = s.recurso_id
        WHERE i.num_participantes IS NOT NULL
          AND i.num_participantes > s.capacidade
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50003, 'O número de participantes excede a capacidade da sala.', 1;
    END
END;
GO

-- T4: adesão ativa única por cliente ---------------------------------
CREATE TRIGGER trg_adesao_ativa_unica
ON adesao AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN adesao a ON a.cliente_id = i.cliente_id
                     AND a.adesao_id <> i.adesao_id
                     AND a.estado     = 'Ativa'
        WHERE i.estado = 'Ativa'
    )
    OR EXISTS (
        SELECT 1
        FROM inserted i1
        JOIN inserted i2 ON i1.cliente_id = i2.cliente_id
                        AND i1.adesao_id  < i2.adesao_id
        WHERE i1.estado = 'Ativa' AND i2.estado = 'Ativa'
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50004, 'O cliente já tem uma adesão ativa.', 1;
    END
END;
GO

-- T5: calcular data_fim da adesão automaticamente --------------------
CREATE TRIGGER trg_adesao_data_fim
ON adesao AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE a
    SET    a.data_fim = DATEADD(MONTH, p.duracao_meses, i.data_inicio)
    FROM   adesao  a
    JOIN   inserted i ON a.adesao_id = i.adesao_id
    JOIN   plano    p ON i.plano_id  = p.plano_id
    WHERE  i.data_fim IS NULL;
END;
GO

-- T6: snapshot do pagamento tem de coincidir com o preço actual ------
-- valor = preco_servico_snapshot já é garantido por CHECK constraint
-- ck_pagamento_valor_snapshot. Este trigger valida a outra metade da
-- ligação: snapshot == preço do serviço referenciado.
CREATE TRIGGER trg_pagamento_valor_correto
ON pagamento AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN reserva r ON i.reserva_id = r.reserva_id
        WHERE i.reserva_id IS NOT NULL
          AND i.preco_servico_snapshot <> r.valor
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50005, 'O snapshot do pagamento não corresponde ao valor da reserva.', 1;
    END
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN adesao a ON i.adesao_id = a.adesao_id
        WHERE i.adesao_id IS NOT NULL
          AND i.preco_servico_snapshot <> a.preco_acordado
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50006, 'O snapshot do pagamento não corresponde ao preço acordado da adesão.', 1;
    END
END;
GO

-- T7: recurso disponível (INSERT only) -------------------------------
CREATE TRIGGER trg_reserva_recurso_disponivel
ON reserva AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN recurso rc ON i.recurso_id = rc.recurso_id
        LEFT JOIN sala  s ON rc.recurso_id = s.recurso_id
        LEFT JOIN posto p ON rc.recurso_id = p.recurso_id
        WHERE (s.recurso_id IS NOT NULL AND s.estado <> 'Disponivel')
           OR (p.recurso_id IS NOT NULL AND p.estado <> 'Disponivel')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50007, 'O recurso reservado não está disponível.', 1;
    END
END;
GO

-- T8: pagamento.cliente_id consistente com titular -------------------
CREATE TRIGGER trg_pagamento_cliente_consistente
ON pagamento AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN reserva r ON i.reserva_id = r.reserva_id
        LEFT JOIN adesao  a ON i.adesao_id  = a.adesao_id
        WHERE i.cliente_id <> COALESCE(r.cliente_id, a.cliente_id)
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50008, 'O cliente do pagamento não corresponde ao titular da reserva/adesão.', 1;
    END
END;
GO

-- T9: adesão coerente com plano e recurso ----------------------------
CREATE TRIGGER trg_adesao_recurso_coerente
ON adesao AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN plano pl ON i.plano_id = pl.plano_id
        WHERE pl.tipo_plano = 'Flex' AND i.recurso_id IS NOT NULL
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50009, 'Adesão Flex não pode ter recurso atribuído.', 1;
    END
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN plano pl ON i.plano_id = pl.plano_id
        WHERE pl.tipo_plano IN ('Fixo','Privado') AND i.recurso_id IS NULL
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50009, 'Adesão Fixo/Privado tem de ter um posto atribuído.', 1;
    END
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN plano pl ON i.plano_id = pl.plano_id
        JOIN recurso rc ON i.recurso_id = rc.recurso_id
        WHERE pl.tipo_plano IN ('Fixo','Privado') AND rc.tipo <> 'Posto'
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50009, 'Adesão Fixo/Privado tem de apontar para um posto.', 1;
    END
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN plano pl ON i.plano_id = pl.plano_id
        JOIN posto p  ON i.recurso_id = p.recurso_id
        WHERE pl.tipo_plano IN ('Fixo','Privado')
          AND p.tipo_posto <> pl.tipo_plano
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50009, 'O tipo do posto não corresponde ao tipo do plano.', 1;
    END
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN adesao a ON a.recurso_id = i.recurso_id
                     AND a.adesao_id <> i.adesao_id
                     AND a.estado IN ('Pendente','Ativa')
        WHERE i.recurso_id IS NOT NULL
          AND i.estado IN ('Pendente','Ativa')
          AND i.data_inicio <= COALESCE(a.data_fim, '9999-12-31')
          AND COALESCE(i.data_fim, '9999-12-31') >= a.data_inicio
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50009, 'Já existe uma adesão Pendente/Ativa sobre este posto em datas sobrepostas.', 1;
    END
END;
GO

-- T10: (removido) snapshot de preco_acordado movido para sp_criar_adesao
-- Razão: SYSTEM_VERSIONING não é compatível com triggers INSTEAD OF.
-- A lógica que preenche preco_acordado a partir do plano vive agora
-- no SP, e o INSERT direto na tabela passa a exigir preco_acordado NOT NULL.

-- T11: reserva avulsa de posto não colide com adesão Ativa -----------
CREATE TRIGGER trg_reserva_posto_sem_adesao
ON reserva AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN posto    po ON i.recurso_id = po.recurso_id
        JOIN adesao   a  ON a.cliente_id = i.cliente_id AND a.estado = 'Ativa'
        JOIN plano    pl ON a.plano_id   = pl.plano_id
        WHERE i.estado <> 'Cancelada'
          AND po.tipo_posto = 'Flex'
          AND pl.tipo_plano = 'Flex'
          AND i.data_reserva BETWEEN a.data_inicio AND COALESCE(a.data_fim, '9999-12-31')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50011, 'Cliente tem adesão Flex Ativa — não pode reservar posto Flex no mesmo dia.', 1;
    END
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN posto    po ON i.recurso_id = po.recurso_id
        JOIN adesao   a  ON a.recurso_id = i.recurso_id AND a.estado = 'Ativa'
        WHERE i.estado <> 'Cancelada'
          AND po.tipo_posto IN ('Fixo','Privado')
          AND i.data_reserva BETWEEN a.data_inicio AND COALESCE(a.data_fim, '9999-12-31')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50011, 'Posto Fixo/Privado está atribuído a uma adesão ativa.', 1;
    END
END;
GO

-- T12: horas coerentes com tipo do recurso ---------------------------
CREATE TRIGGER trg_reserva_horas_coerentes
ON reserva AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN sala s ON i.recurso_id = s.recurso_id
        WHERE i.hora_inicio IS NULL OR i.hora_fim IS NULL
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50012, 'Reserva de sala tem de ter hora_inicio e hora_fim.', 1;
    END
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN posto p ON i.recurso_id = p.recurso_id
        WHERE i.hora_inicio IS NOT NULL
           OR i.hora_fim    IS NOT NULL
           OR i.num_participantes IS NOT NULL
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50012, 'Reserva de posto não pode ter horas nem num_participantes.', 1;
    END
END;
GO

-- T13: notificação automática ao criar reserva -----------------------
CREATE TRIGGER trg_reserva_notificacao
ON reserva AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO notificacao (cliente_id, tipo, assunto, mensagem)
    SELECT
        i.cliente_id,
        'ReservaCriada',
        CONCAT('Reserva criada — ', CONVERT(NVARCHAR, i.data_reserva, 23)),
        CONCAT('A sua reserva #', i.reserva_id,
               ' para ', CONVERT(NVARCHAR, i.data_reserva, 23),
               COALESCE(' das ' + CONVERT(NVARCHAR(5), i.hora_inicio, 108) +
                        ' às '  + CONVERT(NVARCHAR(5), i.hora_fim,    108), ' (dia inteiro)'),
               ' foi registada com estado ', i.estado, '.')
    FROM inserted i;
END;
GO

-- T14: notificação ao cancelar reserva -------------------------------
CREATE TRIGGER trg_reserva_cancelada_notificacao
ON reserva AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF NOT UPDATE(estado) RETURN;
    INSERT INTO notificacao (cliente_id, tipo, assunto, mensagem)
    SELECT
        i.cliente_id,
        'ReservaCancelada',
        CONCAT('Reserva cancelada #', i.reserva_id),
        CONCAT('A sua reserva #', i.reserva_id,
               ' para ', CONVERT(NVARCHAR, i.data_reserva, 23),
               ' foi cancelada.')
    FROM inserted i
    JOIN deleted  d ON i.reserva_id = d.reserva_id
    WHERE i.estado = 'Cancelada' AND d.estado <> 'Cancelada';
END;
GO

-- T15: notificação ao confirmar pagamento ----------------------------
CREATE TRIGGER trg_pagamento_confirmado_notificacao
ON pagamento AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO notificacao (cliente_id, tipo, assunto, mensagem)
    SELECT
        i.cliente_id,
        'PagamentoConfirmado',
        CONCAT('Pagamento confirmado — €', FORMAT(i.valor, 'N2')),
        CONCAT('O pagamento #', i.pagamento_id,
               ' de €', FORMAT(i.valor, 'N2'),
               ' (', i.metodo_pagamento, ') foi confirmado.')
    FROM inserted i
    LEFT JOIN deleted d ON i.pagamento_id = d.pagamento_id
    WHERE i.estado = 'Pago'
      AND (d.estado IS NULL OR d.estado <> 'Pago');
END;
GO

-- T16: lista de espera — não duplicar entrada ativa do mesmo cliente
CREATE TRIGGER trg_lista_espera_unica
ON lista_espera AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM   inserted i
        JOIN   lista_espera le
            ON le.cliente_id      = i.cliente_id
           AND le.recurso_id      = i.recurso_id
           AND le.data_pretendida = i.data_pretendida
           AND le.lista_espera_id <> i.lista_espera_id
           AND le.estado IN ('Aguarda','Notificado')
        WHERE i.estado IN ('Aguarda','Notificado')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50016, 'Cliente já tem entrada ativa na lista de espera para este recurso/dia.', 1;
    END
END;
GO
