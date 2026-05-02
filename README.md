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
2. Criar a base de dados:
   ```sql
   CREATE DATABASE CoworkingDB;
   ```
3. Executar `SQL_DDL.sql` contra a base de dados `CoworkingDB`

## Setup da Aplicação

1. Abrir `app/CoworkingApp.sln` no Visual Studio
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
├── SQL_DDL.sql        # DDL: tabelas, constraints, triggers, índices
├── apfe_submit.md     # Análise de requisitos (entrega APF-E)
├── der.png            # Diagrama Entidade-Relacionamento
├── er.png             # Esquema Relacional
└── app/               # Projeto C# Windows Forms (a desenvolver)
```

## Modelo de Dados

Entidades principais: **Cliente**, **Plano**, **Espaço**, **Sala**, **Posto de Trabalho**, **Adesão**, **Reserva**, **Pagamento**.

Regras chave:
- Uma sala ou posto de trabalho não pode ter reservas sobrepostas no mesmo período
- Cada pagamento está associado a exatamente um serviço (adesão, reserva de sala, ou reserva de posto)
- Um cliente pode ter múltiplas adesões ao longo do tempo
