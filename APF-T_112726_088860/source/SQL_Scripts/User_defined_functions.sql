-- =====================================================================
-- User-defined functions da CoworkingDB
-- =====================================================================
USE CoworkingDB;
GO

-- fn_recurso_disponivel: verifica se um recurso está livre num período
-- Para postos passar @hora_inicio/@hora_fim NULL.
CREATE FUNCTION fn_recurso_disponivel
(
    @recurso_id   INTEGER,
    @data_reserva DATE,
    @hora_inicio  TIME,
    @hora_fim     TIME
)
RETURNS BIT
AS
BEGIN
    DECLARE @ocupado BIT = 0;

    IF EXISTS (
        SELECT 1
        FROM reserva r
        WHERE r.recurso_id   = @recurso_id
          AND r.data_reserva = @data_reserva
          AND r.estado <> 'Cancelada'
          AND (
                (@hora_inicio IS NOT NULL AND r.hora_inicio IS NOT NULL
                 AND @hora_inicio < r.hora_fim AND @hora_fim > r.hora_inicio)
             OR (@hora_inicio IS NULL OR r.hora_inicio IS NULL)
          )
    )
        SET @ocupado = 1;

    RETURN CASE WHEN @ocupado = 1 THEN 0 ELSE 1 END;
END;
GO

-- fn_receita_cliente: soma de pagamentos Pagos de um cliente
CREATE FUNCTION fn_receita_cliente (@cliente_id INTEGER)
RETURNS DECIMAL(12,2)
AS
BEGIN
    DECLARE @total DECIMAL(12,2);

    SELECT @total = COALESCE(SUM(valor), 0)
    FROM   pagamento
    WHERE  cliente_id = @cliente_id
      AND  estado = 'Pago';

    RETURN @total;
END;
GO

-- fn_reservas_cliente_periodo: tabela com as reservas de um cliente
-- num intervalo de datas.
CREATE FUNCTION fn_reservas_cliente_periodo
(
    @cliente_id INTEGER,
    @data_de    DATE,
    @data_ate   DATE
)
RETURNS TABLE
AS
RETURN (
    SELECT
        r.reserva_id,
        r.recurso_id,
        rc.tipo AS tipo_recurso,
        r.data_reserva,
        r.hora_inicio,
        r.hora_fim,
        r.valor,
        r.estado
    FROM reserva r
    JOIN recurso rc ON r.recurso_id = rc.recurso_id
    WHERE r.cliente_id = @cliente_id
      AND r.data_reserva BETWEEN @data_de AND @data_ate
);
GO

-- fn_taxa_ocupacao_espaco: % de recursos do espaço com reserva
-- Confirmada/Concluida numa data específica.
CREATE FUNCTION fn_taxa_ocupacao_espaco
(
    @espaco_id INTEGER,
    @data      DATE
)
RETURNS DECIMAL(5,2)
AS
BEGIN
    DECLARE @total INTEGER, @ocupados INTEGER;

    SELECT @total = COUNT(*)
    FROM (
        SELECT recurso_id FROM sala  WHERE espaco_id = @espaco_id
        UNION
        SELECT recurso_id FROM posto WHERE espaco_id = @espaco_id
    ) AS recursos_do_espaco;

    IF @total = 0 RETURN 0;

    SELECT @ocupados = COUNT(DISTINCT r.recurso_id)
    FROM   reserva r
    WHERE  r.data_reserva = @data
      AND  r.estado IN ('Confirmada','Concluida')
      AND  r.recurso_id IN (
            SELECT recurso_id FROM sala  WHERE espaco_id = @espaco_id
            UNION
            SELECT recurso_id FROM posto WHERE espaco_id = @espaco_id
        );

    RETURN CAST(@ocupados AS DECIMAL(5,2)) / @total * 100;
END;
GO

-- fn_calc_reembolso: aplica politica_cancelamento e devolve o valor
-- a reembolsar para uma reserva, dadas as horas de antecedência.
CREATE FUNCTION fn_calc_reembolso
(
    @valor_pago      DECIMAL(10,2),
    @antecedencia_h  INTEGER
)
RETURNS DECIMAL(10,2)
AS
BEGIN
    DECLARE @perc DECIMAL(5,2);

    SELECT TOP 1 @perc = perc_reembolso
    FROM   politica_cancelamento
    WHERE  ativa = 1 AND horas_minimas <= @antecedencia_h
    ORDER BY horas_minimas DESC;

    RETURN ROUND(@valor_pago * COALESCE(@perc, 0) / 100, 2);
END;
GO

-- fn_receita_periodo: soma de pagamentos Pagos num intervalo, opcional
-- por método de pagamento (NULL = todos).
CREATE FUNCTION fn_receita_periodo
(
    @data_de  DATE,
    @data_ate DATE,
    @metodo   NVARCHAR(40) = NULL
)
RETURNS DECIMAL(12,2)
AS
BEGIN
    DECLARE @total DECIMAL(12,2);

    SELECT @total = COALESCE(SUM(valor), 0)
    FROM   pagamento
    WHERE  estado = 'Pago'
      AND  data_pagamento BETWEEN @data_de AND @data_ate
      AND  (@metodo IS NULL OR metodo_pagamento = @metodo);

    RETURN @total;
END;
GO

-- fn_clientes_inativos: tabela de clientes sem reservas nem adesão
-- ativa nos últimos @dias dias.
CREATE FUNCTION fn_clientes_inativos (@dias INT)
RETURNS TABLE
AS
RETURN (
    SELECT c.cliente_id, c.nome, c.email,
           (SELECT MAX(r.data_reserva)
            FROM   reserva r WHERE r.cliente_id = c.cliente_id) AS ultima_reserva
    FROM   cliente c
    WHERE  NOT EXISTS (
            SELECT 1 FROM reserva r
            WHERE  r.cliente_id = c.cliente_id
              AND  r.data_reserva >= DATEADD(DAY, -@dias, CAST(GETDATE() AS DATE))
        )
      AND  NOT EXISTS (
            SELECT 1 FROM adesao a
            WHERE  a.cliente_id = c.cliente_id
              AND  a.estado = 'Ativa'
        )
);
GO
