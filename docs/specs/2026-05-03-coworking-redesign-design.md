# Coworking App — Redesign da Interface

**Data:** 2026-05-03  
**Projeto:** APF-T — Sistema de Gestão de Coworking  
**Stack:** C# Windows Forms, .NET 4.8, SQL Server 2019 Express

---

## Contexto e Problemas a Resolver

A aplicação atual tem os seguintes problemas identificados:

1. **Sem edição de registos existentes** — não existe botão "Editar"; o caminho UPDATE existe no código mas nunca é ativado.
2. **Navegação por janelas separadas** — `FormMain` abre cada secção com `new FormXXX().Show()`, resultando em múltiplas janelas independentes.
3. **Layout quebra ao maximizar** — propriedades `Anchor`/`Dock` não definidas nos controlos.
4. **Contexto não claro** — o utilizador não percebe se está num painel de administração ou numa interface de cliente.
5. **Design antiquado** — aspeto Windows 95, sem cores, sem hierarquia visual.
6. **Moeda sem formatação** — valores monetários sem símbolo € nem separador de milhar.

---

## Decisões de Design

| Decisão | Escolha | Alternativa rejeitada |
|---|---|---|
| Navegação | Sidebar fixa, janela única | Botão Voltar + multi-janela |
| Tema de cor | Light Clean (azul #1d4ed8 + fundo #f0f9ff) | Dark Pro (índigo), Teal |
| Layout CRUD | Lista em cima, formulário de edição em baixo | Lista + painel lateral direito |

---

## Arquitetura

### Estrutura de ficheiros

```
CoworkingApp/
├── FormMain.cs / .Designer.cs      ← shell único (sidebar + painel de conteúdo)
├── Theme.cs                        ← constantes de cor e helpers de estilo
├── Controls/
│   ├── UcDashboard.cs / .Designer.cs
│   ├── UcClientes.cs / .Designer.cs
│   ├── UcPlanos.cs / .Designer.cs
│   ├── UcEspacos.cs / .Designer.cs
│   ├── UcSalas.cs / .Designer.cs
│   ├── UcPostos.cs / .Designer.cs
│   ├── UcAdesoes.cs / .Designer.cs
│   ├── UcReservas.cs / .Designer.cs
│   ├── UcPagamentos.cs / .Designer.cs
│   └── UcRelatorios.cs / .Designer.cs
├── Database.cs                     ← sem alterações
└── Program.cs                      ← sem alterações
```

Os `Form*` existentes são **eliminados** e substituídos pelos `UserControl` acima. Inclui `FormNovaReserva.cs` — a lógica de criação de reserva é absorvida por `UcReservas`.

### FormMain — shell

- Janela única, `MinimumSize = 900×600`, `WindowState = Maximized` por omissão.
- Painel esquerdo (`pnlSidebar`): `Width=180`, `Dock=Left`, fundo `#1d4ed8`.
  - Título "COWORKING" + subtítulo "PAINEL DE GESTÃO".
  - Botões de navegação: Dashboard, Clientes, Planos, Espaços, Salas, Postos, Adesões, Reservas, Pagamentos, Relatórios.
  - Botão ativo destacado com fundo `#1e40af`.
- Painel direito (`pnlContent`): `Dock=Fill`, fundo `#f0f9ff`.
  - Carrega o `UserControl` correspondente ao botão clicado.
  - Ao trocar de secção, o UC anterior é removido do painel e o novo adicionado com `Dock=Fill`.
  - Cada navegação cria uma nova instância do UC (`new UcClientes()`, etc.) — garante dados sempre frescos sem estado residual.

### Padrão CRUD (todos os UserControls de gestão)

Cada UC tem três zonas verticais:

```
┌─────────────────────────────────────────┐
│  [+ Novo]  [Editar]  [Eliminar]         │  ← pnlToolbar  (Dock=Top, Height=44)
├─────────────────────────────────────────┤
│                                         │
│  DataGridView (lista de registos)       │  ← dgv  (Dock=Fill)
│                                         │
├─────────────────────────────────────────┤
│  Formulário de edição / criação         │  ← pnlForm  (Dock=Bottom, Height=variável)
│  [Guardar]  [Cancelar]                  │  ← visível apenas em modo edição
└─────────────────────────────────────────┘
```

**Estados:**

| Estado | pnlForm | Botões ativos |
|---|---|---|
| Nenhum registo selecionado | oculto | + Novo |
| Registo selecionado | oculto | + Novo, Editar, Eliminar |
| Modo Novo (`_editId = -1`) | visível, campos limpos | Guardar, Cancelar |
| Modo Editar (`_editId = N`) | visível, campos preenchidos | Guardar, Cancelar |

**Fluxo Editar:**
1. Utilizador seleciona linha no DataGridView → `_editId` fica com o ID da linha.
2. Clica "Editar" → `pnlForm.Visible = true`, campos preenchidos com os valores da linha, foco no primeiro campo.
3. Clica "Guardar" → validação → `UPDATE` → `pnlForm.Visible = false` → `LoadData()`.
4. Clica "Cancelar" → `pnlForm.Visible = false`, `_editId` mantém o valor (linha continua selecionada).

**Fluxo Novo:**
1. Clica "+ Novo" → `_editId = -1` → campos limpos → `pnlForm.Visible = true`.
2. Clica "Guardar" → validação → `INSERT` → `pnlForm.Visible = false` → `LoadData()`.

### UcDashboard

Ecrã inicial. Não tem CRUD. Mostra:

- 4 cards de resumo (Dock=Top, layout em FlowLayoutPanel):
  - **Clientes** — `SELECT COUNT(*) FROM cliente`
  - **Reservas Ativas** — `SELECT COUNT(*) FROM reserva WHERE estado IN ('Pendente','Confirmada')`
  - **Adesões Ativas** — `SELECT COUNT(*) FROM adesao WHERE estado='Ativa'`
  - **Receita este mês** — `SELECT SUM(valor) FROM pagamento WHERE estado='Pago' AND MONTH(data_pagamento)=MONTH(GETDATE()) AND YEAR(data_pagamento)=YEAR(GETDATE())`
- Tabela de pagamentos recentes (`SELECT TOP 10 ...` por `data_pagamento DESC`).

Botão de refresh no toolbar do Dashboard.

### Formatação Monetária

Classe `Theme.cs` expõe um método utilitário:

```csharp
public static string FormatEuro(decimal value)
    => value.ToString("#,##0.00", new System.Globalization.CultureInfo("pt-PT")) + " €";
```

Aplicado em:
- Todos os `DataGridView` com colunas de valor (via `CellFormatting` ou formatação da coluna).
- Todos os `Label` que mostram valores calculados.
- `UcDashboard` — card de receita.

### Tema Visual (`Theme.cs`)

```csharp
public static class Theme {
    public static Color SidebarBg       = ColorTranslator.FromHtml("#1d4ed8");
    public static Color SidebarActive   = ColorTranslator.FromHtml("#1e40af");
    public static Color SidebarText     = ColorTranslator.FromHtml("#bfdbfe");
    public static Color SidebarTextAct  = Color.White;
    public static Color ContentBg       = ColorTranslator.FromHtml("#f0f9ff");
    public static Color GridHeader      = ColorTranslator.FromHtml("#1d4ed8");
    public static Color GridHeaderText  = Color.White;
    public static Color GridRowAlt      = ColorTranslator.FromHtml("#f8fafc");
    public static Color GridSelected    = ColorTranslator.FromHtml("#dbeafe");
    public static Color FormBorder      = ColorTranslator.FromHtml("#bfdbfe");
    public static Color BtnPrimary      = ColorTranslator.FromHtml("#1d4ed8");
    public static Color BtnDanger       = ColorTranslator.FromHtml("#ef4444");
    public static Font  FontBase        = new Font("Segoe UI", 9.5f);
    public static Font  FontTitle       = new Font("Segoe UI", 14f, FontStyle.Bold);
    public static Font  FontLabel       = new Font("Segoe UI", 8f);
}
```

Aplicado via `FormMain` e helpers estáticos, não repetido em cada UC.

### Layout Responsivo

- `pnlSidebar`: `Dock=Left`
- `pnlContent`: `Dock=Fill`
- Dentro de cada UC:
  - `pnlToolbar`: `Dock=Top`
  - `dgv`: `Dock=Fill`
  - `pnlForm`: `Dock=Bottom` (visível/oculto)
- Nenhum controlo usa posição absoluta (`Location`) exceto dentro do `pnlForm` que usa `TableLayoutPanel` com colunas percentuais.

### DataGridView — Estilo

- `AutoGenerateColumns = false` — colunas definidas explicitamente, sem coluna ID visível.
- `ReadOnly = true` — edição apenas via formulário em baixo.
- `SelectionMode = FullRowSelect`.
- `RowHeadersVisible = false`.
- Cabeçalho: `EnableHeadersVisualStyles = false`, `BackColor = GridHeader`, `ForeColor = GridHeaderText`.
- Linhas alternadas: `dgv.AlternatingRowsDefaultCellStyle.BackColor = GridRowAlt`.
- Linha selecionada: `DefaultCellStyle.SelectionBackColor = GridSelected`, `SelectionForeColor = Color.Black`.
- `BorderStyle = None`, `CellBorderStyle = SingleHorizontal`.

---

## Fora de Âmbito

- Autenticação / login (não existe no projeto).
- Interface de cliente (app é exclusivamente de gestão/administração).
- Animações CSS (Windows Forms não suporta).
- Alterações à base de dados ou DDL.
- Relatórios existentes (`FormRelatorios`) — migrados para `UcRelatorios` sem alteração de lógica.

---

## Critérios de Sucesso

- [ ] Toda a navegação acontece dentro de uma única janela.
- [ ] É possível editar qualquer registo existente sem recriar os campos manualmente.
- [ ] Maximizar a janela não quebra nenhum layout.
- [ ] Todos os valores monetários aparecem no formato `1.250,00 €`.
- [ ] O Dashboard mostra os 4 indicadores e a tabela de pagamentos recentes.
- [ ] O subtítulo "PAINEL DE GESTÃO" está sempre visível na sidebar.
