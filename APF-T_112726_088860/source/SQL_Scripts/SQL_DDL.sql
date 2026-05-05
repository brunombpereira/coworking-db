-- =====================================================================
-- Projeto: Sistema de Gestão de Coworking — APF-T
-- DDL Revisto (modelo redesenhado: recurso supertype + posto via adesão)
-- =====================================================================

USE master;
GO
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'CoworkingDB')
    DROP DATABASE CoworkingDB;
GO
CREATE DATABASE CoworkingDB;
GO
USE CoworkingDB;
GO

-- plano ---------------------------------------------------------------
CREATE TABLE plano (
    plano_id      INTEGER       IDENTITY(1,1) PRIMARY KEY,
    nome_plano    NVARCHAR(100) NOT NULL UNIQUE,
    tipo_plano    NVARCHAR(10)  NOT NULL
        CHECK (tipo_plano IN ('Flex','Fixo','Privado')),
    preco_mensal  DECIMAL(10,2) NOT NULL CHECK (preco_mensal >= 0),
    duracao_meses INTEGER       NOT NULL CHECK (duracao_meses > 0),
    descricao     NVARCHAR(255) NULL
);
GO

-- cliente -------------------------------------------------------------
CREATE TABLE cliente (
    cliente_id   INTEGER       IDENTITY(1,1) PRIMARY KEY,
    nome         NVARCHAR(255) NOT NULL,
    nif          CHAR(9)       NOT NULL UNIQUE
        CHECK (nif NOT LIKE '%[^0-9]%'),
    email        NVARCHAR(255) NOT NULL UNIQUE,
    telefone     NVARCHAR(20)  NULL,
    data_registo DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE)
);
GO

-- espaco --------------------------------------------------------------
CREATE TABLE espaco (
    espaco_id     INTEGER       IDENTITY(1,1) PRIMARY KEY,
    nome          NVARCHAR(120) NOT NULL UNIQUE,
    morada        NVARCHAR(255) NOT NULL,
    telefone      NVARCHAR(20)  NULL,
    email         NVARCHAR(255) NULL,
    hora_abertura TIME          NOT NULL,
    hora_fecho    TIME          NOT NULL,
    CONSTRAINT ck_espaco_horario CHECK (hora_fecho > hora_abertura)
);
GO

-- recurso (supertype) -------------------------------------------------
CREATE TABLE recurso (
    recurso_id  INTEGER      IDENTITY(1,1) PRIMARY KEY,
    tipo        NVARCHAR(10) NOT NULL CHECK (tipo IN ('Sala','Posto'))
);
GO

-- sala (subtype) ------------------------------------------------------
CREATE TABLE sala (
    recurso_id  INTEGER       NOT NULL PRIMARY KEY
        REFERENCES recurso(recurso_id) ON DELETE CASCADE,
    espaco_id   INTEGER       NOT NULL
        REFERENCES espaco(espaco_id) ON DELETE NO ACTION,
    nome        NVARCHAR(100) NOT NULL,
    capacidade  INTEGER       NOT NULL CHECK (capacidade > 0),
    preco_hora  DECIMAL(10,2) NOT NULL CHECK (preco_hora >= 0),
    estado      NVARCHAR(30)  NOT NULL DEFAULT 'Disponivel'
        CHECK (estado IN ('Disponivel','Indisponivel','Manutencao','Inativo')),
    CONSTRAINT uq_sala_nome_por_espaco UNIQUE (espaco_id, nome)
);
GO

-- posto (subtype, renomeado de posto_trabalho) ------------------------
CREATE TABLE posto (
    recurso_id  INTEGER       NOT NULL PRIMARY KEY
        REFERENCES recurso(recurso_id) ON DELETE CASCADE,
    espaco_id   INTEGER       NOT NULL
        REFERENCES espaco(espaco_id) ON DELETE NO ACTION,
    codigo      NVARCHAR(50)  NOT NULL,
    tipo_posto  NVARCHAR(30)  NOT NULL
        CHECK (tipo_posto IN ('Flex','Fixo','Privado')),
    preco_dia   DECIMAL(10,2) NOT NULL CHECK (preco_dia >= 0),
    estado      NVARCHAR(30)  NOT NULL DEFAULT 'Disponivel'
        CHECK (estado IN ('Disponivel','Indisponivel','Manutencao','Inativo')),
    CONSTRAINT uq_posto_codigo_por_espaco UNIQUE (espaco_id, codigo)
);
GO

-- adesao --------------------------------------------------------------
CREATE TABLE adesao (
    adesao_id      INTEGER       IDENTITY(1,1) PRIMARY KEY,
    cliente_id     INTEGER       NOT NULL
        REFERENCES cliente(cliente_id) ON DELETE CASCADE,
    plano_id       INTEGER       NOT NULL
        REFERENCES plano(plano_id),
    recurso_id     INTEGER       NULL
        REFERENCES recurso(recurso_id),
    data_inicio    DATE          NOT NULL,
    data_fim       DATE          NULL,
    preco_acordado DECIMAL(10,2) NOT NULL CHECK (preco_acordado >= 0),
    estado         NVARCHAR(30)  NOT NULL DEFAULT 'Pendente'
        CHECK (estado IN ('Pendente','Ativa','Suspensa','Cancelada','Terminada')),
    CONSTRAINT ck_adesao_datas CHECK (data_fim IS NULL OR data_fim >= data_inicio)
);
GO

-- reserva (Cliente M:N Recurso) ---------------------------------------
CREATE TABLE reserva (
    reserva_id        INTEGER       IDENTITY(1,1) PRIMARY KEY,
    cliente_id        INTEGER       NOT NULL
        REFERENCES cliente(cliente_id) ON DELETE CASCADE,
    recurso_id        INTEGER       NOT NULL
        REFERENCES recurso(recurso_id),
    data_reserva      DATE          NOT NULL,
    hora_inicio       TIME          NULL,
    hora_fim          TIME          NULL,
    valor             DECIMAL(10,2) NOT NULL CHECK (valor >= 0),
    estado            NVARCHAR(30)  NOT NULL DEFAULT 'Pendente'
        CHECK (estado IN ('Pendente','Confirmada','Cancelada','Concluida')),
    num_participantes INTEGER       NULL CHECK (num_participantes > 0),
    notas             NVARCHAR(500) NULL,
    CONSTRAINT ck_reserva_horas CHECK (
        (hora_inicio IS NULL AND hora_fim IS NULL)
     OR (hora_inicio IS NOT NULL AND hora_fim IS NOT NULL AND hora_fim > hora_inicio)
    )
);
GO

-- pagamento -----------------------------------------------------------
CREATE TABLE pagamento (
    pagamento_id     INTEGER       IDENTITY(1,1) PRIMARY KEY,
    cliente_id       INTEGER       NOT NULL REFERENCES cliente(cliente_id),
    data_pagamento   DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    valor            DECIMAL(10,2) NOT NULL CHECK (valor > 0),
    metodo_pagamento NVARCHAR(40)  NOT NULL
        CHECK (metodo_pagamento IN ('Dinheiro','Cartao','Transferencia','MBWay','PayPal')),
    estado           NVARCHAR(30)  NOT NULL DEFAULT 'Pendente'
        CHECK (estado IN ('Pendente','Pago','Cancelado','Reembolsado')),
    adesao_id        INTEGER       NULL REFERENCES adesao(adesao_id),
    reserva_id       INTEGER       NULL REFERENCES reserva(reserva_id),
    CONSTRAINT ck_pagamento_servico CHECK (
        (CASE WHEN adesao_id  IS NULL THEN 0 ELSE 1 END) +
        (CASE WHEN reserva_id IS NULL THEN 0 ELSE 1 END) = 1
    )
);
GO

-- =====================================================================
-- Índices
-- =====================================================================
SET QUOTED_IDENTIFIER ON;
GO
CREATE INDEX idx_reserva_recurso   ON reserva   (recurso_id, data_reserva, hora_inicio, hora_fim);
CREATE INDEX idx_pagamento_adesao  ON pagamento (adesao_id)  WHERE adesao_id  IS NOT NULL;
CREATE INDEX idx_pagamento_reserva ON pagamento (reserva_id) WHERE reserva_id IS NOT NULL;
CREATE INDEX idx_adesao_cliente    ON adesao    (cliente_id, estado);
CREATE INDEX idx_adesao_recurso    ON adesao    (recurso_id) WHERE recurso_id IS NOT NULL;
CREATE INDEX idx_reserva_cliente   ON reserva   (cliente_id, estado);
CREATE INDEX idx_pagamento_cliente ON pagamento (cliente_id, estado);
GO

-- =====================================================================
-- Triggers
-- =====================================================================

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
              -- Sala vs sala: sobreposição horária
              (i.hora_inicio IS NOT NULL AND r.hora_inicio IS NOT NULL
               AND i.hora_inicio < r.hora_fim AND i.hora_fim > r.hora_inicio)
              -- Posto vs posto (ou misto): mesmo dia já chega
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
-- Só se aplica a reservas de sala (têm hora_inicio/hora_fim).
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

-- T6: pagamento.valor deve corresponder ao preço do serviço ----------
-- Adesão valida contra preco_acordado (snapshot), não plano.preco_mensal.
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
          AND i.valor <> r.valor
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50005, 'O valor do pagamento não corresponde ao valor da reserva.', 1;
    END
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN adesao a ON i.adesao_id = a.adesao_id
        WHERE i.adesao_id IS NOT NULL
          AND i.valor <> a.preco_acordado
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50006, 'O valor do pagamento não corresponde ao preço acordado da adesão.', 1;
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
-- Flex   => recurso_id IS NULL
-- Fixo/Privado => recurso_id NOT NULL, recurso é Posto, posto.tipo_posto = plano.tipo_plano
-- Sem duas adesões Pendente/Ativa para o mesmo recurso em datas sobrepostas.
CREATE TRIGGER trg_adesao_recurso_coerente
ON adesao AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- Flex com recurso_id atribuído
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN plano pl ON i.plano_id = pl.plano_id
        WHERE pl.tipo_plano = 'Flex' AND i.recurso_id IS NOT NULL
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50009, 'Adesão Flex não pode ter recurso atribuído.', 1;
    END
    -- Fixo/Privado sem recurso_id
    IF EXISTS (
        SELECT 1 FROM inserted i
        JOIN plano pl ON i.plano_id = pl.plano_id
        WHERE pl.tipo_plano IN ('Fixo','Privado') AND i.recurso_id IS NULL
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50009, 'Adesão Fixo/Privado tem de ter um posto atribuído.', 1;
    END
    -- Fixo/Privado a apontar para sala (não é posto)
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
    -- Fixo/Privado: tipo_posto deve coincidir com tipo_plano
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
    -- Duas adesões Pendente/Ativa para o mesmo recurso com datas sobrepostas
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

-- T10: snapshot de preco_acordado quando NULL ------------------------
-- (só fill — não dispara erro)
-- Defesa adicional para garantir que preco_acordado nunca fica vazio
-- mesmo que a aplicação se esqueça de o preencher.
-- INSTEAD OF para correr antes do CHECK NOT NULL.
CREATE TRIGGER trg_adesao_preco_snapshot
ON adesao INSTEAD OF INSERT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio, data_fim, preco_acordado, estado)
    SELECT
        i.cliente_id,
        i.plano_id,
        i.recurso_id,
        i.data_inicio,
        i.data_fim,
        COALESCE(i.preco_acordado, p.preco_mensal),
        i.estado
    FROM inserted i
    JOIN plano p ON i.plano_id = p.plano_id;
END;
GO

-- T11: reserva avulsa de posto não colide com adesão Ativa do cliente
-- - Cliente com adesão Flex Ativa não pode reservar avulso um posto Flex no mesmo dia.
-- - Cliente não pode reservar avulso um posto que está atribuído via adesão Fixo/Privado Ativa
--   (de qualquer cliente).
CREATE TRIGGER trg_reserva_posto_sem_adesao
ON reserva AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- (a) cliente já tem adesão Flex Ativa cobrindo o dia da reserva avulsa Flex
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
    -- (b) posto Fixo/Privado já atribuído via adesão Ativa (qualquer cliente)
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
    -- Sala obriga horas e proíbe horas vazias
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
    -- Posto proíbe horas e num_participantes
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
