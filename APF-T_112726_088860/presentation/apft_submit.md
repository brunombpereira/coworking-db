# Submissão APF-T — Instruções para o avaliador

**Sistema de Gestão de Coworking** · Bruno Pereira (MEC 112726) · Rafael Claro (MEC 088860)

Disciplina: Base de Dados — LECI (3.º ano)

---

## 1. Conteúdo do entregue

```
APF-T_112726_088860/
├── presentation/
│   ├── Relatorio_APF-T.pdf            ← Relatório técnico completo (16 págs)
│   ├── Apresentacao_APF-T.pdf          ← Slides da apresentação oral (PDF)
│   ├── DER.md / EsquemaRelacional.md   ← Modelo de dados (mermaid + texto)
│   ├── Requisitos.md (em APF-E)        ← Análise de requisitos original
│   └── docs/screenshots/               ← Capturas da aplicação
└── source/
    ├── CoworkingApp.sln                ← Solution Visual Studio 2026
    ├── CoworkingApp/                   ← Projecto C# WinForms (.NET 8)
    └── SQL_Scripts/                    ← Scripts SQL separados por tópico
```

---

## 2. Configurar a base de dados (passo 1)

Os scripts SQL estão em `source/SQL_Scripts/` e devem ser executados **pela ordem abaixo** numa base de dados chamada `CoworkingDB`:

| # | Script | O que faz |
|---|---|---|
| 1 | `SQL_DDL.sql` | Schema base (tabelas, FKs, CHECKs, UNIQUE) |
| 2 | `User_defined_functions.sql` | UDFs (`fn_recurso_disponivel`, `fn_taxa_ocupacao_espaco`, …) |
| 3 | `Triggers.sql` | 16 triggers de regras de negócio |
| 4 | `Views.sql` | 11 views de consulta/relatório |
| 5 | `Stored_procedures.sql` | SPs de negócio (`sp_criar_reserva_*`, `sp_registar_pagamento`, …) |
| 6 | `Temporal_tables.sql` | `SYSTEM_VERSIONING` em `adesao`, `reserva`, `pagamento` |
| 7 | `Indexes.sql` | Índices não-clustered |
| 8 | `SQL_DML.sql` | Seed: 20 clientes, 25 adesões, ~72 reservas, ~9 day passes, pagamentos |

**Execução em batch (PowerShell):**

```powershell
cd APF-T_112726_088860\source\SQL_Scripts
@("SQL_DDL", "User_defined_functions", "Triggers", "Views",
  "Stored_procedures", "Temporal_tables", "Indexes", "SQL_DML") | % {
    sqlcmd -S .\SQLEXPRESS -d CoworkingDB -E -i "$_.sql"
}
```

**Reset completo** (se já existir uma `CoworkingDB`): correr `Drop_tables.sql` antes da sequência acima.

Plano de testes em `SQL_Scripts/Tests/Plano_Testes.md`.

---

## 3. Configurar a connection string (passo 2)

A aplicação lê a connection string de **`source/CoworkingApp/App.config`**:

```xml
<connectionStrings>
    <add name="CoworkingDB"
         connectionString="Server=.\SQLEXPRESS;Database=CoworkingDB;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;" />
</connectionStrings>
```

**Para apontar para outro servidor / utilizador da BD:**

- **Mudar de servidor**: substituir `Server=.\SQLEXPRESS` (ex. `Server=tcp:servidor.aula.pt,1433`).
- **Usar SQL Auth** (em vez de Windows Auth): remover `Integrated Security=True` e adicionar `User Id=<utilizador>;Password=<password>;`.

Exemplo com SQL Auth para o servidor da disciplina:

```xml
<add name="CoworkingDB"
     connectionString="Server=<servidor_aula>;Database=CoworkingDB;User Id=<aluno>;Password=<password>;TrustServerCertificate=True;Encrypt=False;" />
```

`Database.cs:9` lê a connection string via `ConfigurationManager.ConnectionStrings["CoworkingDB"].ConnectionString` — não há credenciais hardcoded em código.

---

## 4. Correr a aplicação (passo 3)

**Requisitos**: .NET 8 SDK + Visual Studio Community 2026 (ou superior).

1. Abrir `source/CoworkingApp.sln` no Visual Studio.
2. `Build > Rebuild Solution` — restaura NuGet automaticamente.
3. F5 (Debug) ou Ctrl+F5 (Run sem debug).

A app abre **directamente no Dashboard** — sem ecrã de login. O status bar mostra **LIGADO** (verde) se a connection string estiver correcta.

**Linha de comando** (sem VS):

```powershell
cd APF-T_112726_088860\source\CoworkingApp
dotnet build
dotnet run
```

---

## 5. Funcionalidades a explorar

| UserControl | O que demonstra |
|---|---|
| **Dashboard** | KPIs operacionais, charts (Receita mensal, métodos de pagamento), próximas reservas |
| **Clientes** | CRUD + filtros + detalhe read-only com adesões/reservas/pagamentos do cliente |
| **Planos** | Catálogo SaaS (Flex/Fixo/Privado) com pricing cards |
| **Espaços** | Tabs Espaços/Salas/Postos — CRUD para cada |
| **Reservas** | Lista + **tab Mapa** (timeline horária); criar reserva valida T1 (sobreposição) e T11 (posto vs adesão) |
| **Adesões** | CRUD; ao criar valida T4 (1 activa por cliente) |
| **Pagamentos** | CRUD; **botão "↓ Recibo PDF"** no detalhe gera A4 com snapshot do preço |
| **Notificações** | Triggers T13/T14/T15 emitem automaticamente ao criar/cancelar reserva ou pagar |
| **Relatórios** | Tabs Disponibilidade / Receita por Cliente / Análise (charts) |
| **Estatísticas** | Top clientes, receita por método/espaço, ocupação |

---

## 6. Stack e dependências

| Componente | Versão |
|---|---|
| SQL Server | 2019 Express ou superior |
| .NET | 8.0 (`net8.0-windows`) |
| Visual Studio | Community 2026 |
| **NuGet packages** (auto-restored) | `Microsoft.Data.SqlClient` 5.2.2 · `System.Configuration.ConfigurationManager` 8.0.0 · `WinForms.DataVisualization` 1.10.0 · `FontAwesome.Sharp` 6.6.0 · `PdfSharp` 6.1.1 |

---

## 7. Estrutura do projecto C#

```
CoworkingApp/
├── Program.cs              ← Entry point — abre directo o FormMain
├── FormMain.cs             ← Shell com sidebar + Navigate<T>()
├── Database.cs             ← Wrapper ADO.NET (GetConnection, IsAvailable)
├── App.config              ← Connection string (ver §3)
├── Theme.cs                ← Design tokens (cores, fontes, padding constants)
├── ThemeManager.cs         ← Light/Dark toggle (persiste em %LocalAppData%)
├── Controls/               ← 10 UserControls (Dashboard, Clientes, Planos, …)
├── ModernCard.cs, ModernButton.cs, ModernSelect.cs, ModernDateField.cs,
│   ModernCalendar.cs, ScrollableList.cs, SegmentedControl.cs,
│   StatusPill.cs, TabButton.cs, ToggleChip.cs    ← Componentes custom
└── ReciboPdf.cs            ← Gerador PDF (PdfSharp)
```

Toda a comunicação com a BD passa por `SqlCommand` parametrizado (`Database.cs` + invocações pontuais nos UCs). Sem `EXEC(@sql)` nem concatenação de strings — robusto a SQL injection por construção.

---

## 8. Correcções do professor

| # | Correção | Implementação |
|---|---|---|
| 1 | *"Qual o preço?"* no Pagamento | Coluna `preco_servico_snapshot` + CHECK `valor = preco_servico_snapshot` + trigger T6. Detalhes em §3.2 do relatório. |
| 2 | Reserva como entidade associativa M:N | Tabela única `reserva` com FK para `recurso` (supertype `sala`/`posto`). T12 enforça as semânticas diferentes. |
