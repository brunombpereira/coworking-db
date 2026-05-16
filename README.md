# Sistema de Gestão de Coworking e Reserva de Salas

Projeto académico da disciplina de Base de Dados — LECI.

## Descrição

Sistema de gestão de um espaço de coworking, permitindo o registo de clientes, planos de adesão, espaços físicos, salas de reunião, postos de trabalho, reservas e pagamentos. O sistema evita conflitos de reserva e permite acompanhar a atividade dos clientes.

## Stack Técnica

| Componente | Tecnologia |
|---|---|
| Base de dados | Microsoft SQL Server 2019 Express |
| Aplicação | C# Windows Forms (.NET Framework) |
| IDE | Visual Studio 2022+ |
| Ferramenta BD | SQL Server Management Studio 22 |

## Pré-requisitos

- SQL Server 2019 Express ou superior
- Visual Studio 2022+ com workload **.NET Desktop Development**
- SQL Server Management Studio 22

## Setup da Base de Dados

1. Abrir SSMS e ligar a `.\SQLEXPRESS`
2. Executar os scripts em `APF-T_112726_088860/source/SQL_Scripts/` pela seguinte ordem (o `SQL_DDL.sql` cria automaticamente a base de dados `CoworkingDB`):
   1. `SQL_DDL.sql` — tabelas + constraints
   2. `Indexes.sql` — índices
   3. `Triggers.sql` — regras de negócio (T1..T16)
   4. `Views.sql` — views de consulta
   5. `User_defined_functions.sql` — UDFs
   6. `Stored_procedures.sql` — SPs operacionais
   7. `Auth.sql` — registo/login (HASHBYTES SHA256 + salt)
   8. `Temporal_tables.sql` — system-versioning (auditoria)
   9. `Security.sql` — DB roles + GRANTs
   10. `SQL_DML.sql` — dados de teste

> Para reset completo: executar `Drop_tables.sql` (já trata de `SYSTEM_VERSIONING OFF` antes de dropar).
> Backups: ver `Backup.sql`.
> Plano de testes: ver `Tests/Plano_Testes.md`.

## Setup da Aplicação

1. Abrir `APF-T_112726_088860/source/CoworkingApp.sln` no Visual Studio

### Login inicial (após executar SQL_DML)

| Username | Password     | Role    |
|----------|--------------|---------|
| admin    | admin1234    | Admin   |
| staff1   | staff1234    | Staff   |
| ana      | cliente1234  | Cliente |
| bruno    | cliente1234  | Cliente |
| carla    | cliente1234  | Cliente |
| diogo    | cliente1234  | Cliente |
| eva      | cliente1234  | Cliente |

Trocar passwords no primeiro arranque em produção.
2. Atualizar a connection string em `App.config`:
   ```xml
   <connectionStrings>
     <add name="CoworkingDB"
          connectionString="Server=.\SQLEXPRESS;Database=CoworkingDB;Trusted_Connection=yes;"
          providerName="System.Data.SqlClient" />
   </connectionStrings>
   ```
3. **Build → Rebuild Solution** e depois **F5** para executar

## Estrutura do Repositório

```
├── APF-E_112726_088860/           # Entrega APF-E (análise de requisitos)
│   ├── Requisitos.md
│   ├── DER.png
│   └── EsquemaRelacional.png
└── APF-T_112726_088860/           # Entrega APF-T (implementação)
    ├── presentation/
    │   ├── apft_submit.md
    │   ├── Relatorio_APF-T.md     # Relatório académico
    │   └── docs/screenshots/
    └── source/
        ├── CoworkingApp.sln       # Projeto C# Windows Forms
        ├── CoworkingApp/          # FormLogin, FormMain (sidebar por role),
        │                          # Session, UcNotificacoes, UcEstatisticas, ...
        └── SQL_Scripts/
            ├── SQL_DDL.sql                # Tabelas + constraints
            ├── SQL_DML.sql                # Dados de teste
            ├── Drop_tables.sql            # Teardown completo
            ├── Indexes.sql                # Índices
            ├── Triggers.sql               # Triggers T1..T16
            ├── Views.sql                  # 12 views de consulta
            ├── User_defined_functions.sql # 7 UDFs
            ├── Stored_procedures.sql      # 14 SPs (com concorrência)
            ├── Auth.sql                   # Registo/login com hash+salt
            ├── Security.sql               # Roles + GRANTs
            ├── Temporal_tables.sql        # SYSTEM_VERSIONING
            ├── Backup.sql                 # Estratégia de backup/restore
            └── Tests/
                ├── Plano_Testes.md        # 25 casos de teste
                ├── smoke_triggers.sql     # T1..T12
                ├── test_auth.sql
                ├── test_concorrencia.sql
                ├── test_temporal.sql
                ├── test_reembolso.sql
                ├── test_lista_espera.sql
                └── test_recorrente.sql
```

## Modelo de Dados

Entidades principais: **Cliente**, **Plano**, **Espaço**, **Sala**, **Posto de Trabalho**, **Adesão**, **Reserva**, **Pagamento**.

Regras chave:
- Uma sala ou posto de trabalho não pode ter reservas sobrepostas no mesmo período
- Cada pagamento está associado a exatamente um serviço (adesão, reserva de sala, ou reserva de posto)
- Um cliente pode ter múltiplas adesões ao longo do tempo
