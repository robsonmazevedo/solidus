# Arquitetura Alvo — Código (C4 Nível 4)

## 1. Propósito

Este documento detalha o nível 4 do modelo C4 para o Solidus. O foco deixa de ser o componente lógico e passa a ser o código relevante à arquitetura: classes, interfaces, handlers, agregados, serviços de infraestrutura e fronteiras transacionais que implementam os fluxos principais do sistema.

O objetivo não é mapear todo tipo existente no repositório, mas sim tornar explícito como o comportamento arquitetural é realizado no código executável.

---

## 2. Escopo

Entram neste nível:

- Controllers, consumers e handlers que controlam fluxo
- Agregados e value objects que encapsulam regras de negócio
- Repositórios, unit of work e serviços de infraestrutura que sustentam garantias arquiteturais
- Pontos de observabilidade, autenticação e bootstrap que impactam o comportamento do sistema

Ficam fora deste nível:

- Detalhes de mapeamento do EF Core
- Arquivos gerados em `bin`, `obj` e `TestResults`
- Configurações internas de RabbitMQ, Redis e PostgreSQL
- Classes de teste e detalhes de framework sem impacto arquitetural direto

---

## 3. Registros API

### 3.1 Responsabilidade arquitetural

O `Registros API` implementa o caso de uso de escrita do sistema. Seu papel é validar a identidade do comerciante, registrar o lançamento, garantir idempotência por chave e persistir o evento de integração na outbox dentro da mesma transação.

### 3.2 Código relevante

```mermaid
%%{init: {"theme": "base", "themeVariables": {"background": "#fffdf8", "primaryColor": "#e7f0ea", "primaryTextColor": "#20302b", "primaryBorderColor": "#7aa38b", "lineColor": "#6c8a7a", "secondaryColor": "#f5efe2", "secondaryTextColor": "#3e3528", "secondaryBorderColor": "#c7ae7b", "tertiaryColor": "#edf4fb", "tertiaryTextColor": "#243447", "tertiaryBorderColor": "#88a9c3", "clusterBkg": "#f9f6ef", "clusterBorder": "#b9c7b0", "edgeLabelBackground": "#fffaf0", "fontFamily": "Georgia, Charter, serif", "fontSize": "15px"}}}%%
flowchart TD
    subgraph HTTP["Entrada HTTP"]
        LC["LancamentosController\nASP.NET Controller"]
    end

    subgraph APP["Application"]
        RC["RegistrarLancamentoCommand\nCommand"]
        RH["RegistrarLancamentoHandler\nMediatR Handler"]
    end

    subgraph DOM["Domain"]
        LA["Lancamento\nAggregate Root"]
        TL["TipoLancamento\nValue Object"]
        VO["Valor\nValue Object"]
    end

    subgraph INF["Infrastructure"]
        LR["ILancamentoRepository / LancamentoRepository"]
        OR["IOutboxRepository / OutboxRepository"]
        UOW["IUnitOfWork / UnitOfWork"]
        OE["OutboxEntry\nRegistro pendente"]
        OS["OutboxRelayService\nBackgroundService"]
        RM["RegistrosMetrics"]
        PE["IPublishEndpoint\nMassTransit"]
    end

    EXT1["PostgreSQL\nSchema registros"]
    EXT2["RabbitMQ\nExchange movimentacao-registrada"]

    LC -->|cria| RC
    RC --> RH
    RH -->|busca por chave idempotente| LR
    RH -->|cria| LA
    LA --> TL
    LA --> VO
    RH -->|adiciona| LR
    RH -->|cria e adiciona| OE
    RH --> OR
    RH -->|commit transacional| UOW
    RH --> RM
    LR --> EXT1
    OR --> EXT1
    UOW --> EXT1
    OS -->|busca pendentes| OR
    OS -->|marca publicados| OR
    OS -->|commit| UOW
    OS -->|publish| PE
    OS --> RM
    PE --> EXT2
```

### 3.3 Fluxo principal

1. `LancamentosController` extrai o claim `comerciante_id`, monta `RegistrarLancamentoCommand` e delega a execução ao MediatR.
2. `RegistrarLancamentoHandler` consulta `ILancamentoRepository.BuscarPorChaveIdempotenciaAsync` para resolver retries idempotentes.
3. Não havendo lançamento prévio, o handler chama `Lancamento.Registrar`, que aplica as invariantes de domínio com `TipoLancamento.Parse` e `Valor.Criar`.
4. O handler cria um `MovimentacaoRegistradaEvent`, serializa o payload e registra um `OutboxEntry`.
5. `ILancamentoRepository`, `IOutboxRepository` e `IUnitOfWork` garantem que lançamento e outbox sejam persistidos atomicamente.
6. `OutboxRelayService` executa polling periódico, busca pendências, publica no broker via `IPublishEndpoint` e marca os itens como publicados.

---

## 4. Posição Processor

### 4.1 Responsabilidade arquitetural

O `Posição Processor` projeta o evento de movimentação para o read model consolidado. Seu papel é consumir a mensagem, garantir idempotência por `EventoId`, aplicar a atualização no agregado de posição diária e persistir o registro de processamento na mesma transação.

### 4.2 Código relevante

```mermaid
%%{init: {"theme": "base", "themeVariables": {"background": "#fffdf8", "primaryColor": "#e8effa", "primaryTextColor": "#223248", "primaryBorderColor": "#86a6c7", "lineColor": "#68839e", "secondaryColor": "#f1f5eb", "secondaryTextColor": "#31412e", "secondaryBorderColor": "#97ae8b", "tertiaryColor": "#f8ede8", "tertiaryTextColor": "#4a3028", "tertiaryBorderColor": "#c99886", "clusterBkg": "#faf7f1", "clusterBorder": "#c1c9d1", "edgeLabelBackground": "#fffaf3", "fontFamily": "Georgia, Charter, serif", "fontSize": "15px"}}}%%
flowchart TD
    EXT["RabbitMQ\nMensagem MovimentacaoRegistradaEvent"]

    subgraph MSG["Entrada assíncrona"]
        MC["MovimentacaoRegistradaConsumer\nMassTransit Consumer"]
    end

    subgraph APP["Application"]
        PC["ProcessarMovimentacaoCommand\nCommand"]
        PH["ProcessarMovimentacaoHandler\nMediatR Handler"]
    end

    subgraph DOM["Domain"]
        PD["PosicaoDiaria\nAggregate Root"]
    end

    subgraph INF["Infrastructure"]
        PR["IPosicaoDiariaRepository / PosicaoDiariaRepository"]
        ER["IEventoProcessadoRepository / EventoProcessadoRepository"]
        EP["EventoProcessado\nRegistro de idempotencia"]
        UOW["IUnitOfWork / UnitOfWork"]
        PM["ProcessorMetrics"]
    end

    DB["PostgreSQL\nSchema posicao"]

    EXT --> MC
    MC -->|adapta mensagem para comando| PC
    PC --> PH
    PH -->|verifica duplicidade| ER
    PH -->|obter ou criar| PR
    PR --> PD
    PH -->|aplica movimentacao| PD
    PH -->|registra processamento| EP
    PH --> ER
    PH -->|commit transacional| UOW
    PH --> PM
    PR --> DB
    ER --> DB
    UOW --> DB
```

### 4.3 Fluxo principal

1. `MovimentacaoRegistradaConsumer` recebe `MovimentacaoRegistradaEvent` do RabbitMQ e o adapta para `ProcessarMovimentacaoCommand`.
2. `ProcessarMovimentacaoHandler` consulta `IEventoProcessadoRepository.ExisteAsync` para descartar reentregas do mesmo evento.
3. O handler usa `IPosicaoDiariaRepository.ObterOuCriarAsync` para recuperar ou inicializar a posição do comerciante na data da competência.
4. `PosicaoDiaria.AplicarMovimentacao` recalcula créditos, débitos e saldo.
5. O handler cria `EventoProcessado.Registrar` e persiste esse registro como prova de processamento.
6. `IUnitOfWork.CommitAsync` fecha a transação que grava a nova posição e a marcação de idempotência.

---

## 5. Posição API

### 5.1 Responsabilidade arquitetural

O `Posição API` expõe a leitura do consolidado diário. Seu papel é validar a consulta, aplicar isolamento por comerciante, tentar o cache distribuído primeiro e buscar o banco apenas em caso de `cache miss`.

### 5.2 Código relevante

```mermaid
%%{init: {"theme": "base", "themeVariables": {"background": "#fffdf8", "primaryColor": "#edf5fb", "primaryTextColor": "#22374a", "primaryBorderColor": "#7ea6c1", "lineColor": "#7290a7", "secondaryColor": "#eef4ea", "secondaryTextColor": "#31442c", "secondaryBorderColor": "#92ae86", "tertiaryColor": "#f8f1e5", "tertiaryTextColor": "#4b3c28", "tertiaryBorderColor": "#c9ab77", "clusterBkg": "#faf7f0", "clusterBorder": "#bfd0d8", "edgeLabelBackground": "#fffaf2", "fontFamily": "Georgia, Charter, serif", "fontSize": "15px"}}}%%
flowchart TD
    subgraph HTTP["Entrada HTTP"]
        PC["PosicaoController\nASP.NET Controller"]
    end

    subgraph APP["Application"]
        CQ["ConsultarPosicaoDiariaQuery\nQuery"]
        CH["ConsultarPosicaoDiariaHandler\nMediatR Handler"]
        DTO["PosicaoDiariaDto\nResposta da consulta"]
    end

    subgraph INF["Infrastructure"]
        CS["IPosicaoCacheService / PosicaoCacheService"]
        RR["IPosicaoDiariaReadRepository / PosicaoDiariaReadRepository"]
        PX["PosicaoMetrics"]
    end

    subgraph EXT["Dependencias externas"]
        RD["Redis\nCache distribuido"]
        DB["PostgreSQL\nSchema posicao"]
    end

    PC -->|cria| CQ
    CQ --> CH
    CH --> PX
    CH -->|obter async| CS
    CS --> RD
    CH -->|cache miss| RR
    RR --> DB
    CH -->|monta| DTO
    CH -->|grava async| CS
```

### 5.3 Fluxo principal

1. `PosicaoController` valida se a data não é futura, obtém `comerciante_id` do token e delega a execução ao MediatR.
2. `ConsultarPosicaoDiariaHandler` registra a consulta em `PosicaoMetrics`.
3. O handler chama `IPosicaoCacheService.ObterAsync`.
4. Em `cache hit`, retorna imediatamente o `PosicaoDiariaDto`.
5. Em `cache miss`, busca o dado em `IPosicaoDiariaReadRepository.ObterAsync`.
6. Se não houver consolidado persistido, retorna um DTO zerado.
7. Se houver dado, grava o DTO em Redis por meio de `IPosicaoCacheService.GravarAsync`.

---

## 6. Bootstrap Arquitetural Relevante

Os arquivos `Program.cs` dos três serviços participam do nível 4 apenas nos pontos que afetam comportamento arquitetural:

- Registro do MediatR como barramento in-process para commands e queries
- Configuração do MassTransit para publicação e consumo de `MovimentacaoRegistradaEvent`
- Registro de `OutboxRelayService` como `HostedService`
- Autenticação JWT e autorização nos dois serviços HTTP
- Rate limit por `comerciante_id`
- OpenTelemetry e Prometheus para métricas e tracing
- Health checks dos serviços e do banco

```mermaid
%%{init: {"theme": "base", "themeVariables": {"background": "#fffdf8", "primaryColor": "#ecf3ea", "primaryTextColor": "#25362e", "primaryBorderColor": "#86a58d", "lineColor": "#728978", "secondaryColor": "#edf3f8", "secondaryTextColor": "#293a49", "secondaryBorderColor": "#8ca8be", "tertiaryColor": "#f7efe5", "tertiaryTextColor": "#4a3726", "tertiaryBorderColor": "#c5a57e", "clusterBkg": "#faf7f0", "clusterBorder": "#bec9bc", "edgeLabelBackground": "#fffaf3", "fontFamily": "Georgia, Charter, serif", "fontSize": "15px"}}}%%
flowchart LR
    subgraph RegistrosAPI["Registros API Program.cs"]
        R1["AddMediatR"]
        R2["AddMassTransit publish"]
        R3["AddHostedService OutboxRelayService"]
        R4["AddAuthentication JWT"]
        R5["AddRateLimiter por-comerciante"]
        R6["AddOpenTelemetry + Prometheus"]
    end

    subgraph PosicaoAPI["Posicao API Program.cs"]
        P1["AddMediatR"]
        P2["Redis ConnectionMultiplexer"]
        P3["AddAuthentication JWT"]
        P4["AddRateLimiter por-comerciante"]
        P5["AddOpenTelemetry + Prometheus"]
    end

    subgraph Processor["Posicao Processor Program.cs"]
        W1["AddMediatR"]
        W2["AddMassTransit consumer"]
        W3["AddOpenTelemetry + Prometheus"]
        W4["DbContext + MigrateAsync"]
    end
```

---

## 7. Fluxo Consolidado Fim a Fim

```mermaid
%%{init: {"theme": "base", "themeVariables": {"background": "#fffdf8", "primaryColor": "#eef4fb", "primaryTextColor": "#26384a", "primaryBorderColor": "#85a7c2", "lineColor": "#708ca2", "secondaryColor": "#eef5eb", "secondaryTextColor": "#31422f", "secondaryBorderColor": "#94ad8a", "tertiaryColor": "#f8efe6", "tertiaryTextColor": "#4b3527", "tertiaryBorderColor": "#c5a183", "clusterBkg": "#fbf8f2", "clusterBorder": "#c2ccd2", "edgeLabelBackground": "#fffaf4", "fontFamily": "Georgia, Charter, serif", "fontSize": "15px", "actorBkg": "#e6efe4", "actorBorder": "#88a087", "actorTextColor": "#203128", "signalColor": "#6f8aa3", "signalTextColor": "#314052", "labelBoxBkgColor": "#fffaf4", "labelBoxBorderColor": "#d8c8aa"}}}%%
sequenceDiagram
    autonumber
    participant C as Comerciante
    participant RA as LancamentosController
    participant RH as RegistrarLancamentoHandler
    participant RDB as PostgreSQL registros
    participant ORS as OutboxRelayService
    participant MQ as RabbitMQ
    participant MC as MovimentacaoRegistradaConsumer
    participant PH as ProcessarMovimentacaoHandler
    participant PDB as PostgreSQL posicao
    participant PA as PosicaoController
    participant QH as ConsultarPosicaoDiariaHandler
    participant Redis as Redis

    C->>RA: POST /lancamentos
    RA->>RH: RegistrarLancamentoCommand
    RH->>RDB: grava lancamento + outbox
    RH-->>C: 201 Created ou 200 OK idempotente

    ORS->>RDB: busca pendentes
    ORS->>MQ: publica MovimentacaoRegistradaEvent
    ORS->>RDB: marca outbox como publicada

    MQ->>MC: entrega evento
    MC->>PH: ProcessarMovimentacaoCommand
    PH->>PDB: verifica EventoId
    PH->>PDB: atualiza posicao_diaria
    PH->>PDB: grava EventoProcessado

    C->>PA: GET /posicao/diaria?data=...
    PA->>QH: ConsultarPosicaoDiariaQuery
    QH->>Redis: obter cache
    alt cache hit
        Redis-->>QH: dto consolidado
    else cache miss
        QH->>PDB: ler posicao_diaria
        PDB-->>QH: consolidado ou vazio
        QH->>Redis: gravar dto com TTL
    end
    QH-->>C: 200 OK
```

