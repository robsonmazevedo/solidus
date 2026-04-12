# Arquitetura Alvo — Componentes (C4 Nível 3)

## 1. Propósito

O diagrama de componentes detalha o interior de cada serviço, mostrando como a Clean Architecture organiza as responsabilidades em camadas: API, Application, Domain e Infrastructure.

---

## 2. Registros API

```mermaid
C4Component
    title Componentes — Registros API

    Container_Boundary(registros_api, "Registros API") {
        Component(controller, "LancamentosController", "ASP.NET Controller", "Recebe requisições HTTP")
        Component(handler, "RegistrarLancamentoHandler", "MediatR Handler", "Orquestra o registro")
        Component(lancamento, "Lancamento", "Aggregate Root", "Regras de negócio")
        Component(valor, "Valor", "Value Object", "Valor monetário")
        Component(tipo, "TipoLancamento", "Value Object", "Crédito ou débito")
        Component(lancamento_repo, "LancamentoRepository", "EF Core", "Persiste lançamentos")
        Component(outbox_repo, "OutboxRepository", "EF Core", "Persiste eventos outbox")
    }

    ContainerDb_Ext(postgres, "PostgreSQL", "Banco relacional", "Schema registros")

    Rel(controller, handler, "Envia comando", "MediatR")
    Rel(handler, lancamento, "Cria e valida")
    Rel(lancamento, valor, "Usa")
    Rel(lancamento, tipo, "Usa")
    Rel(handler, lancamento_repo, "Persiste lançamento")
    Rel(handler, outbox_repo, "Persiste evento outbox")
    Rel(lancamento_repo, postgres, "SQL")
    Rel(outbox_repo, postgres, "SQL")
```

### Elementos

| Componente | Camada | Descrição |
|------------|--------|-----------|
| LancamentosController | API | Recebe e valida a requisição HTTP. Delega ao handler via MediatR |
| RegistrarLancamentoHandler | Application | Orquestra o caso de uso: cria o agregado, persiste e grava o evento de saída na mesma transação |
| Lancamento | Domain | Aggregate root. Contém as regras de negócio do lançamento financeiro |
| Valor | Domain | Value object que encapsula o valor monetário e garante que seja positivo |
| TipoLancamento | Domain | Value object que representa crédito ou débito |
| LancamentoRepository | Infrastructure | Persiste e recupera lançamentos no banco de dados |
| OutboxRepository | Infrastructure | Persiste eventos de saída na mesma transação do lançamento |

---

## 3. Posição API

```mermaid
C4Component
    title Componentes — Posição API

    Container_Boundary(posicao_api, "Posição API") {
        Component(controller, "PosicaoController", "ASP.NET Controller", "Recebe requisições HTTP")
        Component(handler, "ConsultarPosicaoDiariaHandler", "MediatR Handler", "Orquestra a consulta")
        Component(cache, "PosicaoCacheService", "Redis", "Lê e grava no cache")
        Component(repo, "PosicaoDiariaRepository", "EF Core", "Lê consolidado do banco")
    }

    ContainerDb_Ext(redis, "Redis", "Cache distribuído", "Consolidados por data")
    ContainerDb_Ext(postgres, "PostgreSQL", "Banco relacional", "Schema posicao")

    Rel(controller, handler, "Envia query", "MediatR")
    Rel(handler, cache, "Consulta cache")
    Rel(handler, repo, "Consulta banco (cache miss)")
    Rel(cache, redis, "TCP")
    Rel(repo, postgres, "SQL")
```

### Elementos

| Componente | Camada | Descrição |
|------------|--------|-----------|
| PosicaoController | API | Recebe a requisição HTTP e delega ao handler via MediatR |
| ConsultarPosicaoDiariaHandler | Application | Aplica Cache-Aside: consulta o cache primeiro; em caso de miss, consulta o banco e atualiza o cache |
| PosicaoCacheService | Infrastructure | Abstração sobre o Redis para leitura e escrita do consolidado diário |
| PosicaoDiariaRepository | Infrastructure | Lê o consolidado diário diretamente do banco quando o cache não tem o dado |

---

## 4. Posição Processor

```mermaid
C4Component
    title Componentes — Posição Processor

    Container_Boundary(posicao_processor, "Posição Processor") {
        Component(consumer, "MovimentacaoRegistradaConsumer", "MassTransit Consumer", "Consome evento")
        Component(handler, "ProcessarMovimentacaoHandler", "MediatR Handler", "Orquestra o processamento")
        Component(posicao_diaria, "PosicaoDiaria", "Aggregate Root", "Regras do consolidado")
        Component(repo, "PosicaoDiariaRepository", "EF Core", "Persiste consolidado")
    }

    Container_Ext(rabbitmq, "RabbitMQ", "Message broker", "Fonte dos eventos")
    ContainerDb_Ext(postgres, "PostgreSQL", "Banco relacional", "Schema posicao")

    Rel(rabbitmq, consumer, "MovimentaçãoRegistrada", "AMQP")
    Rel(consumer, handler, "Envia comando", "MediatR")
    Rel(handler, posicao_diaria, "Atualiza consolidado")
    Rel(handler, repo, "Persiste consolidado")
    Rel(repo, postgres, "SQL")
```

### Elementos

| Componente | Camada | Descrição |
|------------|--------|-----------|
| MovimentacaoRegistradaConsumer | Infrastructure | Recebe o evento do broker e garante idempotência antes de processar |
| ProcessarMovimentacaoHandler | Application | Orquestra a atualização do consolidado diário a partir do evento recebido |
| PosicaoDiaria | Domain | Aggregate root que contém as regras de cálculo do saldo diário |
| PosicaoDiariaRepository | Infrastructure | Persiste e recupera o consolidado diário no banco de dados |
