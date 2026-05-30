# Esquema Relacional — APF-T

Versão actualizada após correcções do professor e features adicionadas na APF-T.
O esquema original (`APF-E_112726_088860/EsquemaRelacional.png`) é preservado
como registo histórico do que foi entregue na APF-E.

**Convenções:**
- <u>Atributo sublinhado</u> = chave primária (PK)
- *Atributo em itálico* = chave estrangeira (FK)
- `↑Tabela` = aponta para PK da tabela referenciada
- `{XOR a,b}` = exactamente um de `a` ou `b` é NOT NULL
- `?` = NULL permitido

## Esquema

### 1. Entidades base

**cliente**(<u>cliente_id</u>, nome, nif, email, telefone, data_registo)
- UNIQUE: `nif`, `email`

**plano**(<u>plano_id</u>, nome_plano, tipo_plano, preco_mensal, duracao_meses, descricao?)
- UNIQUE: `nome_plano`
- CHECK: `tipo_plano ∈ {Flex, Fixo, Privado}`

**espaco**(<u>espaco_id</u>, nome, morada, telefone?, email?, hora_abertura, hora_fecho)
- UNIQUE: `nome`
- CHECK: `hora_fecho > hora_abertura`

### 2. Supertype + subtypes (recurso)

**recurso**(<u>recurso_id</u>, tipo)
- CHECK: `tipo ∈ {Sala, Posto}`

**sala**(<u>*recurso_id*↑recurso</u>, *espaco_id*↑espaco, nome, capacidade, preco_hora, estado)
- UNIQUE: `(espaco_id, nome)`
- CHECK: `capacidade > 0`, `preco_hora ≥ 0`, `estado ∈ {Disponivel, Indisponivel, Manutencao, Inativo}`
- ON DELETE CASCADE em `recurso_id`

**posto**(<u>*recurso_id*↑recurso</u>, *espaco_id*↑espaco, codigo, tipo_posto, preco_dia, estado)
- UNIQUE: `(espaco_id, codigo)`
- CHECK: `tipo_posto ∈ {Flex, Fixo, Privado}`, `preco_dia ≥ 0`
- ON DELETE CASCADE em `recurso_id`

> **Nota — supertype/subtype:** `sala` e `posto` partilham `recurso_id` (PK=FK).
> Implementa a regra "todo o recurso é sala XOR posto" via `recurso.tipo` +
> triggers que validam a coerência com as subtypes.

### 3. Adesão e Reserva (entidades associativas)

**adesao**(<u>adesao_id</u>, *cliente_id*↑cliente, *plano_id*↑plano, *recurso_id*↑recurso?, data_inicio, data_fim?, preco_acordado, estado)
- CHECK: `data_fim ≥ data_inicio` (se NOT NULL)
- CHECK: `estado ∈ {Pendente, Ativa, Suspensa, Cancelada, Terminada}`
- `recurso_id` NULL para planos Flex, NOT NULL para Fixo/Privado (T9)
- `preco_acordado` = snapshot de `plano.preco_mensal` no momento da criação

**reserva**(<u>reserva_id</u>, *cliente_id*↑cliente, *recurso_id*↑recurso, data_reserva, hora_inicio?, hora_fim?, valor, estado, num_participantes?, notas?)
- CHECK: `(hora_inicio, hora_fim) ambos NULL` ou `hora_fim > hora_inicio`
- CHECK: `estado ∈ {Pendente, Confirmada, Cancelada, Concluida}`
- Para reservas de **sala** → `hora_inicio`/`hora_fim` NOT NULL (T12)
- Para reservas de **posto** → todas as horas NULL (T12)

> **Correção #2 do prof.:** entidade `reserva` única (em vez de `reserva_sala`
> + `reserva_posto` separadas), com FK para `recurso`. M:N entre cliente e
> recurso, materializada como entidade associativa com atributos próprios.

### 4. Pagamento

**pagamento**(<u>pagamento_id</u>, *cliente_id*↑cliente, data_pagamento, valor, **preco_servico_snapshot**, metodo_pagamento, estado, *adesao_id*↑adesao?, *reserva_id*↑reserva?)
- CHECK `ck_pagamento_servico`: `{XOR adesao_id, reserva_id}`
- CHECK `ck_pagamento_valor_snapshot`: `valor = preco_servico_snapshot`
- CHECK: `valor > 0`, `preco_servico_snapshot > 0`
- CHECK: `metodo_pagamento ∈ {Dinheiro, Cartao, Transferencia, MBWay, PayPal}`
- CHECK: `estado ∈ {Pendente, Pago, Cancelado, Reembolsado}`
- Triggers T6/T8: snapshot tem de bater certo com `reserva.valor`/`adesao.preco_acordado`; `cliente_id` tem de ser titular do serviço

> **Correção #1 do prof.:** `preco_servico_snapshot` torna a ligação ao valor
> cobrado **explícita no schema** em vez de viver só nos triggers — fotografa o
> preço do serviço no momento do pagamento.

### 5. Política de cancelamento

**politica_cancelamento**(<u>politica_id</u>, nome, horas_minimas, perc_reembolso, ativa)
- UNIQUE: `nome`
- CHECK: `horas_minimas ≥ 0`, `perc_reembolso ∈ [0, 100]`
- Aplicada por `sp_cancelar_reserva_com_reembolso` (não por FK directa de `reserva`)

### 6. Notificações

**notificacao**(<u>notificacao_id</u>, *cliente_id*↑cliente, tipo, assunto, mensagem, data_criacao, lida, data_leitura?)
- CHECK: `tipo ∈ {ReservaCriada, ReservaProxima, ReservaCancelada, PagamentoConfirmado, AdesaoExpirar, ListaEsperaPromovida}`
- ON DELETE CASCADE em `cliente_id`

### 7. Lista de espera

**lista_espera**(<u>lista_espera_id</u>, *cliente_id*↑cliente, *recurso_id*↑recurso, data_pretendida, hora_inicio?, hora_fim?, data_inscricao, estado, *reserva_id*↑reserva?)
- CHECK: `estado ∈ {Aguarda, Notificado, Promovido, Cancelado}`
- CHECK: `(hora_inicio, hora_fim) ambos NULL` ou `hora_fim > hora_inicio`
- ON DELETE CASCADE em `cliente_id`
- `reserva_id` é preenchido quando a entrada é promovida pelo SP

## Diferenças vs. esquema original (APF-E)

| Mudança | Razão |
|---|---|
| `Reserva Sala` + `Reserva Posto` → **`reserva`** com FK para `recurso` | Correção #2 do prof. |
| Adicionado `recurso` (supertype) + `sala`/`posto` como subtypes (PK=FK) | Centraliza a FK de `reserva` e `lista_espera` |
| `Adesão` ganha `recurso_id` (NULL para Flex) e `preco_acordado` | Modela a regra "Fixo/Privado têm posto atribuído" + snapshot do preço |
| `Pagamento` ganha `preco_servico_snapshot` + FKs `adesao_id`/`reserva_id` (XOR) | Correção #1 do prof. |
| **Novas tabelas:** `politica_cancelamento`, `notificacao`, `lista_espera` | Features APF-T (reembolsos, eventos, espera) |

## Tabelas de histórico (implementação)

Não fazem parte do modelo conceptual — são auto-criadas pelo `SYSTEM_VERSIONING = ON`:

- **adesao_history** — versões anteriores de `adesao` (todos os UPDATE/DELETE)
- **reserva_history** — versões anteriores de `reserva`
- **pagamento_history** — versões anteriores de `pagamento`

Consultáveis via `FOR SYSTEM_TIME AS OF | BETWEEN | ALL` (ver `vw_reservas_historico`).

## Sumário de relações

| Origem | Cardinalidade | Destino |
|---|---|---|
| `cliente` | 1 — 0..N | `adesao`, `reserva`, `pagamento`, `notificacao`, `lista_espera` |
| `plano` | 1 — 0..N | `adesao` |
| `espaco` | 1 — 0..N | `sala`, `posto` |
| `recurso` | 1 — 1 | `sala` (subtype) |
| `recurso` | 1 — 1 | `posto` (subtype) |
| `recurso` | 1 — 0..N | `reserva`, `lista_espera` |
| `recurso` | 0..1 — 0..N | `adesao` |
| `adesao` | 1 — 0..N | `pagamento` (XOR `reserva`) |
| `reserva` | 1 — 0..N | `pagamento` (XOR `adesao`) |
| `reserva` | 0..1 — 0..1 | `lista_espera` (promoção) |
