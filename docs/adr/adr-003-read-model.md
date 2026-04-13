# ADR-003 — Read model materializado para a posição diária

## Contexto

O domínio de Posição precisa disponibilizar ao comerciante o saldo consolidado de um dia específico, contendo total de créditos, total de débitos e saldo resultante. O RNF define que a API de Posição deve suportar 50 requisições por segundo com no máximo 5% de erro.

---

## Problema de negócio

O consolidado diário é um dado calculado a partir das movimentações registradas. Calculá-lo em tempo real significa executar agregações sobre a tabela `lancamentos` a cada requisição. Em um sistema com histórico crescente e 50 req/s, essa abordagem pressiona o banco do domínio de Registros e aumenta a latência de resposta da consulta.

---

## Opções consideradas

**Opção 1: Consulta sob demanda (aggregation em tempo real)**
A cada requisição `GET /posicao/diaria`, o sistema executa `SELECT SUM(valor)` filtrado por comerciante, tipo e data diretamente na tabela `lancamentos`.

**Opção 2: View materializada no PostgreSQL**
Uma view materializada é mantida no banco, atualizada periodicamente ou por trigger, pré-calculando o consolidado por comerciante e data.

**Opção 3: Read model separado**
Uma tabela `posicao_diaria` é mantida pelo domínio de Posição e atualizada de forma assíncrona a partir dos eventos de movimentação recebidos pelo broker.

---

## Decisão

**Opção 3: read model separado (tabela `posicao_diaria`)**

---

## Justificativa

A consulta sob demanda (Opção 1) acopla o domínio de Posição ao schema e ao banco de Registros, violando o isolamento dos bounded contexts. Concentra também a carga de leitura no mesmo banco responsável pela escrita de lançamentos, criando contenção.

A view materializada (Opção 2) mantém o acoplamento de schema entre os dois domínios no nível do banco de dados e limita a capacidade de escalar Posição de forma independente. O refresh por trigger reintroduz acoplamento síncrono.

O read model separado é consistente com o padrão CQRS e com as fronteiras dos bounded contexts. O domínio de Posição é dono do seu próprio dado, atualizado a partir dos eventos que já circulam pelo broker. A consulta se torna uma leitura direta por chave (`comerciante_id` + `data_posicao`), com latência constante independente do volume histórico de lançamentos.

---

## Prós e contras

| Opção | Prós | Contras |
|-------|------|---------|
| Consulta sob demanda | Sempre atualizado; sem infraestrutura extra | Acoplamento de schema; carga crescente no banco de escrita |
| View materializada | Simples de implementar no banco | Acoplamento de banco; refresh periódico ou por trigger |
| **Read model separado** | **Isolamento total entre domínios; consulta com latência constante; escalável independentemente** | **Consistência eventual; dado pode estar defasado** |

---

## Limitações

- O consolidado pode apresentar defasagem de até 60 segundos em relação ao último lançamento registrado (RN-013). Esse limite é definido pelo requisito e é aceito pelo negócio.
- Se o Posição Processor ficar indisponível por um período prolongado, a defasagem aumenta além do limite. Os eventos ficam retidos na fila do broker e são processados quando o serviço voltar, sem perda de dados.

---

## Quando evoluir

Se o requisito de defasagem máxima for reduzido para sub-segundo, avaliar processamento de stream com Apache Kafka + Kafka Streams para atualização em tempo quase real do read model, eliminando a dependência do ciclo de polling e processamento em lote.
