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

---

## Interface — Redesign UI/UX

### Motivação

A interface inicial era funcional mas monocrómica (apenas tons de azul saturado), com ícones unicode inconsistentes na sidebar e formulários inline em drawer no rodapé que pouco contribuíam para a hierarquia visual. O objetivo desta fase foi alinhar a aplicação com padrões modernos de gestão SaaS (estilo Linear/Notion/Stripe), introduzir suporte a tema claro e escuro, e tornar os fluxos de criação/edição mais focados.

### Direção estética: *Bespoke modern*

Após brainstorming dirigido com mockups comparativos (Material Design, Fluent/Win11, Bespoke modern), optámos por **bespoke modern** — paleta indigo+slate, custom theming sem biblioteca de terceiros, sidebar escura com agrupamento semântico. Justificação: maior diferenciação visual e controlo sobre a estética, sem dependências pesadas.

### Decisões de design

| Dimensão | Escolha |
|---|---|
| Paleta accent | Indigo-500 `#6366f1` |
| Sidebar light | `#0f172a` (slate-900) |
| Sidebar dark | `#020617` (slate-950) |
| Page bg light/dark | `#f8fafc` / `#0f172a` |
| Card bg light/dark | `#ffffff` / `#1e293b` |
| Modos | Light + Dark com toggle persistente |
| Iconografia | FontAwesome.Sharp (NuGet, ~500 KB) |
| Tipografia | Segoe UI (com escalas de 8 a 28pt) |
| Pattern de form | Modal centrado com overlay (substitui drawer) |
| Status colors | Verde/amarelo/vermelho/cinza/laranja, calibrados para contraste em ambos modos |

### Arquitectura do tema

`Theme.cs` é a *single source of truth* de design tokens. Em vez de campos `static readonly Color`, todas as cores são **propriedades dinâmicas** que invocam um helper `Pick(light, dark)` baseado no `ThemeManager.Current`. Isto permite que o mesmo código que escreveu `BackColor = Theme.CardBg` em qualquer UC funcione em ambos os modos sem alterações.

```csharp
public static Color CardBg => ThemeManager.Current == ThemeMode.Light
    ? L("#ffffff") : L("#1e293b");
```

`ThemeManager` (novo) mantém o modo corrente, dispara `event ThemeChanged` ao alternar, e persiste a preferência em `%LocalAppData%\CoworkingApp\theme.json`. `Program.Main` chama `ThemeManager.Load()` antes de `Application.Run()` para que o tema do utilizador seja restaurado entre sessões.

### Componentes redesenhados

**Sidebar** (200 px) — header com logo gradient indigo + título, items agrupados em **OPERACIONAL** (Dashboard, Clientes, Planos, Espaços, Reservas) e **FINANCEIRO** (Adesões, Pagamentos, Relatórios). Cada item é um `IconButton` da FontAwesome.Sharp. Footer contém o botão de toggle light/dark.

**Status bar** custom (32 px) com indicador do módulo activo à esquerda e relógio actualizado a 1 Hz à direita.

**Dashboard** redesenhado com layout *Hero KPI + insights*:
- Card **hero** com gradient indigo (135°) destacando a Receita do mês, indicador de delta vs mês anterior, e sparkline horizontal dos últimos 6 meses.
- Dois **KPI cards** ao lado: Reservas hoje, Adesões activas.
- Lista de **Próximas reservas** (top 5).
- **Pie chart** dos métodos de pagamento.

**FormDialog** — componente genérico que substitui o drawer de criar/editar em todos os UserControls. Modal centrado com overlay opaco, header com botão de fechar, body com scroll, footer com Guardar/Cancelar. ESC e ✕ cancelam. O delegate `onSave` lança `ApplicationException` para validação (apresentada em MessageBox de aviso) e `SqlException` para erros de BD (formatados via `Database.SqlErrorMessage`).

**UserControls** (Clientes, Planos, Espaços, Reservas, Adesões, Pagamentos) — todos refactorados para o pattern uniforme: title + toolbar + DataGridView + `OpenEditor(int? id)` que constrói o conteúdo e abre o `FormDialog`. Helpers `internal static` em `UcClientes` (`AddField`, `AddCombo`, `AddComboDataSource`, `AddDate`) garantem consistência visual entre formulários.

**Charts** (Dashboard e Relatórios) repintados com paleta indigo/violet/emerald/amber/red, áreas transparentes, eixos com `Theme.CardBorder`.

### Decisões e trade-offs documentados

- **Tema dinâmico apenas em controlos novos.** O `Theme.X` recompõe-se a cada acesso (getter dinâmico), mas controlos já instanciados conservam as cores da última pintura. Após toggle, a sidebar e novas modais aparecem no novo tema; UCs já abertos só refletem a alteração ao re-navegar. Decisão: implementar subscritores de `ThemeChanged` em cada UC seria significativo (8 UCs com lógica recursiva de re-aplicação) e o ganho académico é marginal — preferiu-se documentar a limitação.
- **Modal vs side drawer.** A spec original considerou o pattern *side drawer* (estilo Notion). Optámos por modal centrado por ser mais consistente com convenções desktop e exigir menos rework de layout dos formulários existentes.
- **`new Font(...)` repetido.** Cada `IconButton` da sidebar e cada hover/active swap em `SetActive` instanciam um novo `Font`. Em produção seriam campos `static readonly` reutilizáveis; para o âmbito académico (uma instância de `FormMain` por sessão) o custo é desprezável.
- **Two-step `INSERT recurso` + `INSERT sala/posto`** sem transação. Se o segundo INSERT falhar, fica um `recurso` órfão. Para produção exigiria `SqlTransaction`; para o seed académico o risco é baixo.

### Validação

- Build limpo: `dotnet build` retorna 0 warnings, 0 errors.
- App arranca em modo dark se essa for a preferência persistida; toggle light↔dark funciona instantaneamente nos elementos novos.
- 17/17 triggers SQL e 4/4 fluxos manuais (Flex, Fixo, Sala, Posto avulso) confirmados.
- Screenshots em `presentation/docs/screenshots/` (light e dark mode para cada UserControl) para inclusão na apresentação.

### Métricas

- 23 commits dedicados ao redesign UI (`32e71b6` → `8e383c3`), todos com prefixos convencionais (`feat/refactor/fix/chore/docs(ui)`).
- ~2400 linhas de código removidas (drawer machinery + duplicação) contra ~1800 linhas adicionadas (componentes novos + reescrita do Dashboard) — net negativo de ~600 linhas.
- 1 nova dependência (`FontAwesome.Sharp 6.6.0`).
