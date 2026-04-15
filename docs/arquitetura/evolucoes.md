# Evoluções Futuras

## 1. Premissa

Este documento registra o que o sistema não implementa na versão atual, por que essa foi uma escolha deliberada e qual seria o caminho técnico para cada evolução. O objetivo é demonstrar que as limitações conhecidas são decisões conscientes, não lacunas ignoradas.

Cada item segue o mesmo raciocínio dos ADRs: situação atual, limitação que justifica a evolução, caminho técnico e pré-requisitos para avançar.

---

## 2. Chave de idempotência como contrato do integrador

### Situação atual

A tabela `lancamentos` possui o campo `chave_idempotencia` com constraint `UNIQUE NOT NULL`. Na versão atual, o sistema aceita esse valor do cliente da API e o usa para garantir que a mesma operação não seja registrada mais de uma vez.

O índice é `UNIQUE(chave_idempotencia)` simples, sem escopo por comerciante.

### Limitação

Um índice `UNIQUE` simples na `chave_idempotencia` significa que dois comerciantes diferentes não podem usar o mesmo valor como chave, mesmo que a operação seja completamente distinta. Isso força o integrador a gerar chaves globalmente únicas (ex: UUID), quando na prática seria mais natural usar uma chave única apenas dentro do próprio escopo do comerciante.

### Caminho de evolução

Alterar o índice de `UNIQUE(chave_idempotencia)` para `UNIQUE(comerciante_id, chave_idempotencia)`. Isso libera o integrador para usar chaves semânticas dentro do seu próprio contexto, como o número de ordem do seu sistema de origem, sem risco de colisão com outros comerciantes.

Essa mudança requer:
- Acordo de contrato com os integradores existentes antes da migração
- Migration no schema `registros` para substituir o índice
- Atualização da documentação da API

### Por que não foi feito agora

A versão atual resolve o problema de duplicidade. A evolução do escopo da chave é um refinamento de contrato que requer alinhamento com os integradores antes de existir no código. Fazer agora, sem integradores reais, seria especular sobre um requisito ainda não confirmado.

---

## 3. Apache Kafka em substituição ao RabbitMQ

### Situação atual

A comunicação entre os domínios de Registros e Posição usa RabbitMQ com MassTransit. O broker garante entrega assíncrona, desacoplamento temporal e persistência de mensagens durante indisponibilidade do consumidor.

### Limitação

O RabbitMQ é projetado para roteamento de mensagens com entrega e consumo. Quando o volume de eventos cresce significativamente, surgem limitações que o Kafka resolve de forma nativa:

| Necessidade | RabbitMQ | Kafka |
|-------------|----------|-------|
| Replay histórico de eventos | Não suportado após consumo | Retenção configurável; replay por offset |
| Múltiplos consumidores independentes do mesmo evento | Requer fanout exchange | Consumer groups nativos |
| Throughput muito alto (centenas de milhares de eventos/min) | Limitado pela topologia de filas | Particionamento horizontal nativo |
| Ordenação garantida por chave | Não garantida | Garantida dentro de uma partição |

### Caminho de evolução

O MassTransit abstrai o broker: o código dos produtores e consumidores não muda. A migração envolve:
1. Provisionar o cluster Kafka
2. Atualizar a configuração do MassTransit nos serviços para usar o transporte Kafka
3. Migrar as filas existentes com período de operação paralela para garantir que nenhuma mensagem em trânsito seja perdida

### Pré-requisitos para avançar

- Volume de eventos acima da capacidade do RabbitMQ em cluster (indicador: latência de entrega crescente ou backpressure frequente)
- Necessidade real de replay histórico ou múltiplos consumidores independentes
- Equipe com capacidade operacional para manter um cluster Kafka

---

## 4. CDC com Debezium

### Situação atual

O relay da Transactional Outbox usa polling: um `BackgroundService` consulta periodicamente a tabela `outbox` em busca de registros com status `PENDENTE` e os publica no broker. A frequência do polling determina a latência de publicação.

### Limitação

O polling introduz latência mínima configurável e carga constante no banco, mesmo quando não há eventos novos. Em volumes muito altos, o ciclo de polling pode se tornar o gargalo da publicação.

### Caminho de evolução

Substituir o relay de polling por CDC com Debezium. O Debezium conecta ao WAL (Write-Ahead Log) do PostgreSQL e captura as inserções na tabela `outbox` em tempo quase real, publicando diretamente no Kafka. O relay de polling é eliminado por completo.

```
Situação atual:  INSERT outbox → polling periódico → PUBLISH broker
Com Debezium:    INSERT outbox → WAL → Debezium → PUBLISH Kafka
```

### Pré-requisitos para avançar

- Migração para Kafka (Debezium publica em tópicos Kafka)
- PostgreSQL configurado com `wal_level = logical`
- Equipe com capacidade operacional para manter o conector Debezium
- Latência de publicação atual sendo um problema mensurável

### Por que não foi feito agora

O overhead operacional do Debezium é desproporcional para o volume atual. O relay de polling resolve o problema com complexidade muito menor e sem dependência de configuração avançada do banco.

---

## 5. Redis Cluster

### Situação atual

O cache da Posição API usa um nó Redis único com circuit breaker. Em caso de indisponibilidade, o serviço faz fallback transparente para o banco.

### Limitação

Um nó Redis único tem limites de memória e throughput. Em cenários de volume muito alto, com muitos comerciantes e histórico extenso de consolidados em cache, o nó único pode se tornar o gargalo de leitura da Posição API.

### Caminho de evolução

Redis Cluster distribui as chaves entre múltiplos nós usando sharding por hash slot. Para o Solidus, a chave de cache `posicao:{comerciante_id}:{data}` distribui naturalmente entre os slots, sem necessidade de lógica de roteamento no código.

Em ambientes gerenciados, a migração é operacional: ElastiCache (AWS), Azure Cache for Redis Premium ou Memorystore (GCP) oferecem cluster sem alteração de código. O cliente Redis do .NET suporta cluster de forma transparente via configuração de connection string.

### Pré-requisitos para avançar

- Cache hit rate saudável (acima de 80%), indicando que o nó único está sendo bem utilizado
- Latência do Redis crescendo sob carga, indicando saturação do nó
- Volume de dados em cache próximo ao limite de memória configurado

---

## 6. Particionamento do PostgreSQL

### Situação atual

As tabelas `lancamentos` e `posicao_diaria` são tabelas regulares com índices `BTREE`. O isolamento entre comerciantes é garantido por filtros na camada de aplicação (`WHERE comerciante_id = ...`).

### Limitação

À medida que o volume de lançamentos cresce, as tabelas aumentam de tamanho. Consultas filtradas por `comerciante_id` continuam eficientes com os índices existentes, mas operações de manutenção (vacuum, reindex, backup) passam a operar sobre tabelas cada vez maiores.

### Caminho de evolução

**Particionamento nativo do PostgreSQL** por `comerciante_id` (hash partitioning): cada partição contém os dados de um subconjunto de comerciantes. O PostgreSQL roteia as queries automaticamente para a partição correta. Nenhuma alteração na camada de aplicação é necessária.

**Citus** é a alternativa para sharding horizontal em múltiplos nós: distribui as tabelas particionadas entre servidores físicos distintos, aumentando o throughput de escrita além do que um único servidor PostgreSQL suporta. Disponível como extensão self-managed ou como serviço gerenciado no Azure (Cosmos DB for PostgreSQL).

### Pré-requisitos para avançar

- Tamanho das tabelas impactando a performance de manutenção ou de queries específicas
- Throughput de escrita próximo ao limite do servidor atual
- Benchmark comparativo antes da migração para confirmar o ganho esperado

---

## 7. Service mesh para comunicação interna

### Situação atual

A comunicação entre os serviços internos usa credenciais de broker e connection strings fornecidas via variáveis de ambiente. Não há autenticação HTTP entre serviços porque não há chamadas HTTP entre serviços: toda comunicação interna passa pelo broker.

### Limitação

Em arquiteturas que evoluem para múltiplos serviços com comunicação HTTP entre si, a gestão manual de credenciais por variável de ambiente escala mal. mTLS manual exige rotação de certificados, distribuição segura e renovação periódica, tudo isso sem erros.

### Caminho de evolução

Um service mesh como Istio ou Linkerd injeta sidecars em cada pod e gerencia mTLS automaticamente: certificados são emitidos, rotacionados e validados pela malha sem intervenção manual. O código dos serviços não muda; a segurança da comunicação é responsabilidade da infraestrutura.

### Por que não foi feito agora

O Solidus tem exatamente dois serviços com APIs externas e um worker sem porta exposta. A comunicação interna é toda via broker. Um service mesh resolveria um problema que o sistema atual não tem. O custo operacional de manter Istio para esta topologia não teria retorno.

---

## 8. Expansão do modelo de lançamentos

### Situação atual

O domínio de Registros captura tipo, valor, data de competência e descrição livre. O domínio de Posição consolida créditos e débitos por dia.

### O que o modelo atual não suporta

| Capacidade | Impacto no domínio |
|------------|-------------------|
| Categorias de lançamento | Nova entidade no domínio de Registros; novo agrupamento no consolidado |
| Tags livres por lançamento | Estrutura flexível para classificação sem schema fixo |
| Lançamentos recorrentes | Lógica de geração automática; novo aggregate no domínio de Registros |
| Conciliação bancária | Novo bounded context; correlação entre lançamentos e extratos externos |
| Consolidado por categoria | Expansão do read model no domínio de Posição |

### Caminho de evolução

Cada item é uma expansão independente do domínio. A arquitetura atual suporta essa evolução sem redesenho: novos campos em `lancamentos`, novas entidades no domínio de Registros, novos eventos publicados no broker e novos read models no domínio de Posição. O contrato de API atual não é quebrado; novas capacidades são adicionadas de forma aditiva.

### Por que não foi feito agora

O escopo do sistema é controle de fluxo de caixa: registro de lançamentos e consolidado diário. As expansões listadas pertencem a estágios de maturidade do produto que pressupõem a base funcionando e adotada. Implementar categorias ou conciliação antes de ter o núcleo estável seria otimização prematura de produto.
