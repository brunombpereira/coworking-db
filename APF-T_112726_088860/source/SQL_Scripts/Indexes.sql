-- =====================================================================
-- Índices da CoworkingDB
-- Otimizam: deteção de sobreposições, consultas por cliente/recurso,
-- joins com pagamentos via adesão ou reserva.
-- =====================================================================
USE CoworkingDB;
GO

SET QUOTED_IDENTIFIER ON;
GO

-- Reserva: detecção de sobreposições e listagens por recurso/dia
CREATE INDEX idx_reserva_recurso
    ON reserva (recurso_id, data_reserva, hora_inicio, hora_fim);

-- Pagamentos filtrados por tipo de serviço
CREATE INDEX idx_pagamento_adesao
    ON pagamento (adesao_id)
    WHERE adesao_id IS NOT NULL;

CREATE INDEX idx_pagamento_reserva
    ON pagamento (reserva_id)
    WHERE reserva_id IS NOT NULL;

-- Listagens por cliente
CREATE INDEX idx_adesao_cliente
    ON adesao (cliente_id, estado);

CREATE INDEX idx_reserva_cliente
    ON reserva (cliente_id, estado);

CREATE INDEX idx_pagamento_cliente
    ON pagamento (cliente_id, estado);

-- Adesão por recurso (postos Fixo/Privado)
CREATE INDEX idx_adesao_recurso
    ON adesao (recurso_id)
    WHERE recurso_id IS NOT NULL;
GO
