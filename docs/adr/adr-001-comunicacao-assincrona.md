# ADR-001 — Comunicação assíncrona entre os domínios

## Contexto

O sistema possui dois domínios independentes: Registros e Posição. O principal requisito não-funcional determina que a indisponibilidade do domínio de Posição não deve impedir o funcionamento do domínio de Registros. Essa regra define a principal decisão de integração entre os dois domínios.

---

## Problema de negócio

O comerciante precisa registrar lançamentos a qualquer momento, mesmo que o processamento do consolidado esteja indisponível. Um lançamento não registrado representa perda de dado financeiro, o que é inaceitável. Um atraso no consolidado, por outro lado, é tolerável dentro de um limite definido (até 60 segundos, conforme RN-013).

---

## Opções consideradas

**Opção 1: Chamada HTTP síncrona de Registros para Posição**
Após persistir o lançamento, Registros API chama Posição Processor via HTTP para notificar o novo lançamento. Ambos os serviços participam da mesma cadeia de resposta.

**Opção 2: Polling periódico por parte de Posição**
Posição Processor consulta periodicamente o banco de Registros em busca de lançamentos novos que ainda não foram consolidados.

**Opção 3: Comunicação assíncrona via broker de mensagens**
Registros publica um evento em um broker (RabbitMQ). Posição Processor consome o evento de forma independente, no seu próprio ritmo, sem participar da transação de escrita.

---

## Decisão

**Opção 3: comunicação assíncrona via broker de mensagens (RabbitMQ)**

---

## Justificativa

A chamada HTTP síncrona (Opção 1) cria acoplamento temporal direto: se Posição estiver fora do ar, Registros também falha. Isso viola o requisito central do sistema.

O polling (Opção 2) introduz latência indeterminada, gera carga constante no banco de Registros e acopla Posição ao schema de Registros, violando as fronteiras dos bounded contexts.

O broker desacopla os dois domínios no tempo e no espaço. Registros publica o evento e retorna 201 ao comerciante imediatamente. Posição consome quando disponível. A fila persiste os eventos durante períodos de indisponibilidade e os entrega assim que o serviço voltar.

---

## Prós e contras

| Opção | Prós | Contras |
|-------|------|---------|
| HTTP síncrono | Simples; sem infraestrutura adicional | Acoplamento temporal; falha de Posição derruba Registros |
| Polling | Sem dependência em tempo real | Latência alta; carga no banco de Registros; acoplamento de schema |
| **Broker assíncrono** | **Desacoplamento total; resiliência; escalabilidade independente por domínio** | **Complexidade operacional; consistência eventual** |

---

## Limitações

- A consistência entre os domínios é eventual. O RN-013 aceita até 60 segundos de defasagem, o que torna essa limitação aceitável para o negócio.
- O broker se torna um componente crítico da infraestrutura. A alta disponibilidade do RabbitMQ deve ser garantida em produção com cluster de múltiplos nós.
- O diagnóstico de problemas em fluxos assíncronos é mais complexo do que em fluxos síncronos, o que reforça a necessidade de observabilidade com traces distribuídos.

---

## Quando evoluir

Se o volume de mensagens crescer além da capacidade do RabbitMQ em cluster, avaliar migração para Apache Kafka, que oferece maior throughput, particionamento e retenção configurável de mensagens, habilitando também reprocessamento histórico de eventos.
