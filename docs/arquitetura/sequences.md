# Arquitetura Alvo — Diagramas de Sequência

## 1. Propósito

Os diagramas de sequência descrevem os fluxos principais do sistema, mostrando a ordem das interações entre os componentes para cada operação.

---

## 2. Fluxo — Registro de lançamento

```mermaid
sequenceDiagram
    actor Comerciante
    participant API as Registros API
    participant DB as PostgreSQL (registros)
    participant MQ as RabbitMQ
    participant Proc as Posição Processor
    participant DBPos as PostgreSQL (posicao)

    Comerciante->>API: POST /lancamentos

    rect rgb(235, 245, 255)
        Note over API,DB: Transação ACID
        API->>DB: INSERT lancamentos
        API->>DB: INSERT outbox (MovimentaçãoRegistrada)
    end

    API-->>Comerciante: 201 Created

    rect rgb(240, 255, 240)
        Note over API,MQ: Outbox relay — assíncrono
        API->>DB: SELECT outbox pendentes
        API->>MQ: PUBLISH MovimentaçãoRegistrada
        API->>DB: UPDATE outbox (publicado)
    end

    MQ->>Proc: DELIVER MovimentaçãoRegistrada

    rect rgb(255, 250, 235)
        Note over Proc,DBPos: Processamento idempotente
        Proc->>DBPos: SELECT eventos_processados (chave de idempotência)
        alt Evento já processado
            Proc-->>MQ: ACK
        else Evento novo
            Proc->>DBPos: UPDATE posicao_diaria
            Proc->>DBPos: INSERT eventos_processados
            Proc-->>MQ: ACK
        end
    end
```

### Pontos-chave

| Decisão | Justificativa |
|---------|--------------|
| Transactional Outbox | Garante que o lançamento e o evento sejam persistidos atomicamente. Sem risco de publicar sem persistir ou persistir sem publicar |
| Resposta 201 antes da publicação | O comerciante recebe a confirmação do registro imediatamente. A propagação ao domínio de Posição é assíncrona |
| Idempotência no Processor | O broker entrega ao menos uma vez (at-least-once). A verificação impede que o mesmo evento atualize o consolidado mais de uma vez |

---

## 3. Fluxo — Consulta de posição diária

```mermaid
sequenceDiagram
    actor Comerciante
    participant API as Posição API
    participant Cache as Redis
    participant DB as PostgreSQL (posicao)

    Comerciante->>API: GET /posicao/diaria?data={data}
    API->>Cache: GET posicao:{data}

    alt Cache hit
        Cache-->>API: posicao diaria
        API-->>Comerciante: 200 OK (posicao diaria)
    else Cache miss
        Cache-->>API: null
        API->>DB: SELECT posicao_diaria WHERE data = {data}
        DB-->>API: posicao diaria
        API->>Cache: SET posicao:{data}
        API-->>Comerciante: 200 OK (posicao diaria)
    end
```

### Pontos-chave

| Decisão | Justificativa |
|---------|--------------|
| Cache-Aside | A posição diária é imutável após consolidada. É um dado de leitura intensiva e altamente cacheável |
| Leitura direta no Redis | Atende o requisito de 50 req/s sem pressionar o banco de dados |
| Fallback para o banco | Garante que o dado esteja sempre disponível, mesmo que o cache esteja frio ou expirado |
| TTL como mecanismo de consistência | O cache do dia corrente usa TTL de 30 segundos; dias anteriores usam TTL de 1 hora. Quando o Posição Processor atualiza o consolidado no banco, o cache expira naturalmente dentro da janela de 60 segundos definida pelo RN-013, sem necessidade de invalidação explícita pelo Processor |
