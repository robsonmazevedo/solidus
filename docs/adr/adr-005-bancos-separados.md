# ADR-005 — Isolamento de dados entre os domínios

## Contexto

Os dois domínios, Registros e Posição, possuem dados distintos e responsabilidades distintas. O princípio de isolamento dos bounded contexts deve se refletir também na camada de dados, garantindo que operações em um domínio não afetem o outro em nenhum nível.

---

## Problema de negócio

O requisito de que Registros nunca pode cair por causa de Posição se estende à camada de dados: uma operação de manutenção, migração, sobrecarga ou falha no banco de dados de Posição não deve afetar de nenhuma forma o banco de dados de Registros. Um banco compartilhado, mesmo com schemas separados, cria um ponto único de falha para os dois domínios.

---

## Opções consideradas

**Opção 1: Schema único com tabelas compartilhadas**
Todas as tabelas dos dois domínios no mesmo schema, no mesmo servidor PostgreSQL, sem separação lógica.

**Opção 2: Schemas separados no mesmo banco**
Schema registros e schema posicao no mesmo servidor PostgreSQL. Isolamento lógico, mas instância física compartilhada.

**Opção 3: Bancos de dados separados por domínio**
Dois servidores PostgreSQL distintos, um por domínio. Isolamento físico completo em todos os ambientes.

---

## Decisão

**Opção 3: bancos de dados separados por domínio**

---

## Justificativa

O schema único (Opção 1) viola o isolamento dos bounded contexts: as tabelas dos dois domínios ficam no mesmo namespace, criando risco concreto de consultas cruzadas que acoplam os domínios no nível do banco.

Schemas separados no mesmo banco (Opção 2) resolvem o isolamento lógico, mas mantêm um ponto único de falha físico. Uma sobrecarga de conexões, um lock de tabela ou uma operação de manutenção no servidor afeta os dois domínios simultaneamente, violando o mesmo requisito central que motivou a comunicação assíncrona entre os serviços.

Bancos separados (Opção 3) são a única opção que garante isolamento físico completo. Cada serviço tem sua própria connection string, seu próprio ciclo de manutenção e seu próprio limite de recursos. Uma falha no banco de Posição não é sequer visível para o domínio de Registros.

Nota de implementação: no ambiente de desenvolvimento local, os dois domínios podem compartilhar a mesma instância PostgreSQL com schemas separados para reduzir a complexidade do docker-compose. Essa é uma concessão operacional do ambiente local, não uma decisão de arquitetura. O código, as migrations e as connection strings são idênticos ao ambiente de produção.

---

## Prós e contras

| Opção | Prós | Contras |
|-------|------|---------|
| Schema único | Operação simples | Sem isolamento lógico; acoplamento de domínios |
| Schemas separados | Isolamento lógico; operação simples | Ponto único de falha físico; manutenção compartilhada |
| Bancos separados | Isolamento físico total; falha e manutenção independentes; escalamento independente | Dois servidores para operar |

---

## Limitações

- Dois bancos de dados significam dois conjuntos de operações: backups, patches, monitoramento e alertas independentes. Isso é um custo operacional real que deve ser previsto.
- Não há transação distribuída entre os dois bancos. Toda comunicação entre os domínios ocorre via eventos, o que é consistente com a decisão do ADR-001.

---

## Quando evoluir

Se o sistema crescer para mais de dois domínios, avaliar um banco gerenciado por domínio em plataformas como AWS RDS ou Azure Database for PostgreSQL, onde cada instância é provisionada e escalada de forma independente, mantendo o mesmo padrão de isolamento.
