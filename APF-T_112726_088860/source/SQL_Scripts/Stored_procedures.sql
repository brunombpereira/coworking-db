-- =====================================================================
-- Stored procedures da CoworkingDB
-- Encapsulam as operações principais para uso pela aplicação C#.
-- Concorrência: SPs de criar reserva usam sp_getapplock por
-- (recurso_id, data_reserva) para evitar race-condition entre dois
-- INSERTs concorrentes que ambos passariam o trigger de sobreposição.
-- =====================================================================
USE CoworkingDB;
GO

-- Registar cliente -----------------------------------------------------
CREATE OR ALTER PROCEDURE sp_registar_cliente
    @nome       NVARCHAR(255),
    @nif        CHAR(9),
    @email      NVARCHAR(255),
    @telefone   NVARCHAR(20)  = NULL,
    @cliente_id INTEGER       OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO cliente (nome, nif, email, telefone)
    VALUES (@nome, @nif, @email, @telefone);
    SET @cliente_id = SCOPE_IDENTITY();
END;
GO

-- Criar adesão (snapshot de preço inline, depois do drop do trigger T10)
CREATE OR ALTER PROCEDURE sp_criar_adesao
    @cliente_id     INTEGER,
    @plano_id       INTEGER,
    @recurso_id     INTEGER       = NULL,
    @data_inicio    DATE,
    @preco_acordado DECIMAL(10,2) = NULL,
    @estado         NVARCHAR(30)  = 'Pendente'
AS
BEGIN
    SET NOCOUNT ON;

    IF @preco_acordado IS NULL
    BEGIN
        SELECT @preco_acordado = preco_mensal
        FROM   plano
        WHERE  plano_id = @plano_id;

        IF @preco_acordado IS NULL
        BEGIN
            THROW 51010, 'Plano inexistente — não foi possível inferir preco_acordado.', 1;
        END
    END

    INSERT INTO adesao (cliente_id, plano_id, recurso_id, data_inicio,
                        preco_acordado, estado)
    VALUES (@cliente_id, @plano_id, @recurso_id, @data_inicio,
            @preco_acordado, @estado);
END;
GO

-- Cancelar adesão ------------------------------------------------------
CREATE OR ALTER PROCEDURE sp_cancelar_adesao
    @adesao_id INTEGER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE adesao
       SET estado   = 'Cancelada',
           data_fim = CAST(GETDATE() AS DATE)
     WHERE adesao_id = @adesao_id;
END;
GO

-- Criar reserva de sala (com app lock) --------------------------------
CREATE OR ALTER PROCEDURE sp_criar_reserva_sala
    @cliente_id        INTEGER,
    @recurso_id        INTEGER,
    @data_reserva      DATE,
    @hora_inicio       TIME,
    @hora_fim          TIME,
    @num_participantes INTEGER       = NULL,
    @notas             NVARCHAR(500) = NULL,
    @reserva_id        INTEGER       OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @preco DECIMAL(10,2), @horas DECIMAL(10,4), @valor DECIMAL(10,2);

    SELECT @preco = preco_hora FROM sala WHERE recurso_id = @recurso_id;
    IF @preco IS NULL
    BEGIN
        THROW 51001, 'Recurso indicado não é uma sala.', 1;
    END

    SET @horas = DATEDIFF(MINUTE, @hora_inicio, @hora_fim) / 60.0;
    SET @valor = @preco * @horas;

    DECLARE @lock_res NVARCHAR(255) =
        CONCAT('reserva_recurso_', @recurso_id, '_', CONVERT(NVARCHAR(10), @data_reserva, 23));
    DECLARE @lock_rc INT;

    BEGIN TRANSACTION;

    EXEC @lock_rc = sp_getapplock
        @Resource    = @lock_res,
        @LockMode    = 'Exclusive',
        @LockOwner   = 'Transaction',
        @LockTimeout = 5000;

    IF @lock_rc < 0
    BEGIN
        ROLLBACK;
        THROW 51020, 'Não foi possível obter lock exclusivo no recurso/data.', 1;
    END

    INSERT INTO reserva (cliente_id, recurso_id, data_reserva,
                         hora_inicio, hora_fim, valor, estado,
                         num_participantes, notas)
    VALUES (@cliente_id, @recurso_id, @data_reserva,
            @hora_inicio, @hora_fim, @valor, 'Pendente',
            @num_participantes, @notas);

    SET @reserva_id = SCOPE_IDENTITY();

    COMMIT;
END;
GO

-- Criar reserva de posto (dia inteiro, com app lock) ------------------
CREATE OR ALTER PROCEDURE sp_criar_reserva_posto
    @cliente_id   INTEGER,
    @recurso_id   INTEGER,
    @data_reserva DATE,
    @notas        NVARCHAR(500) = NULL,
    @reserva_id   INTEGER       OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @valor DECIMAL(10,2);
    SELECT @valor = preco_dia FROM posto WHERE recurso_id = @recurso_id;
    IF @valor IS NULL
    BEGIN
        THROW 51002, 'Recurso indicado não é um posto.', 1;
    END

    DECLARE @lock_res NVARCHAR(255) =
        CONCAT('reserva_recurso_', @recurso_id, '_', CONVERT(NVARCHAR(10), @data_reserva, 23));
    DECLARE @lock_rc INT;

    BEGIN TRANSACTION;

    EXEC @lock_rc = sp_getapplock
        @Resource    = @lock_res,
        @LockMode    = 'Exclusive',
        @LockOwner   = 'Transaction',
        @LockTimeout = 5000;

    IF @lock_rc < 0
    BEGIN
        ROLLBACK;
        THROW 51020, 'Não foi possível obter lock exclusivo no recurso/data.', 1;
    END

    INSERT INTO reserva (cliente_id, recurso_id, data_reserva,
                         hora_inicio, hora_fim, valor, estado, notas)
    VALUES (@cliente_id, @recurso_id, @data_reserva,
            NULL, NULL, @valor, 'Pendente', @notas);

    SET @reserva_id = SCOPE_IDENTITY();

    COMMIT;
END;
GO

-- Cancelar reserva (simples) ------------------------------------------
CREATE OR ALTER PROCEDURE sp_cancelar_reserva
    @reserva_id INTEGER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE reserva SET estado = 'Cancelada' WHERE reserva_id = @reserva_id;
END;
GO

-- Cancelar reserva com cálculo de reembolso ---------------------------
-- Aplica a politica_cancelamento com horas_minimas mais alta que <=
-- antecedência. Marca o pagamento original como 'Reembolsado' se a %
-- de reembolso for 100; caso contrário cria um pagamento negativo? No
-- nosso schema valor > 0, por isso registamos só um novo pagamento
-- 'Reembolsado' com o valor reembolsado (informativo).
CREATE OR ALTER PROCEDURE sp_cancelar_reserva_com_reembolso
    @reserva_id      INTEGER,
    @valor_reembolso DECIMAL(10,2) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @cliente_id INTEGER, @data_reserva DATE, @hora_inicio TIME,
            @valor      DECIMAL(10,2), @estado NVARCHAR(30),
            @antecedencia_h INTEGER, @perc DECIMAL(5,2),
            @inicio_dt DATETIME2;

    SELECT @cliente_id = cliente_id, @data_reserva = data_reserva,
           @hora_inicio = hora_inicio, @valor = valor, @estado = estado
    FROM   reserva
    WHERE  reserva_id = @reserva_id;

    IF @cliente_id IS NULL
    BEGIN
        THROW 51030, 'Reserva inexistente.', 1;
    END

    IF @estado = 'Cancelada'
    BEGIN
        THROW 51031, 'Reserva já está cancelada.', 1;
    END

    SET @inicio_dt = CAST(@data_reserva AS DATETIME2)
                   + CAST(COALESCE(@hora_inicio, '00:00') AS DATETIME2);
    SET @antecedencia_h = DATEDIFF(HOUR, SYSDATETIME(), @inicio_dt);

    SELECT TOP 1 @perc = perc_reembolso
    FROM   politica_cancelamento
    WHERE  ativa = 1 AND horas_minimas <= @antecedencia_h
    ORDER BY horas_minimas DESC;

    SET @perc = COALESCE(@perc, 0);
    SET @valor_reembolso = ROUND(@valor * @perc / 100, 2);

    BEGIN TRANSACTION;

    UPDATE reserva SET estado = 'Cancelada' WHERE reserva_id = @reserva_id;

    UPDATE pagamento
       SET estado = 'Reembolsado'
     WHERE reserva_id = @reserva_id AND estado = 'Pago';

    COMMIT;
END;
GO

-- Criar reservas recorrentes ------------------------------------------
-- Cria N reservas — uma por cada @dia_semana entre @data_inicio e
-- @data_fim. dia_semana: 1=Domingo .. 7=Sábado (DATEPART(weekday,...) com
-- DATEFIRST padrão US).
-- Reservas que falhem (sobreposição, etc) são saltadas — devolve resultset
-- com data + sucesso/erro.
CREATE OR ALTER PROCEDURE sp_criar_reserva_recorrente
    @cliente_id        INTEGER,
    @recurso_id        INTEGER,
    @dia_semana        INTEGER,
    @hora_inicio       TIME,
    @hora_fim          TIME,
    @data_inicio       DATE,
    @data_fim          DATE,
    @num_participantes INTEGER = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET DATEFIRST 7;  -- domingo = 1

    DECLARE @results TABLE (data_reserva DATE, sucesso BIT, mensagem NVARCHAR(500));
    DECLARE @cur DATE = @data_inicio, @rid INT;

    WHILE @cur <= @data_fim
    BEGIN
        IF DATEPART(WEEKDAY, @cur) = @dia_semana
        BEGIN
            BEGIN TRY
                EXEC sp_criar_reserva_sala
                    @cliente_id, @recurso_id, @cur,
                    @hora_inicio, @hora_fim,
                    @num_participantes, NULL,
                    @reserva_id = @rid OUTPUT;

                INSERT INTO @results VALUES (@cur, 1, CONCAT('reserva_id=', @rid));
            END TRY
            BEGIN CATCH
                INSERT INTO @results VALUES (@cur, 0, ERROR_MESSAGE());
            END CATCH
        END
        SET @cur = DATEADD(DAY, 1, @cur);
    END

    SELECT * FROM @results ORDER BY data_reserva;
END;
GO

-- Lista de espera: adicionar entrada ---------------------------------
CREATE OR ALTER PROCEDURE sp_adicionar_lista_espera
    @cliente_id      INTEGER,
    @recurso_id      INTEGER,
    @data_pretendida DATE,
    @hora_inicio     TIME = NULL,
    @hora_fim        TIME = NULL,
    @lista_espera_id INTEGER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO lista_espera (cliente_id, recurso_id, data_pretendida,
                              hora_inicio, hora_fim)
    VALUES (@cliente_id, @recurso_id, @data_pretendida,
            @hora_inicio, @hora_fim);
    SET @lista_espera_id = SCOPE_IDENTITY();
END;
GO

-- Lista de espera: promover entrada (cria reserva) -------------------
CREATE OR ALTER PROCEDURE sp_promover_lista_espera
    @lista_espera_id INTEGER,
    @reserva_id      INTEGER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @cliente_id INTEGER, @recurso_id INTEGER, @data DATE,
            @hora_inicio TIME, @hora_fim TIME, @estado NVARCHAR(20),
            @tipo NVARCHAR(10);

    SELECT @cliente_id  = le.cliente_id,
           @recurso_id  = le.recurso_id,
           @data        = le.data_pretendida,
           @hora_inicio = le.hora_inicio,
           @hora_fim    = le.hora_fim,
           @estado      = le.estado,
           @tipo        = rc.tipo
    FROM   lista_espera le
    JOIN   recurso rc ON le.recurso_id = rc.recurso_id
    WHERE  le.lista_espera_id = @lista_espera_id;

    IF @cliente_id IS NULL
        THROW 51040, 'Entrada de lista de espera inexistente.', 1;
    IF @estado NOT IN ('Aguarda','Notificado')
        THROW 51041, 'Entrada de lista de espera já promovida ou cancelada.', 1;

    BEGIN TRANSACTION;

    IF @tipo = 'Sala'
        EXEC sp_criar_reserva_sala
             @cliente_id, @recurso_id, @data,
             @hora_inicio, @hora_fim,
             NULL, N'Promovido da lista de espera',
             @reserva_id = @reserva_id OUTPUT;
    ELSE
        EXEC sp_criar_reserva_posto
             @cliente_id, @recurso_id, @data,
             N'Promovido da lista de espera',
             @reserva_id = @reserva_id OUTPUT;

    UPDATE lista_espera
       SET estado     = 'Promovido',
           reserva_id = @reserva_id
     WHERE lista_espera_id = @lista_espera_id;

    INSERT INTO notificacao (cliente_id, tipo, assunto, mensagem)
    VALUES (@cliente_id, 'ListaEsperaPromovida',
            'Reserva confirmada a partir da lista de espera',
            CONCAT('A entrada #', @lista_espera_id,
                   ' foi promovida — reserva #', @reserva_id, '.'));

    COMMIT;
END;
GO

-- Registar pagamento ---------------------------------------------------
CREATE OR ALTER PROCEDURE sp_registar_pagamento
    @cliente_id       INTEGER,
    @valor            DECIMAL(10,2),
    @metodo_pagamento NVARCHAR(40),
    @adesao_id        INTEGER = NULL,
    @reserva_id       INTEGER = NULL,
    @pagamento_id     INTEGER OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO pagamento (cliente_id, valor, metodo_pagamento,
                           estado, adesao_id, reserva_id)
    VALUES (@cliente_id, @valor, @metodo_pagamento,
            'Pago', @adesao_id, @reserva_id);

    SET @pagamento_id = SCOPE_IDENTITY();
END;
GO

-- Relatório de receita por período ------------------------------------
CREATE OR ALTER PROCEDURE sp_relatorio_receita_periodo
    @data_de  DATE,
    @data_ate DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        CASE
            WHEN adesao_id  IS NOT NULL THEN 'Adesao'
            WHEN reserva_id IS NOT NULL THEN 'Reserva'
        END             AS tipo_servico,
        COUNT(*)        AS num_pagamentos,
        SUM(valor)      AS receita
    FROM pagamento
    WHERE estado = 'Pago'
      AND data_pagamento BETWEEN @data_de AND @data_ate
    GROUP BY
        CASE
            WHEN adesao_id  IS NOT NULL THEN 'Adesao'
            WHEN reserva_id IS NOT NULL THEN 'Reserva'
        END;
END;
GO

-- Notificações: marcar como lida -------------------------------------
CREATE OR ALTER PROCEDURE sp_marcar_notificacao_lida
    @notificacao_id INTEGER
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE notificacao
       SET lida = 1, data_leitura = SYSDATETIME()
     WHERE notificacao_id = @notificacao_id;
END;
GO
