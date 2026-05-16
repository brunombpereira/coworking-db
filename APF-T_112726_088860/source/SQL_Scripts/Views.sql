-- =====================================================================
-- Views da CoworkingDB
-- Consultas de uso frequente: ocupação, receita, listagens.
-- =====================================================================
USE CoworkingDB;
GO

-- Reservas confirmadas ou pendentes (futuras ou de hoje) --------------
CREATE VIEW vw_reservas_ativas
AS
SELECT
    r.reserva_id,
    r.cliente_id,
    c.nome              AS cliente_nome,
    r.recurso_id,
    rc.tipo             AS tipo_recurso,
    COALESCE(s.nome, p.codigo) AS recurso_identificador,
    r.data_reserva,
    r.hora_inicio,
    r.hora_fim,
    r.valor,
    r.estado
FROM reserva r
JOIN cliente c  ON r.cliente_id = c.cliente_id
JOIN recurso rc ON r.recurso_id = rc.recurso_id
LEFT JOIN sala  s ON rc.recurso_id = s.recurso_id
LEFT JOIN posto p ON rc.recurso_id = p.recurso_id
WHERE r.estado IN ('Pendente','Confirmada')
  AND r.data_reserva >= CAST(GETDATE() AS DATE);
GO

-- Ocupação por recurso (nº de reservas concluídas/confirmadas) --------
CREATE VIEW vw_ocupacao_recurso
AS
SELECT
    rc.recurso_id,
    rc.tipo,
    COALESCE(s.nome, p.codigo) AS identificador,
    COALESCE(s.espaco_id, p.espaco_id) AS espaco_id,
    COUNT(r.reserva_id)        AS total_reservas,
    SUM(CASE WHEN r.estado = 'Confirmada' THEN 1 ELSE 0 END) AS confirmadas,
    SUM(CASE WHEN r.estado = 'Concluida'  THEN 1 ELSE 0 END) AS concluidas,
    SUM(CASE WHEN r.estado = 'Cancelada'  THEN 1 ELSE 0 END) AS canceladas
FROM recurso rc
LEFT JOIN sala  s ON rc.recurso_id = s.recurso_id
LEFT JOIN posto p ON rc.recurso_id = p.recurso_id
LEFT JOIN reserva r ON r.recurso_id = rc.recurso_id
GROUP BY rc.recurso_id, rc.tipo, COALESCE(s.nome, p.codigo),
         COALESCE(s.espaco_id, p.espaco_id);
GO

-- Receita por plano (somatório de pagamentos Pagos via adesão) --------
CREATE VIEW vw_receita_por_plano
AS
SELECT
    pl.plano_id,
    pl.nome_plano,
    pl.tipo_plano,
    COUNT(DISTINCT a.adesao_id) AS num_adesoes,
    COALESCE(SUM(pg.valor), 0)  AS receita_total
FROM plano pl
LEFT JOIN adesao a    ON a.plano_id = pl.plano_id
LEFT JOIN pagamento pg ON pg.adesao_id = a.adesao_id AND pg.estado = 'Pago'
GROUP BY pl.plano_id, pl.nome_plano, pl.tipo_plano;
GO

-- Receita mensal (todos os pagamentos Pagos) --------------------------
CREATE VIEW vw_receita_mensal
AS
SELECT
    YEAR(data_pagamento)  AS ano,
    MONTH(data_pagamento) AS mes,
    COUNT(*)              AS num_pagamentos,
    SUM(valor)            AS receita_total
FROM pagamento
WHERE estado = 'Pago'
GROUP BY YEAR(data_pagamento), MONTH(data_pagamento);
GO

-- Clientes com adesão ativa -------------------------------------------
CREATE VIEW vw_clientes_com_adesao_ativa
AS
SELECT
    c.cliente_id,
    c.nome,
    c.email,
    a.adesao_id,
    pl.nome_plano,
    pl.tipo_plano,
    a.data_inicio,
    a.data_fim,
    a.preco_acordado
FROM cliente c
JOIN adesao a  ON a.cliente_id = c.cliente_id AND a.estado = 'Ativa'
JOIN plano  pl ON a.plano_id   = pl.plano_id;
GO

-- Pagamentos por liquidar ---------------------------------------------
CREATE VIEW vw_pagamentos_pendentes
AS
SELECT
    pg.pagamento_id,
    pg.cliente_id,
    c.nome AS cliente_nome,
    pg.data_pagamento,
    pg.valor,
    pg.metodo_pagamento,
    CASE
        WHEN pg.adesao_id  IS NOT NULL THEN 'Adesao'
        WHEN pg.reserva_id IS NOT NULL THEN 'Reserva'
    END AS tipo_servico,
    COALESCE(pg.adesao_id, pg.reserva_id) AS servico_id
FROM pagamento pg
JOIN cliente c ON pg.cliente_id = c.cliente_id
WHERE pg.estado = 'Pendente';
GO

-- Top clientes por receita (window functions) ------------------------
CREATE VIEW vw_top_clientes_receita
AS
SELECT
    c.cliente_id,
    c.nome,
    COALESCE(SUM(pg.valor), 0)                            AS receita,
    COUNT(pg.pagamento_id)                                AS num_pagamentos,
    RANK() OVER (ORDER BY COALESCE(SUM(pg.valor),0) DESC) AS posicao
FROM cliente c
LEFT JOIN pagamento pg ON pg.cliente_id = c.cliente_id AND pg.estado = 'Pago'
GROUP BY c.cliente_id, c.nome;
GO

-- Receita por espaço por mês -----------------------------------------
CREATE VIEW vw_receita_por_espaco_mes
AS
SELECT
    e.espaco_id,
    e.nome AS espaco_nome,
    YEAR(pg.data_pagamento)  AS ano,
    MONTH(pg.data_pagamento) AS mes,
    SUM(pg.valor)            AS receita
FROM pagamento pg
JOIN reserva   r  ON pg.reserva_id = r.reserva_id
JOIN recurso   rc ON r.recurso_id  = rc.recurso_id
LEFT JOIN sala  s ON rc.recurso_id = s.recurso_id
LEFT JOIN posto p ON rc.recurso_id = p.recurso_id
JOIN espaco    e  ON e.espaco_id   = COALESCE(s.espaco_id, p.espaco_id)
WHERE pg.estado = 'Pago'
GROUP BY e.espaco_id, e.nome, YEAR(pg.data_pagamento), MONTH(pg.data_pagamento);
GO

-- Receita por método de pagamento ------------------------------------
CREATE VIEW vw_receita_por_metodo
AS
SELECT
    metodo_pagamento,
    COUNT(*)   AS num_pagamentos,
    SUM(valor) AS receita,
    CAST(SUM(valor) * 100.0 /
         SUM(SUM(valor)) OVER () AS DECIMAL(5,2)) AS percentagem
FROM pagamento
WHERE estado = 'Pago'
GROUP BY metodo_pagamento;
GO

-- Adesões a expirar nos próximos 30 dias -----------------------------
CREATE VIEW vw_adesoes_a_expirar
AS
SELECT
    a.adesao_id,
    a.cliente_id,
    c.nome AS cliente_nome,
    pl.nome_plano,
    a.data_fim,
    DATEDIFF(DAY, CAST(GETDATE() AS DATE), a.data_fim) AS dias_restantes
FROM adesao a
JOIN cliente c  ON a.cliente_id = c.cliente_id
JOIN plano   pl ON a.plano_id   = pl.plano_id
WHERE a.estado = 'Ativa'
  AND a.data_fim BETWEEN CAST(GETDATE() AS DATE)
                     AND DATEADD(DAY, 30, CAST(GETDATE() AS DATE));
GO

-- Histórico de uma reserva (temporal — requer Temporal_tables.sql) ---
-- Nota: esta view só funciona depois de aplicado SYSTEM_VERSIONING.
CREATE VIEW vw_reservas_historico
AS
SELECT
    reserva_id,
    cliente_id,
    recurso_id,
    data_reserva,
    estado,
    valor
FROM reserva
FOR SYSTEM_TIME ALL;
GO

-- Notificações por ler -----------------------------------------------
CREATE VIEW vw_notificacoes_por_ler
AS
SELECT
    n.notificacao_id,
    n.cliente_id,
    c.nome AS cliente_nome,
    n.tipo,
    n.assunto,
    n.mensagem,
    n.data_criacao
FROM notificacao n
JOIN cliente c ON n.cliente_id = c.cliente_id
WHERE n.lida = 0;
GO
