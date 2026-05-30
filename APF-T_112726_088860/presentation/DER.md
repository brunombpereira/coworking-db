# DER — Modelo Entidade-Relacionamento (APF-T)

Versão actualizada após correcções do professor e features adicionadas na APF-T.
Para exportar como PNG: copiar o bloco `mermaid` para [mermaid.live](https://mermaid.live)
e usar **Actions → PNG**.

## Diferenças vs. DER original (APF-E)

| Mudança | Razão |
|---|---|
| `Reserva Sala` + `Reserva Posto` → tabela única `reserva` com FK para `recurso` | **Correção #2 do prof.**: relação M:N com entidade associativa em vez de duas tabelas separadas. |
| Adicionado supertype `recurso` (com subtypes `sala` / `posto`) | Centraliza a FK das reservas; trigger T1 (sobreposição) fica mais simples. |
| `pagamento.preco_servico_snapshot` | **Correção #1 do prof.**: ligação explícita ao preço cobrado, em vez de só viver no trigger T6. |
| Nova entidade `adesao` (em vez da relação "Subscreve") | Adesão tem atributos próprios (estado, datas, preço acordado) — promovida a entidade. |
| Nova entidade `politica_cancelamento` | Tiers de reembolso aplicados em `sp_cancelar_reserva_com_reembolso`. |
| Nova entidade `notificacao` | Eventos enviados ao cliente (criação de reserva, pagamento, etc.). |
| Nova entidade `lista_espera` | Cliente aguarda recurso indisponível; pode ser promovida a reserva. |
| Tabelas history (`adesao_history`, `reserva_history`, `pagamento_history`) | Auto-criadas por `SYSTEM_VERSIONING`. Não mostradas no DER conceptual — são implementação. |

## Diagrama

```mermaid
erDiagram
    %% ── Identidade ──────────────────────────────────────────
    cliente ||--o{ adesao                : "tem"
    cliente ||--o{ reserva               : "faz"
    cliente ||--o{ pagamento             : "efetua"
    cliente ||--o{ notificacao           : "recebe"
    cliente ||--o{ lista_espera          : "inscreve-se"

    %% ── Catálogo + recursos ──────────────────────────────────
    plano   ||--o{ adesao                : "instancia"
    espaco  ||--o{ sala                  : "contém"
    espaco  ||--o{ posto                 : "contém"

    %% ── Supertype/subtype (recurso) ──────────────────────────
    recurso ||--|| sala                  : "is-a"
    recurso ||--|| posto                 : "is-a"

    %% ── Adesão / Reserva ────────────────────────────────────
    recurso |o--o{ adesao                : "atribuído a (NULL se Flex)"
    recurso ||--o{ reserva               : "alvo de"
    recurso ||--o{ lista_espera          : "pretendido"

    %% ── Pagamento (XOR adesao/reserva) ──────────────────────
    adesao  |o--o{ pagamento             : "pago via (XOR reserva)"
    reserva |o--o{ pagamento             : "pago via (XOR adesão)"
    reserva |o--o| lista_espera          : "promoção origina"

    cliente {
        int      cliente_id   PK
        nvarchar nome
        char(9)  nif          UK
        nvarchar email        UK
        nvarchar telefone
        date     data_registo
    }

    plano {
        int      plano_id      PK
        nvarchar nome_plano    UK
        nvarchar tipo_plano    "Flex | Fixo | Privado"
        decimal  preco_mensal
        int      duracao_meses
        nvarchar descricao
    }

    espaco {
        int      espaco_id     PK
        nvarchar nome          UK
        nvarchar morada
        nvarchar telefone
        nvarchar email
        time     hora_abertura
        time     hora_fecho
    }

    recurso {
        int      recurso_id    PK
        nvarchar tipo          "Sala | Posto"
    }

    sala {
        int      recurso_id    PK_FK "supertype"
        int      espaco_id     FK
        nvarchar nome
        int      capacidade
        decimal  preco_hora
        nvarchar estado        "Disponivel | Indisponivel | Manutencao | Inativo"
    }

    posto {
        int      recurso_id    PK_FK "supertype"
        int      espaco_id     FK
        nvarchar codigo
        nvarchar tipo_posto    "Flex | Fixo | Privado"
        decimal  preco_dia
        nvarchar estado
    }

    adesao {
        int      adesao_id      PK
        int      cliente_id     FK
        int      plano_id       FK
        int      recurso_id     FK "NULL para Flex"
        date     data_inicio
        date     data_fim       "auto via T5"
        decimal  preco_acordado "snapshot do plano"
        nvarchar estado         "Pendente | Ativa | Suspensa | Cancelada | Terminada"
    }

    reserva {
        int      reserva_id        PK
        int      cliente_id        FK
        int      recurso_id        FK
        date     data_reserva
        time     hora_inicio       "NULL se posto"
        time     hora_fim          "NULL se posto"
        decimal  valor
        nvarchar estado            "Pendente | Confirmada | Cancelada | Concluida"
        int      num_participantes "NULL se posto"
        nvarchar notas
    }

    pagamento {
        int      pagamento_id            PK
        int      cliente_id              FK
        date     data_pagamento
        decimal  valor
        decimal  preco_servico_snapshot  "= valor (CHECK); = preço do serviço (T6)"
        nvarchar metodo_pagamento        "Dinheiro | Cartao | Transferencia | MBWay | PayPal"
        nvarchar estado                  "Pendente | Pago | Cancelado | Reembolsado"
        int      adesao_id               FK "XOR reserva_id"
        int      reserva_id              FK "XOR adesao_id"
    }

    politica_cancelamento {
        int      politica_id     PK
        nvarchar nome            UK
        int      horas_minimas
        decimal  perc_reembolso  "0-100"
        bit      ativa
    }

    notificacao {
        int       notificacao_id PK
        int       cliente_id     FK
        nvarchar  tipo           "ReservaCriada | ReservaProxima | ..."
        nvarchar  assunto
        nvarchar  mensagem
        datetime2 data_criacao
        bit       lida
        datetime2 data_leitura
    }

    lista_espera {
        int       lista_espera_id PK
        int       cliente_id      FK
        int       recurso_id      FK
        date      data_pretendida
        time      hora_inicio
        time      hora_fim
        datetime2 data_inscricao
        nvarchar  estado          "Aguarda | Notificado | Promovido | Cancelado"
        int       reserva_id      FK "preenchido quando promovido"
    }
```

## Notas de modelação

**Supertype/subtype `recurso`.** Sala e posto partilham operações (reservar, ver disponibilidade) mas têm atributos próprios. O supertype `recurso` permite que `reserva.recurso_id` e `lista_espera.recurso_id` sejam uma única FK em vez de discriminar com colunas opcionais.

**XOR em pagamento.** `ck_pagamento_servico` garante que `adesao_id` e `reserva_id` são mutuamente exclusivos (exatamente um preenchido). Mermaid não suporta notação XOR nativa — daí estar anotado em texto nos atributos.

**Snapshot de preço.** Tanto `adesao.preco_acordado` como `pagamento.preco_servico_snapshot` são *redundância controlada* — fixam o preço no momento da operação, para preservar histórico se o preço base mudar mais tarde. Triggers T6 (pagamento) e a lógica do SP `sp_criar_adesao` garantem consistência na inserção.

**System-versioning (não mostrado).** As tabelas `adesao`, `reserva` e `pagamento` têm `SYSTEM_VERSIONING = ON`, com tabelas `_history` auto-mantidas pelo SQL Server. Não fazem parte do modelo conceptual — só aparecem em queries `FOR SYSTEM_TIME` (ver `vw_reservas_historico`).
