# Scripts SQL — ordem de execução

Os scripts têm dependências umas das outras. Correr pela ordem abaixo
garante que tudo é criado antes de ser referenciado.

| # | Script | O que faz | Dependências |
|---|---|---|---|
| 1 | `SQL_DDL.sql` | Schema base: tabelas, FKs, CHECKs | — |
| 2 | `User_defined_functions.sql` | UDFs (`fn_taxa_ocupacao_espaco`, etc.) | DDL |
| 3 | `Triggers.sql` | Triggers de regras de negócio | DDL + UDFs |
| 4 | `Views.sql` | Views de relatório (`vw_*`) | DDL |
| 5 | `Stored_procedures.sql` | SPs de negócio | DDL |
| 6 | `Temporal_tables.sql` | System versioning + `vw_reservas_historico` | DDL |
| 7 | `Indexes.sql` | Índices não-clustered | DDL |
| 8 | `SQL_DML.sql` | Seed: planos, espaços, ~20 clientes, ~25 adesões, ~72 reservas, ~9 day passes, pagamentos auto-gerados | DDL + triggers |

## Execução em batch (PowerShell)

Do directório raiz do projecto:

```powershell
@("SQL_DDL", "User_defined_functions", "Triggers", "Views",
  "Stored_procedures", "Temporal_tables", "Indexes", "SQL_DML") | % {
    Write-Host "→ $_.sql"
    sqlcmd -S .\SQLEXPRESS -d CoworkingDB -E `
           -i "APF-T_112726_088860\source\SQL_Scripts\$_.sql"
}
```

## Reset completo (drop + recreate)

Em SSMS, numa janela ligada a `master` (não a `CoworkingDB`), com
a app **CoworkingApp fechada**:

```sql
USE master;
ALTER DATABASE CoworkingDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
DROP DATABASE CoworkingDB;
CREATE DATABASE CoworkingDB;
```

Depois, correr os 8 scripts pela ordem acima.

## Sub-pasta `Tests/`

`smoke_triggers.sql` — 17 cenários automáticos para validar T1..T13
(cada um deveria devolver `OK` ou `FAIL` numa coluna `resultado`).
Correr **depois** do `SQL_DML` (precisa de dados de seed).

## Sub-pasta `legacy/`

Versões anteriores de scripts mantidas para auditoria do APF-E vs
APF-T. **Não correr.**

## Notas

- **Compatibility level** da BD deve ser **≥ 110** (SQL Server 2012)
  para suportar `THROW`. Verificar com:
  ```sql
  SELECT compatibility_level FROM sys.databases WHERE name='CoworkingDB';
  ```
  Se < 110: `ALTER DATABASE CoworkingDB SET COMPATIBILITY_LEVEL = 150;`
- Erros do *Error List* do SSMS são apenas warnings do IntelliSense
  e podem ser ignorados — o que importa é o que aparece em *Messages*
  ao executar (F5).
