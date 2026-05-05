## Modelo de Dados — Alterações vs APF-E

### Correções do professor incorporadas

1. **Reserva como entidade associativa única (M:N).** A tabela `reserva` substitui as anteriores `reserva_sala`/`reserva_posto`, ligando `cliente` ↔ `recurso` com atributos próprios (data, horas, valor, estado).
2. **Pagamento liga-se ao valor cobrado.** XOR explícito `adesao_id` / `reserva_id`; `trg_pagamento_valor_correto` valida `pagamento.valor` contra `reserva.valor` (sala ou day pass) ou `adesao.preco_acordado`.

### Decisões adicionais (sessão de redesign)

3. **`recurso` supertype.** `recurso(recurso_id, tipo)` é PK partilhada por `sala` e `posto`. Cada subtype mantém o seu `espaco_id` e `estado` para simplicidade de UNIQUE e domínio.
4. **`posto_trabalho` → `posto`.** Renomeado para alinhar com a linguagem do domínio.
5. **`posto.preco_dia` (era `preco_hora`).** Postos cobram-se ao dia (day pass), não à hora.
6. **`plano.tipo_plano` ∈ {Flex, Fixo, Privado}.** Distingue adesão sem posto fixo, com posto fixo, e privada.
7. **`adesao.recurso_id` opcional.** NULL para Flex; NOT NULL e do tipo correspondente para Fixo/Privado (validado pelo trigger T9).
8. **`adesao.preco_acordado` snapshot.** Congela `plano.preco_mensal` no momento da adesão; T10 (INSTEAD OF INSERT) preenche se NULL.
9. **`reserva.hora_inicio`/`hora_fim` `NULL`-able.** NULL para reservas de posto (dia inteiro); NOT NULL para reservas de sala. Validado por T12.

### Novos triggers (T9–T12)

- **T9 `trg_adesao_recurso_coerente`** (50009): Flex sem recurso, Fixo/Privado com posto do tipo correto, sem sobreposições.
- **T10 `trg_adesao_preco_snapshot`** (INSTEAD OF INSERT, sem erro): preenche `preco_acordado` quando NULL.
- **T11 `trg_reserva_posto_sem_adesao`** (50011): impede day pass que colida com adesão Ativa.
- **T12 `trg_reserva_horas_coerentes`** (50012): obriga horas em sala, proíbe-as em posto.

### Validação

- 17 smoke tests SQL automáticos (`SQL_Scripts/Tests/smoke_triggers.sql`) — todos a passar.
- 4 cenários manuais na aplicação verificados.
