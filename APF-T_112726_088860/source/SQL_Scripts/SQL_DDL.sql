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
