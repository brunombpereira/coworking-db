# Scripts SQL — ordem de execução

Os scripts têm dependências umas das outras. Correr pela ordem abaixo
garante que tudo é criado antes de ser referenciado.

| # | Script | O que faz | Dependências |
|---|---|---|---|
| 1 | `SQL_DDL.sql` | Schema base: tabelas, FKs, CHECKs | — |
| 2 | `User_defined_functions.sql` | UDFs (`fn_taxa_ocupacao_espaco`, etc.) | DDL |
| 3 | `Triggers.sql` | 13 triggers (T1..T13) | DDL + UDFs |
| 4 | `Views.sql` | Views de relatório (`vw_*`) | DDL |
| 5 | `Auth.sql` | `sp_register_user`, `sp_login_user`, `sp_admin_*`, `vw_utilizadores_listagem` | DDL |
| 6 | `Stored_procedures.sql` | SPs de negócio (`sp_registar_cliente_completo`, etc.) | DDL + **Auth** (chama `sp_register_user`) |
| 7 | `Temporal_tables.sql` | System versioning + `vw_reservas_historico` | DDL |
| 8 | `Indexes.sql` | Índices não-clustered | DDL |
| 9 | `Security.sql` | Roles (`app_admin`, `app_staff`, `app_cliente`) + GRANTs | **TUDO o que tem GRANT** já existir |
| 10 | `SQL_DML.sql` | Seed: planos, espaços, ~20 clientes, ~25 adesões, ~72 reservas, ~9 day passes, pagamentos auto-gerados, 22 utilizadores | DDL + Auth (`sp_register_user`) + triggers |

> **`Auth.sql` *antes* de `Stored_procedures.sql`** porque o
> `sp_registar_cliente_completo` chama `sp_register_user`.

## Execução em batch (PowerShell)

Do directório raiz do projecto:

```powershell
@("SQL_DDL", "User_defined_functions", "Triggers", "Views",
  "Auth", "Stored_procedures", "Temporal_tables", "Indexes",
  "Security", "SQL_DML") | % {
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

Depois, correr os 10 scripts pela ordem acima.

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
