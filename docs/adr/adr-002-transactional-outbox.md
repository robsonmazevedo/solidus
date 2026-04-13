# ADR-002 — Transactional Outbox no domínio de Registros

## Contexto

O domínio de Registros precisa, a cada lançamento, realizar duas operações: persistir o lançamento no banco e publicar um evento no broker. O RN-007 é explícito: as duas operações devem ocorrer de forma atômica. Não é aceitável registrar sem notificar, nem notificar sem registrar.

---

## Problema de negócio

Uma transação de banco de dados e uma publicação em broker são dois recursos distintos. Não existe transação distribuída que abranja os dois de forma nativa e confiável. Se as operações forem executadas sequencialmente, sempre haverá uma janela de falha entre o commit no banco e a publicação no broker, com dois cenários de inconsistência possíveis:

- O banco commita e o broker está indisponível: lançamento registrado, evento nunca publicado, Posição nunca atualizada.
- A aplicação cai após a publicação e antes do commit: evento publicado, lançamento não persistido.

---

## Opções consideradas

**Opção 1: Publicação direta após commit**
A aplicação faz o commit no banco e, em seguida, publica o evento no broker. Duas operações sequenciais e independentes.

**Opção 2: Saga com compensação**
Em caso de falha na publicação, uma saga executa uma operação de compensação para desfazer o lançamento no banco, mantendo a consistência entre os dois recursos.

**Opção 3: Transactional Outbox**
A aplicação, dentro de uma única transação ACID no banco, persiste o lançamento e registra o evento em uma tabela `outbox`. Um processo relay separado lê os registros pendentes na outbox e publica os eventos no broker, marcando-os como publicados.

---

## Decisão

**Opção 3: Transactional Outbox**

---

## Justificativa

A publicação direta (Opção 1) mantém a janela de falha descrita no problema. Se o broker estiver indisponível no momento do `POST /lancamentos`, o lançamento é persistido mas o evento nunca é publicado, quebrando a atomicidade exigida pelo RN-007.

A Saga com compensação (Opção 2) é desproporcional para este problema. Requer orquestrador de saga, lógica de compensação e gerenciamento de estado distribuído, adicionando complexidade sem benefício real neste contexto.

O Transactional Outbox resolve o problema usando apenas o banco de dados como mecanismo de coordenação, sem infraestrutura adicional. A atomicidade é garantida pela própria transação ACID do PostgreSQL: ou tanto o lançamento quanto o registro na outbox são persistidos, ou nenhum dos dois é. O relay pode falhar e ser reiniciado sem perda de dados; os eventos permanecem na outbox com status `PENDENTE` até serem publicados com sucesso.

---

## Prós e contras

| Opção | Prós | Contras |
|-------|------|---------|
| Publicação direta | Simples; sem tabela adicional | Janela de falha entre commit e publicação |
| Saga | Flexível para fluxos complexos | Complexidade desproporcional para este cenário |
| **Transactional Outbox** | **Atomicidade garantida pelo banco; sem dependência do broker na transação principal** | **Tabela e processo relay adicionais; latência mínima na publicação** |

---

## Limitações

- A latência de publicação depende da frequência de polling do relay. Essa latência é operacionalmente irrelevante para o padrão de uso de um sistema de lançamentos financeiros.
- A tabela `outbox` cresce com registros no status `PUBLICADO`. Uma rotina de limpeza periódica deve remover registros antigos para evitar crescimento indefinido.
- O relay é um processo adicional que precisa ser monitorado. Se parar, os eventos acumulam na outbox e a defasagem do consolidado aumenta além do limite do RN-013.

---

## Quando evoluir

Se a latência do relay se tornar um gargalo em cenários de volume muito alto, avaliar CDC (Change Data Capture) com Debezium como alternativa ao polling. O CDC captura alterações no banco em tempo quase real via WAL (Write-Ahead Log) do PostgreSQL, eliminando o polling e reduzindo a latência de publicação para sub-segundo.
