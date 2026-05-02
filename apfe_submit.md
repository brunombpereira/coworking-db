# BD: Trabalho Prático APFE

**Grupo**:  
- Bruno Pereira, MEC: 112726  
- Rafael Claro, MEC: 088860  

## Introdução
 
Este trabalho prático tem como objetivo a análise e modelação de uma base de dados para um **Sistema de Gestão de Coworking e Reserva de Salas**. O sistema proposto pretende apoiar a gestão de clientes, planos de adesão, espaços, salas de reunião, postos de trabalho, reservas e pagamentos, permitindo uma organização mais eficiente dos recursos disponíveis.

## Análise de Requisitos / Requirements

### 1. Objetivo do sistema
O sistema deve permitir a gestão integrada de um espaço de coworking, centralizando a informação relativa a clientes, planos, espaços físicos, salas, postos de trabalho, reservas e pagamentos.

### 2. Requisitos funcionais
O sistema deverá permitir:

1. Registar clientes do coworking.
2. Armazenar os dados de identificação e contacto de cada cliente.
3. Registar os diferentes planos de adesão disponibilizados.
4. Associar clientes a planos através de adesões.
5. Manter o histórico de adesões de cada cliente.
6. Registar os espaços físicos do coworking.
7. Registar as salas existentes em cada espaço.
8. Registar os postos de trabalho existentes em cada espaço.
9. Efetuar reservas de salas de reunião.
10. Efetuar reservas de postos de trabalho.
11. Consultar o histórico de reservas de um cliente.
12. Consultar a ocupação de salas e postos de trabalho.
13. Registar pagamentos efetuados pelos clientes.
14. Consultar o histórico de pagamentos de cada cliente.
15. Controlar o estado das reservas e das adesões.

### 3. Requisitos não funcionais
O sistema deverá assegurar:

- integridade e consistência dos dados;
- unicidade do NIF e do email de cada cliente;
- facilidade de consulta da disponibilidade dos recursos;
- possibilidade de crescimento do número de clientes, reservas e pagamentos;
- clareza e organização do modelo conceptual e relacional.

### 4. Regras de negócio
1. Um cliente pode ter zero, uma ou várias adesões.
2. Cada adesão pertence a um único cliente e a um único plano.
3. Um plano pode estar associado a vários clientes ao longo do tempo.
4. Um espaço pode incluir várias salas.
5. Um espaço pode disponibilizar vários postos de trabalho.
6. Cada sala pertence a um único espaço.
7. Cada posto de trabalho pertence a um único espaço.
8. Um cliente pode efetuar várias reservas de salas.
9. Um cliente pode efetuar várias reservas de postos de trabalho.
10. Cada reserva de sala refere-se a uma única sala.
11. Cada reserva de posto refere-se a um único posto de trabalho.
12. Uma sala não pode ter reservas sobrepostas no mesmo período temporal.
13. Um posto de trabalho não pode ter reservas sobrepostas no mesmo período temporal.
14. Um cliente pode realizar vários pagamentos.
15. Cada pagamento é associado a um único cliente.

### 5. Entidades identificadas
As principais entidades do sistema são:

- **Cliente**
- **Plano**
- **Espaço**
- **Sala**
- **Posto de Trabalho**
- **Pagamento**

Os eventos e relações relevantes modelados no DER incluem:

- **Subscreve** (Cliente–Plano)
- **Reserva Sala** (Cliente–Sala)
- **Reserva Posto** (Cliente–Posto de Trabalho)
- **Inclui** (Espaço–Sala)
- **Disponibiliza** (Espaço–Posto de Trabalho)
- **Efetua** (Cliente–Pagamento)

## DER

O Diagrama Entidade-Relacionamento apresenta as entidades principais do sistema, os respetivos atributos, os relacionamentos e as cardinalidades entre eles.

![DER Diagram!](der.png "AnImage")

## ER

O Esquema Relacional resulta da transformação do DER para o modelo relacional, identificando as tabelas, as chaves primárias e as chaves estrangeiras.

![ER Diagram!](er.png "AnImage")
