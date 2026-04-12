# Arquitetura Alvo — Containers (C4 Nível 2)

## 1. Propósito

O diagrama de containers detalha o interior do sistema Solidus, mostrando os serviços, banco de dados, broker de mensagens e cache que o compõem, bem como as interações entre eles.

---

## 2. Diagrama

```mermaid
C4Container
    title Diagrama de Containers — Solidus

    Person(comerciante, "Comerciante", "Usuário do sistema")

    System_Boundary(solidus, "Solidus") {
        Container(registros_api, "Registros API", ".NET 10 Web API", "Registra lançamentos financeiros")
        Container(posicao_api, "Posição API", ".NET 10 Web API", "Disponibiliza o saldo diário")
        Container(posicao_processor, "Posição Processor", ".NET 10 Worker", "Processa eventos de movimentação")
        ContainerDb(postgres, "PostgreSQL", "Banco relacional", "Persiste lançamentos e consolidados")
        Container(rabbitmq, "RabbitMQ", "Message broker", "Transporta eventos entre domínios")
        ContainerDb(redis, "Redis", "Cache distribuído", "Armazena consolidados para leitura rápida")
    }

    Rel(comerciante, registros_api, "Registra lançamento", "HTTPS/JSON")
    Rel(comerciante, posicao_api, "Consulta posição diária", "HTTPS/JSON")
    Rel(registros_api, postgres, "Persiste lançamento e outbox", "TCP")
    Rel(registros_api, rabbitmq, "Publica MovimentaçãoRegistrada", "AMQP")
    Rel(rabbitmq, posicao_processor, "Entrega MovimentaçãoRegistrada", "AMQP")
    Rel(posicao_processor, postgres, "Atualiza consolidado diário", "TCP")
    Rel(posicao_api, redis, "Lê posição diária", "TCP")
    Rel(posicao_api, postgres, "Lê posição diária (cache miss)", "TCP")
```

---

## 3. Elementos

| Elemento | Tipo | Tecnologia | Descrição |
|----------|------|-----------|-----------|
| Comerciante | Pessoa | | Usuário que interage com o sistema via API |
| Registros API | Serviço | .NET 10 Web API | Ponto de entrada para registro de lançamentos. Persiste o lançamento e o evento de saída na mesma transação via Transactional Outbox |
| Posição API | Serviço | .NET 10 Web API | Ponto de entrada para consulta do saldo diário. Aplica Cache-Aside com Redis para baixa latência |
| Posição Processor | Serviço | .NET 10 Worker | Consome eventos do broker e atualiza o consolidado diário de forma assíncrona e independente |
| PostgreSQL | Banco de dados | Relacional | Schema `registros`: lançamentos, outbox e idempotência. Schema `posicao`: consolidado diário e eventos processados |
| RabbitMQ | Broker | AMQP | Desacopla os dois domínios. A indisponibilidade do domínio de Posição não afeta o domínio de Registros |
| Redis | Cache | In-memory | Armazena o consolidado por data para atender consultas com alta frequência sem pressionar o banco |
