# Teleprompter — tópicos para ler no ecrã

**Coworking · APF-T** · só o essencial, frase curta por tópico.
Alternância em **pares de slides**: Rafa → Bruno → Rafa → Bruno…

---

## ▌ RAFA — Slides 1–2

**1 · Título**
- Boa tarde — Rafael Claro e Bruno Pereira
- Projeto BD: **Sistema de Gestão de Coworking**
- Do registo do cliente até ao pagamento

**2 · Contexto**
- Domínio: clientes · planos (Flex/Fixo/Privado) · adesões · reservas
- Salas à hora · postos ao dia
- Transversal: notificações · lista de espera · recorrentes · auditoria
- Stack: **SQL Server + C# WinForms .NET 8** · ADO.NET parametrizado, zero SQL injection

---

## ▌ BRUNO — Slides 3–4

**3 · DER**
- Diagrama Entidade-Relacionamento
- Centro: **recurso = supertype** → é-uma sala **ou** é-um posto
- Trata salas e postos de forma uniforme

**4 · Esquema Relacional**
- **12 tabelas em BCNF**
- `adesao` → snapshot do preço (`preco_acordado`)
- `reserva` → entidade associativa cliente↔recurso, atributos próprios
- `pagamento` → adesão **XOR** reserva + `preco_servico_snapshot`
- Os 2 snapshots = correções do professor

---

## ▌ RAFA — Slides 5–6

**5 · SQL Scripts**
- Implementação em **8 scripts** por tópico
- DDL · funções · triggers · views · SPs · temporais · índices · seed

**6 · DDL supertype**
- Supertype com **PK = FK** ao recurso
- **CASCADE delete**
- 1 só FK em `reserva.recurso_id` cobre salas e postos

---

## ▌ BRUNO — Slides 7–8

**7 · UDF fn_recurso_disponivel**
- Função que verifica **sobreposição** de horários
- Usada pela **app** (antes de oferecer) e pelo **trigger** (antes do INSERT)
- Mesma regra, num só sítio

**8 · SP sp_criar_reserva_sala**
- Reserva criada por **stored procedure**
- Problema: **race condition** (2 INSERTs ao mesmo tempo)
- Solução: **`sp_getapplock` Exclusive** na transação → serializa

---

## ▌ RAFA — Slides 9–10

**9 · View vw_top_clientes_receita**
- Views alimentam relatórios
- Ranking por receita com **`RANK()`** (trata empates)
- **`LEFT JOIN`** → clientes sem pagamentos aparecem com 0

**10 · Trigger T6 — snapshot do preço**
- **Correção #1 do professor** ("qual o preço?")
- **3 níveis**: coluna snapshot + CHECK + trigger (rollback)
- Valor cobrado fica **imutável** mesmo que o preço mude depois

---

## ▌ BRUNO — Slides 11–12

**11 · Índices**
- Índice composto: sobreposição passa de **table scan → index seek**
- **Filtered indexes** nos pagamentos → ~50% menos storage (ignora NULL)

**12 · Tabelas Temporais**
- Auditoria **automática** com `SYSTEM_VERSIONING`
- Cada alteração guarda versão anterior no histórico
- Sem trigger nem schema manual · reconstitui estado passado

---

## ▌ RAFA — Slides 13–14

**13 · Dashboard light**
- KPIs operacionais + gráficos (receita mensal, métodos)
- Próximas reservas

**14 · Dashboard dark**
- Toggle **light ↔ dark em tempo real**
- Preferência persiste no disco

---

## ▌ BRUNO — Slides 15–16

**15 · Reservas · Tab Mapa**
- Vista **timeline horária** (08h–20h)
- Salas por espaço · cores = estado da reserva

**16 · Pagamentos & Recibo PDF**
- Detalhe read-only
- Botão **"↓ Recibo PDF"** com o preço congelado (snapshot)

---

## ▌ RAFA — Slides 17–18

**17 · Estatísticas**
- Top clientes (via `vw_top_clientes_receita`)
- Receita por método · ocupação por espaço

**18 · DEMO ao vivo**
- App já aberta → status bar **LIGADO** (fala mesmo com o SQL Server)
- 1) Criar reserva sobreposta → **trigger rejeita** (regra na BD)
- 2) Gerar **Recibo PDF**
- 3) (se houver tempo) toggle dark + estatísticas

---

## ▌ BRUNO — Slides 19–20

**19 · Em números**
- 12 tabelas BCNF · 16 triggers · 18+ SPs · 11 views · 7 UDFs
- Regras em 3 níveis · concorrência segura · auditoria temporal
- **2 correções do professor resolvidas** (snapshot + reserva associativa)

**20 · Obrigado**
- Obrigado — disponíveis para perguntas
