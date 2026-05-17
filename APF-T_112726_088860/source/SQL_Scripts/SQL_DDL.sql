-- =====================================================================
-- Projeto: Sistema de Gestão de Coworking — APF-T
-- DDL: criação da base de dados e tabelas (modelo recurso supertype +
--      posto via adesão).
-- Para a base de dados ficar completa, executar pela seguinte ordem:
--   1)  SQL_DDL.sql
--   2)  Indexes.sql
--   3)  Triggers.sql
--   4)  Views.sql
--   5)  User_defined_functions.sql
--   6)  Stored_procedures.sql
--   7)  Auth.sql
--   8)  Temporal_tables.sql   (também cria vw_reservas_historico)
--   9)  Security.sql
--   10) SQL_DML.sql           (dados de teste; depende de Auth)
-- Drop_tables.sql faz teardown completo.
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

-- posto (subtype) -----------------------------------------------------
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
-- preco_servico_snapshot fotografa o preço do serviço (reserva.valor ou
-- adesao.preco_acordado) no momento do pagamento. Torna a ligação ao
-- valor cobrado explícita no schema (correção do prof.: "Qual o preço?")
-- em vez de viver só no trigger T6.
CREATE TABLE pagamento (
    pagamento_id     INTEGER       IDENTITY(1,1) PRIMARY KEY,
    cliente_id       INTEGER       NOT NULL REFERENCES cliente(cliente_id),
    data_pagamento   DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    valor            DECIMAL(10,2) NOT NULL CHECK (valor > 0),
    preco_servico_snapshot DECIMAL(10,2) NOT NULL
        CHECK (preco_servico_snapshot > 0),
    metodo_pagamento NVARCHAR(40)  NOT NULL
        CHECK (metodo_pagamento IN ('Dinheiro','Cartao','Transferencia','MBWay','PayPal')),
    estado           NVARCHAR(30)  NOT NULL DEFAULT 'Pendente'
        CHECK (estado IN ('Pendente','Pago','Cancelado','Reembolsado')),
    adesao_id        INTEGER       NULL REFERENCES adesao(adesao_id),
    reserva_id       INTEGER       NULL REFERENCES reserva(reserva_id),
    CONSTRAINT ck_pagamento_servico CHECK (
        (CASE WHEN adesao_id  IS NULL THEN 0 ELSE 1 END) +
        (CASE WHEN reserva_id IS NULL THEN 0 ELSE 1 END) = 1
    ),
    CONSTRAINT ck_pagamento_valor_snapshot CHECK (valor = preco_servico_snapshot)
);
GO

-- =====================================================================
-- Extensões (auth + features de domínio adicionais)
-- =====================================================================

-- utilizador (auth) ---------------------------------------------------
-- password_hash = HASHBYTES('SHA2_256', salt || password)
CREATE TABLE utilizador (
    utilizador_id  INTEGER       IDENTITY(1,1) PRIMARY KEY,
    username       NVARCHAR(100) NOT NULL UNIQUE,
    password_hash  VARBINARY(64) NOT NULL,
    salt           VARBINARY(16) NOT NULL,
    role           NVARCHAR(20)  NOT NULL
        CHECK (role IN ('Admin','Staff','Cliente')),
    cliente_id     INTEGER       NULL
        REFERENCES cliente(cliente_id) ON DELETE SET NULL,
    ativo          BIT           NOT NULL DEFAULT 1,
    data_criacao   DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    ultimo_login   DATETIME2     NULL,
    CONSTRAINT ck_utilizador_cliente_role CHECK (
        (role = 'Cliente' AND cliente_id IS NOT NULL)
     OR (role IN ('Admin','Staff') AND cliente_id IS NULL)
    )
);
GO

-- politica_cancelamento ----------------------------------------------
-- Tiers de reembolso por antecedência. A política aplicada é a de maior
-- horas_minimas <= antecedência efetiva da reserva.
CREATE TABLE politica_cancelamento (
    politica_id     INTEGER       IDENTITY(1,1) PRIMARY KEY,
    nome            NVARCHAR(100) NOT NULL UNIQUE,
    horas_minimas   INTEGER       NOT NULL CHECK (horas_minimas >= 0),
    perc_reembolso  DECIMAL(5,2)  NOT NULL
        CHECK (perc_reembolso BETWEEN 0 AND 100),
    ativa           BIT           NOT NULL DEFAULT 1
);
GO

-- notificacao --------------------------------------------------------
CREATE TABLE notificacao (
    notificacao_id INTEGER        IDENTITY(1,1) PRIMARY KEY,
    cliente_id     INTEGER        NOT NULL
        REFERENCES cliente(cliente_id) ON DELETE CASCADE,
    tipo           NVARCHAR(40)   NOT NULL
        CHECK (tipo IN ('ReservaCriada','ReservaProxima','ReservaCancelada',
                        'PagamentoConfirmado','AdesaoExpirar','ListaEsperaPromovida')),
    assunto        NVARCHAR(255)  NOT NULL,
    mensagem       NVARCHAR(MAX)  NOT NULL,
    data_criacao   DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
    lida           BIT            NOT NULL DEFAULT 0,
    data_leitura   DATETIME2      NULL
);
GO

-- lista_espera -------------------------------------------------------
CREATE TABLE lista_espera (
    lista_espera_id INTEGER      IDENTITY(1,1) PRIMARY KEY,
    cliente_id      INTEGER      NOT NULL
        REFERENCES cliente(cliente_id) ON DELETE CASCADE,
    recurso_id      INTEGER      NOT NULL
        REFERENCES recurso(recurso_id),
    data_pretendida DATE         NOT NULL,
    hora_inicio     TIME         NULL,
    hora_fim        TIME         NULL,
    data_inscricao  DATETIME2    NOT NULL DEFAULT SYSDATETIME(),
    estado          NVARCHAR(20) NOT NULL DEFAULT 'Aguarda'
        CHECK (estado IN ('Aguarda','Notificado','Promovido','Cancelado')),
    reserva_id      INTEGER      NULL REFERENCES reserva(reserva_id),
    CONSTRAINT ck_lista_espera_horas CHECK (
        (hora_inicio IS NULL AND hora_fim IS NULL)
     OR (hora_inicio IS NOT NULL AND hora_fim IS NOT NULL AND hora_fim > hora_inicio)
    )
);
GO
