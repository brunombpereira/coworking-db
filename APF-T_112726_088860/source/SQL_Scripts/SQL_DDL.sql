-- =====================================================================
-- Projeto: Sistema de Gestão de Coworking — APF-T
-- DDL Revisto (incorpora correções do professor + recurso supertype)
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
    email        NVARCHAR(255) NOT NULL UNIQUE
        CHECK (email LIKE '%@%.%' AND email NOT LIKE '%@%@%'),
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

-- sala (subtype) -------------------------------------------------------
CREATE TABLE sala (
    recurso_id  INTEGER       NOT NULL PRIMARY KEY
        REFERENCES recurso(recurso_id) ON DELETE CASCADE,
    nome        NVARCHAR(100) NOT NULL,
    capacidade  INTEGER       NOT NULL CHECK (capacidade > 0),
    preco_hora  DECIMAL(10,2) NOT NULL CHECK (preco_hora >= 0),
    estado      NVARCHAR(30)  NOT NULL DEFAULT 'Disponivel'
        CHECK (estado IN ('Disponivel','Indisponivel','Manutencao','Inativa')),
    espaco_id   INTEGER       NOT NULL
        REFERENCES espaco(espaco_id) ON DELETE NO ACTION,
    CONSTRAINT uq_sala_nome_por_espaco UNIQUE (espaco_id, nome)
);
GO

-- posto_trabalho (subtype) --------------------------------------------
CREATE TABLE posto_trabalho (
    recurso_id  INTEGER       NOT NULL PRIMARY KEY
        REFERENCES recurso(recurso_id) ON DELETE CASCADE,
    codigo      NVARCHAR(50)  NOT NULL,
    tipo_posto  NVARCHAR(30)  NOT NULL
        CHECK (tipo_posto IN ('Flex','Fixo','Privado')),
    preco_hora  DECIMAL(10,2) NOT NULL CHECK (preco_hora >= 0),
    estado      NVARCHAR(30)  NOT NULL DEFAULT 'Disponivel'
        CHECK (estado IN ('Disponivel','Indisponivel','Manutencao','Inativo')),
    espaco_id   INTEGER       NOT NULL
        REFERENCES espaco(espaco_id) ON DELETE NO ACTION,
    CONSTRAINT uq_posto_codigo_por_espaco UNIQUE (espaco_id, codigo)
);
GO

-- adesao --------------------------------------------------------------
CREATE TABLE adesao (
    adesao_id   INTEGER       IDENTITY(1,1) PRIMARY KEY,
    cliente_id  INTEGER       NOT NULL
        REFERENCES cliente(cliente_id) ON DELETE CASCADE,
    plano_id    INTEGER       NOT NULL
        REFERENCES plano(plano_id),
    data_inicio DATE          NOT NULL,
    data_fim    DATE          NULL,
    estado      NVARCHAR(30)  NOT NULL DEFAULT 'Pendente'
        CHECK (estado IN ('Pendente','Ativa','Suspensa','Cancelada','Terminada')),
    CONSTRAINT ck_adesao_datas CHECK (data_fim IS NULL OR data_fim >= data_inicio)
);
GO

-- reserva (entidade associativa Cliente M:N Recurso) ------------------
CREATE TABLE reserva (
    reserva_id        INTEGER       IDENTITY(1,1) PRIMARY KEY,
    cliente_id        INTEGER       NOT NULL
        REFERENCES cliente(cliente_id) ON DELETE CASCADE,
    recurso_id        INTEGER       NOT NULL
        REFERENCES recurso(recurso_id),
    data_reserva      DATE          NOT NULL,
    hora_inicio       TIME          NOT NULL,
    hora_fim          TIME          NOT NULL,
    estado            NVARCHAR(30)  NOT NULL DEFAULT 'Pendente'
        CHECK (estado IN ('Pendente','Confirmada','Cancelada','Concluida')),
    valor             DECIMAL(10,2) NOT NULL CHECK (valor >= 0),
    num_participantes INTEGER       NULL CHECK (num_participantes > 0),
    notas             NVARCHAR(500) NULL,
    CONSTRAINT ck_reserva_horas CHECK (hora_fim > hora_inicio)
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
CREATE INDEX idx_reserva_recurso   ON reserva  (recurso_id, data_reserva, hora_inicio, hora_fim);
CREATE INDEX idx_pagamento_adesao  ON pagamento (adesao_id);
CREATE INDEX idx_pagamento_reserva ON pagamento (reserva_id);
CREATE INDEX idx_adesao_cliente    ON adesao    (cliente_id, estado);
CREATE INDEX idx_reserva_cliente   ON reserva   (cliente_id, estado);
CREATE INDEX idx_pagamento_cliente ON pagamento  (cliente_id, estado);
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
    -- conflict with existing reservations for the same recurso
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN reserva r ON r.recurso_id   = i.recurso_id
                      AND r.reserva_id  <> i.reserva_id
                      AND r.data_reserva = i.data_reserva
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
        JOIN inserted i2 ON i1.recurso_id   = i2.recurso_id
                        AND i1.reserva_id   < i2.reserva_id
                        AND i1.data_reserva = i2.data_reserva
        WHERE i1.estado <> 'Cancelada'
          AND i2.estado <> 'Cancelada'
          AND i1.hora_inicio < i2.hora_fim
          AND i1.hora_fim    > i2.hora_inicio
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50001, 'Já existe uma reserva sobreposta para o mesmo recurso e período.', 1;
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
        JOIN recurso rc ON i.recurso_id = rc.recurso_id
        LEFT JOIN sala           s ON rc.recurso_id = s.recurso_id
        LEFT JOIN posto_trabalho p ON rc.recurso_id = p.recurso_id
        JOIN espaco e ON e.espaco_id = COALESCE(s.espaco_id, p.espaco_id)
        WHERE (i.hora_inicio < e.hora_abertura
            OR i.hora_fim    > e.hora_fecho)
          AND i.estado <> 'Cancelada'
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50002, 'A reserva está fora do horário de funcionamento do espaço.', 1;
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
        JOIN sala s ON i.recurso_id = s.recurso_id
        WHERE i.num_participantes > s.capacidade
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50003, 'O número de participantes excede a capacidade da sala.', 1;
    END
END;
GO

-- T4: adesão ativa única por cliente ----------------------------------
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
    OR
    EXISTS (
        SELECT 1
        FROM inserted i1
        JOIN inserted i2 ON i1.cliente_id = i2.cliente_id
                        AND i1.adesao_id  < i2.adesao_id
        WHERE i1.estado = 'Ativa'
          AND i2.estado = 'Ativa'
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
        JOIN plano  p ON a.plano_id  = p.plano_id
        WHERE i.adesao_id IS NOT NULL
          AND i.valor <> p.preco_mensal
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50006, 'O valor do pagamento não corresponde ao preço mensal do plano.', 1;
    END
END;
GO

-- T7: recurso deve estar Disponivel (INSERT only) ---------------------
CREATE TRIGGER trg_reserva_recurso_disponivel
ON reserva AFTER INSERT
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (
        SELECT 1
        FROM inserted i
        JOIN recurso rc ON i.recurso_id = rc.recurso_id
        LEFT JOIN sala           s ON rc.recurso_id = s.recurso_id
        LEFT JOIN posto_trabalho p ON rc.recurso_id = p.recurso_id
        WHERE (s.recurso_id IS NOT NULL AND s.estado <> 'Disponivel')
           OR (p.recurso_id IS NOT NULL AND p.estado <> 'Disponivel')
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50007, 'O recurso reservado não está disponível.', 1;
    END
END;
GO

-- T8: pagamento.cliente_id must match the service owner ---------------
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
