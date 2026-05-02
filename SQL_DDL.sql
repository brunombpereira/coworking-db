-- =====================================================================
-- Projeto: Sistema de Gestão de Coworking e Reserva de Salas
-- =====================================================================

-- Tipos base -----------------------------------------------------------
CREATE TYPE NO_TELEFONE
FROM VARCHAR(20) NOT NULL;
GO

CREATE TYPE EMAILADDRESS
FROM VARCHAR(255) NOT NULL;
GO

-- =====================================================================
-- Tabelas principais
-- =====================================================================

-- Plano ---------------------------------------------------------------
CREATE TABLE plano (
    plano_id INTEGER IDENTITY(1,1) PRIMARY KEY,
    nome_plano VARCHAR(100) NOT NULL UNIQUE,
    preco_mensal MONEY NOT NULL CHECK (preco_mensal >= 0),
    duracao_meses INTEGER NOT NULL CHECK (duracao_meses > 0),
    descricao VARCHAR(255)
);
GO

-- Cliente -------------------------------------------------------------
CREATE TABLE cliente (
    cliente_id INTEGER IDENTITY(1,1) PRIMARY KEY,
    nome VARCHAR(255) NOT NULL,
    nif CHAR(9) NOT NULL UNIQUE CHECK (
        LEN(nif) = 9
        AND nif NOT LIKE '%[^0-9]%'
    ),
    email EMAILADDRESS NOT NULL UNIQUE CHECK (
        email LIKE '%@%.%'
        AND email NOT LIKE '%@%@%'
    ),
    telefone NO_TELEFONE UNIQUE,
    data_registo DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE)
);
GO

-- Espaço --------------------------------------------------------------
CREATE TABLE espaco (
    espaco_id INTEGER IDENTITY(1,1) PRIMARY KEY,
    nome VARCHAR(120) NOT NULL UNIQUE,
    morada VARCHAR(255) NOT NULL,
    hora_abertura TIME NOT NULL,
    hora_fecho TIME NOT NULL,
    CONSTRAINT ck_espaco_horario CHECK (hora_fecho > hora_abertura)
);
GO

-- Sala ----------------------------------------------------------------
CREATE TABLE sala (
    sala_id INTEGER IDENTITY(1,1) PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    capacidade INTEGER NOT NULL CHECK (capacidade > 0),
    preco_hora MONEY NOT NULL CHECK (preco_hora >= 0),
    estado VARCHAR(30) NOT NULL DEFAULT 'Disponivel' CHECK (
        estado IN ('Disponivel', 'Indisponivel', 'Manutencao', 'Inativa')
    ),
    espaco_id INTEGER NOT NULL REFERENCES espaco(espaco_id) ON DELETE CASCADE,
    CONSTRAINT uq_sala_nome_por_espaco UNIQUE (espaco_id, nome)
);
GO

-- Posto de Trabalho ---------------------------------------------------
CREATE TABLE posto_trabalho (
    posto_id INTEGER IDENTITY(1,1) PRIMARY KEY,
    codigo VARCHAR(50) NOT NULL,
    tipo VARCHAR(30) NOT NULL CHECK (
        tipo IN ('Flex', 'Fixo', 'Privado')
    ),
    estado VARCHAR(30) NOT NULL DEFAULT 'Disponivel' CHECK (
        estado IN ('Disponivel', 'Indisponivel', 'Manutencao', 'Inativo')
    ),
    espaco_id INTEGER NOT NULL REFERENCES espaco(espaco_id) ON DELETE CASCADE,
    CONSTRAINT uq_posto_codigo_por_espaco UNIQUE (espaco_id, codigo)
);
GO

-- Adesão --------------------------------------------------------------
CREATE TABLE adesao (
    adesao_id INTEGER IDENTITY(1,1) PRIMARY KEY,
    cliente_id INTEGER NOT NULL REFERENCES cliente(cliente_id) ON DELETE CASCADE,
    plano_id INTEGER NOT NULL REFERENCES plano(plano_id),
    data_inicio DATE NOT NULL,
    data_fim DATE,
    estado VARCHAR(30) NOT NULL DEFAULT 'Pendente' CHECK (
        estado IN ('Pendente', 'Ativa', 'Suspensa', 'Cancelada', 'Terminada')
    ),
    CONSTRAINT ck_adesao_datas CHECK (
        data_fim IS NULL OR data_fim >= data_inicio
    )
);
GO

-- Reserva Sala --------------------------------------------------------
CREATE TABLE reserva_sala (
    reserva_sala_id INTEGER IDENTITY(1,1) PRIMARY KEY,
    cliente_id INTEGER NOT NULL REFERENCES cliente(cliente_id) ON DELETE CASCADE,
    sala_id INTEGER NOT NULL REFERENCES sala(sala_id),
    data_reserva DATE NOT NULL,
    hora_inicio TIME NOT NULL,
    hora_fim TIME NOT NULL,
    estado VARCHAR(30) NOT NULL DEFAULT 'Pendente' CHECK (
        estado IN ('Pendente', 'Confirmada', 'Cancelada', 'Concluida')
    ),
    CONSTRAINT ck_reserva_sala_horas CHECK (hora_fim > hora_inicio)
);
GO

-- Reserva Posto -------------------------------------------------------
CREATE TABLE reserva_posto (
    reserva_posto_id INTEGER IDENTITY(1,1) PRIMARY KEY,
    cliente_id INTEGER NOT NULL REFERENCES cliente(cliente_id) ON DELETE CASCADE,
    posto_id INTEGER NOT NULL REFERENCES posto_trabalho(posto_id),
    data_reserva DATE NOT NULL,
    hora_inicio TIME NOT NULL,
    hora_fim TIME NOT NULL,
    estado VARCHAR(30) NOT NULL DEFAULT 'Pendente' CHECK (
        estado IN ('Pendente', 'Confirmada', 'Cancelada', 'Concluida')
    ),
    CONSTRAINT ck_reserva_posto_horas CHECK (hora_fim > hora_inicio)
);
GO

-- Pagamento -----------------------------------------------------------
CREATE TABLE pagamento (
    pagamento_id INTEGER IDENTITY(1,1) PRIMARY KEY,
    data_pagamento DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    valor MONEY NOT NULL CHECK (valor >= 0),
    metodo_pagamento VARCHAR(40) NOT NULL CHECK (
        metodo_pagamento IN ('Dinheiro', 'Cartao', 'Transferencia', 'MBWay', 'PayPal')
    ),
    estado VARCHAR(30) NOT NULL DEFAULT 'Pendente' CHECK (
        estado IN ('Pendente', 'Pago', 'Cancelado', 'Reembolsado')
    ),
    adesao_id INTEGER NULL REFERENCES adesao(adesao_id),
    reserva_sala_id INTEGER NULL REFERENCES reserva_sala(reserva_sala_id),
    reserva_posto_id INTEGER NULL REFERENCES reserva_posto(reserva_posto_id),
    CONSTRAINT ck_pagamento_servico CHECK (
        (CASE WHEN adesao_id IS NULL THEN 0 ELSE 1 END) +
        (CASE WHEN reserva_sala_id IS NULL THEN 0 ELSE 1 END) +
        (CASE WHEN reserva_posto_id IS NULL THEN 0 ELSE 1 END) = 1
    )
);
GO

-- =====================================================================
-- Índices úteis
-- =====================================================================

CREATE INDEX idx_reserva_sala_consulta
ON reserva_sala (sala_id, data_reserva, hora_inicio, hora_fim);
GO

CREATE INDEX idx_reserva_posto_consulta
ON reserva_posto (posto_id, data_reserva, hora_inicio, hora_fim);
GO

CREATE INDEX idx_pagamento_adesao
ON pagamento (adesao_id);
GO

CREATE INDEX idx_pagamento_reserva_sala
ON pagamento (reserva_sala_id);
GO

CREATE INDEX idx_pagamento_reserva_posto
ON pagamento (reserva_posto_id);
GO

-- =====================================================================
-- Triggers para impedir sobreposição de reservas
-- =====================================================================

CREATE TRIGGER trg_reserva_sala_sem_sobreposicao
ON reserva_sala
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN reserva_sala r
            ON r.sala_id = i.sala_id
            AND r.data_reserva = i.data_reserva
            AND r.reserva_sala_id <> i.reserva_sala_id
        WHERE i.estado <> 'Cancelada'
          AND r.estado <> 'Cancelada'
          AND i.hora_inicio < r.hora_fim
          AND i.hora_fim > r.hora_inicio
    )
    BEGIN
        RAISERROR('Ja existe uma reserva de sala sobreposta para o mesmo periodo.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

CREATE TRIGGER trg_reserva_posto_sem_sobreposicao
ON reserva_posto
AFTER INSERT, UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1
        FROM inserted i
        INNER JOIN reserva_posto r
            ON r.posto_id = i.posto_id
            AND r.data_reserva = i.data_reserva
            AND r.reserva_posto_id <> i.reserva_posto_id
        WHERE i.estado <> 'Cancelada'
          AND r.estado <> 'Cancelada'
          AND i.hora_inicio < r.hora_fim
          AND i.hora_fim > r.hora_inicio
    )
    BEGIN
        RAISERROR('Ja existe uma reserva de posto sobreposto para o mesmo periodo.', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO
