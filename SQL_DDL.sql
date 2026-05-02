-- =====================================================================
-- Projeto: Sistema de Gestão de Coworking — APF-T
-- DDL Revisto (incorpora correções do professor + 10 melhorias)
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
    plano_id      INTEGER      IDENTITY(1,1) PRIMARY KEY,
    nome_plano    VARCHAR(100) NOT NULL UNIQUE,
    preco_mensal  MONEY        NOT NULL CHECK (preco_mensal >= 0),
    duracao_meses INTEGER      NOT NULL CHECK (duracao_meses > 0),
    descricao     VARCHAR(255) NULL
);
GO

-- cliente -------------------------------------------------------------
CREATE TABLE cliente (
    cliente_id   INTEGER      IDENTITY(1,1) PRIMARY KEY,
    nome         VARCHAR(255) NOT NULL,
    nif          CHAR(9)      NOT NULL UNIQUE
        CHECK (nif NOT LIKE '%[^0-9]%'),
    email        VARCHAR(255) NOT NULL UNIQUE
        CHECK (email LIKE '%@%.%' AND email NOT LIKE '%@%@%'),
    telefone     VARCHAR(20)  NULL UNIQUE,
    data_registo DATE         NOT NULL DEFAULT CAST(GETDATE() AS DATE)
);
GO

-- espaco --------------------------------------------------------------
CREATE TABLE espaco (
    espaco_id     INTEGER      IDENTITY(1,1) PRIMARY KEY,
    nome          VARCHAR(120) NOT NULL UNIQUE,
    morada        VARCHAR(255) NOT NULL,
    telefone      VARCHAR(20)  NULL,
    email         VARCHAR(255) NULL,
    hora_abertura TIME         NOT NULL,
    hora_fecho    TIME         NOT NULL,
    CONSTRAINT ck_espaco_horario CHECK (hora_fecho > hora_abertura)
);
GO

-- sala ----------------------------------------------------------------
CREATE TABLE sala (
    sala_id    INTEGER      IDENTITY(1,1) PRIMARY KEY,
    nome       VARCHAR(100) NOT NULL,
    capacidade INTEGER      NOT NULL CHECK (capacidade > 0),
    preco_hora MONEY        NOT NULL CHECK (preco_hora >= 0),
    estado     VARCHAR(30)  NOT NULL DEFAULT 'Disponivel'
        CHECK (estado IN ('Disponivel','Indisponivel','Manutencao','Inativa')),
    espaco_id  INTEGER      NOT NULL
        REFERENCES espaco(espaco_id) ON DELETE NO ACTION,
    CONSTRAINT uq_sala_nome_por_espaco UNIQUE (espaco_id, nome)
);
GO

-- posto_trabalho ------------------------------------------------------
CREATE TABLE posto_trabalho (
    posto_id   INTEGER     IDENTITY(1,1) PRIMARY KEY,
    codigo     VARCHAR(50) NOT NULL,
    tipo       VARCHAR(30) NOT NULL
        CHECK (tipo IN ('Flex','Fixo','Privado')),
    preco_hora MONEY       NOT NULL CHECK (preco_hora >= 0),
    estado     VARCHAR(30) NOT NULL DEFAULT 'Disponivel'
        CHECK (estado IN ('Disponivel','Indisponivel','Manutencao','Inativo')),
    espaco_id  INTEGER     NOT NULL
        REFERENCES espaco(espaco_id) ON DELETE NO ACTION,
    CONSTRAINT uq_posto_codigo_por_espaco UNIQUE (espaco_id, codigo)
);
GO

-- adesao --------------------------------------------------------------
CREATE TABLE adesao (
    adesao_id  INTEGER     IDENTITY(1,1) PRIMARY KEY,
    cliente_id INTEGER     NOT NULL
        REFERENCES cliente(cliente_id) ON DELETE CASCADE,
    plano_id   INTEGER     NOT NULL
        REFERENCES plano(plano_id),
    data_inicio DATE       NOT NULL,
    data_fim    DATE       NULL,
    estado      VARCHAR(30) NOT NULL DEFAULT 'Pendente'
        CHECK (estado IN ('Pendente','Ativa','Suspensa','Cancelada','Terminada')),
    CONSTRAINT ck_adesao_datas CHECK (data_fim IS NULL OR data_fim >= data_inicio)
);
GO

-- reserva (unificada — substitui reserva_sala + reserva_posto) --------
CREATE TABLE reserva (
    reserva_id        INTEGER      IDENTITY(1,1) PRIMARY KEY,
    cliente_id        INTEGER      NOT NULL
        REFERENCES cliente(cliente_id) ON DELETE CASCADE,
    sala_id           INTEGER      NULL REFERENCES sala(sala_id),
    posto_id          INTEGER      NULL REFERENCES posto_trabalho(posto_id),
    data_reserva      DATE         NOT NULL,
    hora_inicio       TIME         NOT NULL,
    hora_fim          TIME         NOT NULL,
    estado            VARCHAR(30)  NOT NULL DEFAULT 'Pendente'
        CHECK (estado IN ('Pendente','Confirmada','Cancelada','Concluida')),
    valor             MONEY        NOT NULL CHECK (valor >= 0),
    num_participantes INTEGER      NULL CHECK (num_participantes > 0),
    notas             VARCHAR(500) NULL,
    CONSTRAINT ck_reserva_tipo CHECK (
        (sala_id IS NOT NULL AND posto_id IS NULL) OR
        (sala_id IS NULL     AND posto_id IS NOT NULL)
    ),
    CONSTRAINT ck_reserva_horas CHECK (hora_fim > hora_inicio)
);
GO

-- pagamento -----------------------------------------------------------
CREATE TABLE pagamento (
    pagamento_id     INTEGER     IDENTITY(1,1) PRIMARY KEY,
    cliente_id       INTEGER     NOT NULL REFERENCES cliente(cliente_id),
    data_pagamento   DATE        NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    valor            MONEY       NOT NULL CHECK (valor > 0),
    metodo_pagamento VARCHAR(40) NOT NULL
        CHECK (metodo_pagamento IN ('Dinheiro','Cartao','Transferencia','MBWay','PayPal')),
    estado           VARCHAR(30) NOT NULL DEFAULT 'Pendente'
        CHECK (estado IN ('Pendente','Pago','Cancelado','Reembolsado')),
    adesao_id        INTEGER     NULL REFERENCES adesao(adesao_id),
    reserva_id       INTEGER     NULL REFERENCES reserva(reserva_id),
    CONSTRAINT ck_pagamento_servico CHECK (
        (CASE WHEN adesao_id  IS NULL THEN 0 ELSE 1 END) +
        (CASE WHEN reserva_id IS NULL THEN 0 ELSE 1 END) = 1
    )
);
GO

-- =====================================================================
-- Índices
-- =====================================================================
CREATE INDEX idx_reserva_sala      ON reserva  (sala_id,  data_reserva, hora_inicio, hora_fim);
CREATE INDEX idx_reserva_posto     ON reserva  (posto_id, data_reserva, hora_inicio, hora_fim);
CREATE INDEX idx_pagamento_adesao  ON pagamento(adesao_id);
CREATE INDEX idx_pagamento_reserva ON pagamento(reserva_id);
CREATE INDEX idx_adesao_cliente    ON adesao   (cliente_id, estado);
GO

-- =====================================================================
-- Triggers
-- =====================================================================

-- T1: sem sobreposição de reservas ------------------------------------
CREATE TRIGGER trg_reserva_sem_sobreposicao
ON reserva AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- conflict with existing reservations
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN reserva r ON (
            (i.sala_id  IS NOT NULL AND r.sala_id  = i.sala_id ) OR
            (i.posto_id IS NOT NULL AND r.posto_id = i.posto_id)
        )
        AND r.data_reserva = i.data_reserva
        AND r.reserva_id  <> i.reserva_id
        WHERE i.estado <> 'Cancelada'
          AND r.estado <> 'Cancelada'
          AND i.hora_inicio < r.hora_fim
          AND i.hora_fim    > r.hora_inicio
    )
    OR
    -- conflict between rows in the same batch
    EXISTS (
        SELECT 1
        FROM inserted i1
        JOIN inserted i2 ON (
            (i1.sala_id  IS NOT NULL AND i1.sala_id  = i2.sala_id ) OR
            (i1.posto_id IS NOT NULL AND i1.posto_id = i2.posto_id)
        )
        AND i1.reserva_id   < i2.reserva_id
        AND i1.data_reserva = i2.data_reserva
        WHERE i1.estado <> 'Cancelada'
          AND i2.estado <> 'Cancelada'
          AND i1.hora_inicio < i2.hora_fim
          AND i1.hora_fim    > i2.hora_inicio
    )
    BEGIN
        RAISERROR('Já existe uma reserva sobreposta para o mesmo recurso e período.',16,1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- T2: reserva dentro do horário do espaço ----------------------------
CREATE TRIGGER trg_reserva_horario_espaco
ON reserva AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN sala           s ON i.sala_id  = s.sala_id
        LEFT JOIN posto_trabalho p ON i.posto_id = p.posto_id
        JOIN espaco e ON e.espaco_id = COALESCE(s.espaco_id, p.espaco_id)
        WHERE (i.hora_inicio < e.hora_abertura
            OR i.hora_fim    > e.hora_fecho)
          AND i.estado <> 'Cancelada'
    )
    BEGIN
        RAISERROR('A reserva está fora do horário de funcionamento do espaço.',16,1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- T3: num_participantes <= capacidade da sala -------------------------
CREATE TRIGGER trg_reserva_capacidade
ON reserva AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN sala s ON i.sala_id = s.sala_id
        WHERE i.num_participantes > s.capacidade
    )
    BEGIN
        RAISERROR('O número de participantes excede a capacidade da sala.',16,1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- T4: adesão ativa única por cliente ----------------------------------
CREATE TRIGGER trg_adesao_ativa_unica
ON adesao AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- conflict with existing active adesoes
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN adesao a ON a.cliente_id = i.cliente_id
                     AND a.adesao_id <> i.adesao_id
                     AND a.estado     = 'Ativa'
        WHERE i.estado = 'Ativa'
    )
    OR
    -- conflict between rows in the same batch
    EXISTS (
        SELECT 1
        FROM inserted i1
        JOIN inserted i2 ON i1.cliente_id = i2.cliente_id
                        AND i1.adesao_id  < i2.adesao_id
        WHERE i1.estado = 'Ativa'
          AND i2.estado = 'Ativa'
    )
    BEGIN
        RAISERROR('O cliente já tem uma adesão ativa.',16,1);
        ROLLBACK TRANSACTION;
    END
END;
GO

-- T5: calcular data_fim da adesão automaticamente --------------------
CREATE TRIGGER trg_adesao_data_fim
ON adesao AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    -- recalculate data_fim when data_inicio or plano_id changes, or on new insert with NULL data_fim
    UPDATE a
    SET    a.data_fim = DATEADD(MONTH, p.duracao_meses, i.data_inicio)
    FROM   adesao  a
    JOIN   inserted i ON a.adesao_id = i.adesao_id
    JOIN   plano    p ON i.plano_id  = p.plano_id
    WHERE  i.data_fim IS NULL
       OR  (UPDATE(data_inicio) OR UPDATE(plano_id));
END;
GO

-- T6: recurso deve estar Disponivel ----------------------------------
CREATE TRIGGER trg_reserva_recurso_disponivel
ON reserva AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        LEFT JOIN sala           s ON i.sala_id  = s.sala_id
        LEFT JOIN posto_trabalho p ON i.posto_id = p.posto_id
        WHERE (s.sala_id  IS NOT NULL AND s.estado  <> 'Disponivel')
           OR (p.posto_id IS NOT NULL AND p.estado  <> 'Disponivel')
    )
    BEGIN
        RAISERROR('O recurso reservado não está disponível.',16,1);
        ROLLBACK TRANSACTION;
    END
END;
GO
