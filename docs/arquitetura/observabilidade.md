# Monitoramento e Observabilidade

## 1. Premissa

Observabilidade não é um conjunto de ferramentas. É a capacidade de responder perguntas sobre o comportamento do sistema a partir dos dados que ele emite. O Solidus adota os três pilares clássicos, cada um com responsabilidade distinta:

| Pilar | Pergunta que responde | Ferramenta |
|-------|----------------------|------------|
| Métricas | O sistema está dentro dos SLAs? | Prometheus + Grafana |
| Logs | O que aconteceu nesta requisição? | Logs estruturados (JSON) + stdout |
| Traces | Onde está o gargalo neste fluxo? | OpenTelemetry + Jaeger |

Os três pilares são correlacionados pelo `trace_id`: o mesmo identificador presente no trace aparece no log, permitindo navegar do alerta no Grafana para o log da requisição e para o trace distribuído no Jaeger.

---

## 2. Métricas

### Instrumentação

Os três serviços expõem métricas via OpenTelemetry no formato Prometheus (`/metrics`). O Prometheus coleta em intervalo de 15 segundos. O Grafana consulta o Prometheus e exibe os dashboards provisionados automaticamente.

### Métricas por serviço

**Registros API**

| Métrica | Tipo | Descrição |
|---------|------|-----------|
| `registros_lancamentos_total` | Counter | Total de lançamentos registrados com sucesso |
| `registros_lancamentos_erro_total` | Counter | Total de lançamentos rejeitados por erro |
| `registros_http_duracao_segundos` | Histogram | Latência das requisições HTTP (p50, p95, p99) |
| `registros_outbox_pendentes` | Gauge | Eventos na outbox com status `PENDENTE` |
| `registros_outbox_idade_maxima_segundos` | Gauge | Tempo em segundos do evento pendente mais antigo na outbox |
| `registros_outbox_publicados_total` | Counter | Eventos publicados pelo relay com sucesso |

**Posição API**

| Métrica | Tipo | Descrição |
|---------|------|-----------|
| `posicao_consultas_total` | Counter | Total de consultas de posição diária |
| `posicao_http_duracao_segundos` | Histogram | Latência das requisições HTTP (p50, p95, p99) |
| `posicao_cache_hit_total` | Counter | Consultas respondidas pelo Redis |
| `posicao_cache_miss_total` | Counter | Consultas que caíram no banco por ausência de cache |

**Posição Processor**

| Métrica | Tipo | Descrição |
|---------|------|-----------|
| `processor_eventos_processados_total` | Counter | Eventos consumidos e processados com sucesso |
| `processor_eventos_duplicados_total` | Counter | Eventos ignorados por já terem sido processados |
| `processor_duracao_processamento_segundos` | Histogram | Tempo de processamento por evento |

**Infraestrutura**

| Métrica | Origem | Descrição |
|---------|--------|-----------|
| `rabbitmq_queue_messages` | RabbitMQ exporter | Mensagens aguardando consumo na fila |
| `rabbitmq_queue_messages_unacknowledged` | RabbitMQ exporter | Mensagens entregues mas ainda não confirmadas |
| `redis_keyspace_hits_total` | Redis exporter | Cache hits acumulados |
| `redis_keyspace_misses_total` | Redis exporter | Cache misses acumulados |

---

## 3. Logs

### Formato

Todos os serviços emitem logs em JSON estruturado para stdout. O agregador de logs (Fluent Bit, Loki ou equivalente) coleta e indexa por campo.

### Campos obrigatórios

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `timestamp` | ISO 8601 | Data e hora do evento |
| `nivel` | string | `DEBUG`, `INFO`, `WARN`, `ERROR` |
| `servico` | string | `registros-api`, `posicao-api`, `posicao-processor` |
| `trace_id` | uuid | Identificador do trace distribuído (OpenTelemetry) |
| `comerciante_id` | uuid | Identificador do comerciante da requisição |
| `mensagem` | string | Descrição legível do evento |
| `duracao_ms` | int | Duração da operação em milissegundos (quando aplicável) |

### Correlação com traces

O `trace_id` é propagado pelo OpenTelemetry em todos os serviços. Uma requisição `POST /lancamentos` gera o mesmo `trace_id` no log da Registros API, no evento publicado no broker e no log do Posição Processor ao processar o evento. Isso permite reconstruir o fluxo completo de um lançamento a partir de qualquer ponto.

---

## 4. Traces distribuídos

### Cobertura

O OpenTelemetry instrumenta automaticamente o ASP.NET Core, o EF Core e o MassTransit. Os spans gerados cobrem o fluxo completo de cada operação:

**Fluxo de registro de lançamento**

```
POST /lancamentos
  └── RegistrarLancamentoHandler
        ├── INSERT lancamentos (EF Core)
        ├── INSERT outbox (EF Core)
        └── PUBLISH MovimentaçãoRegistrada (MassTransit outbox relay)
              └── MovimentacaoRegistradaConsumer (Posição Processor)
                    ├── SELECT eventos_processados
                    ├── UPDATE posicao_diaria
                    └── INSERT eventos_processados
```

**Fluxo de consulta de posição diária**

```
GET /posicao/diaria
  └── ConsultarPosicaoDiariaHandler
        ├── GET Redis (cache hit → fim)
        └── SELECT posicao_diaria (cache miss)
              └── SET Redis
```

### Visualização

O Jaeger está disponível em `localhost:16686` no ambiente local. A busca por `trace_id` retorna o trace completo com todos os spans, latências por operação e atributos de cada span.

---

## 5. SLOs e alertas

Os SLOs são derivados diretamente dos RNFs. Cada SLO tem um limiar de alerta (warning) e um limiar crítico.

| SLO | Origem | Meta | Warning | Crítico |
|-----|--------|------|---------|---------|
| Disponibilidade da Registros API | RNF-002 | 99,9% | Indisponibilidade > 30s | Indisponibilidade > 2min |
| Disponibilidade da Posição API | RNF-003 | 99,5% | Indisponibilidade > 2min | Indisponibilidade > 10min |
| Taxa de erro das requisições | RNF-005 | ≤ 5% | Erro > 3% por 1min | Erro > 5% por 1min |
| Latência p99 da Registros API | RNF-006 | ≤ 600ms | p99 > 400ms por 2min | p99 > 600ms por 2min |
| Latência p99 da Posição API | RNF-007 | ≤ 400ms | p99 > 300ms por 2min | p99 > 400ms por 2min |
| Defasagem do consolidado | RN-013, RNF-009 | ≤ 60s | Evento mais antigo na outbox > 30s | Evento mais antigo na outbox > 55s |
| Fila do broker | RNF-008 | DLQ = 0 | — | DLQ > 0 |
| Cache hit rate | Operacional | ≥ 80% | Hit rate < 80% por 5min | Hit rate < 60% por 5min |

> **RNF-004 (throughput ≥ 50 req/s)** não é monitorado por alerta contínuo. A meta de throughput é validada pelos testes de carga k6 executados em pipeline de CI. Os resultados são documentados em `docs/testes/carga.md`.

### Ações esperadas por alerta

| Alerta | Severidade | Ação |
|--------|-----------|------|
| Registros API indisponível | Crítico | Verificar health check; reiniciar instância; escalar se necessário |
| Posição API indisponível | Warning | Verificar health check; reiniciar instância; período tolerado pelo RNF-003 (99,5%) |
| Taxa de erro > 5% | Crítico | Verificar logs de erro; identificar se é falha de validação ou infraestrutura |
| Latência p99 da Posição API > 400ms | Crítico | Verificar cache hit rate; verificar latência do banco; considerar escala horizontal |
| Latência p99 da Registros API > 600ms | Crítico | Verificar latência do banco; verificar se outbox está represando requisições; considerar escala horizontal |
| Evento mais antigo na outbox > 55s | Crítico | Verificar relay e conectividade com o broker; verificar logs do Posição Processor |
| DLQ > 0 | Crítico | Inspecionar mensagem na DLQ; verificar logs do Processor; corrigir e reprocessar |
| Cache hit rate < 60% | Warning | Verificar TTL; avaliar aquecimento do cache; verificar disponibilidade do Redis |

---

## 6. Dashboards

Os dashboards são provisionados automaticamente via `infra/grafana/provisioning/` no `docker compose up`. Nenhuma configuração manual é necessária.

### Dashboard 1 — Registros

| Painel | O que mede | Visualização |
|--------|------------|--------------|
| Throughput | Taxa de lançamentos registrados com sucesso por segundo | Gráfico de linha |
| Taxa de erro | Percentual de lançamentos rejeitados em relação ao total | Gráfico de linha com limiar em 5% |
| Latência p99 | Tempo de resposta no percentil 99 das requisições HTTP | Gráfico de linha com limiar em 600ms |
| Outbox pendentes | Quantidade de eventos aguardando publicação na outbox | Gauge com limiar em 100 |
| Eventos publicados | Taxa de eventos publicados com sucesso pelo relay por segundo | Gráfico de linha |

### Dashboard 2 — Posição

| Painel | O que mede | Visualização |
|--------|------------|--------------|
| Throughput | Taxa de consultas de posição diária por segundo | Gráfico de linha |
| Latência p99 | Tempo de resposta no percentil 99 das requisições HTTP | Gráfico de linha com limiar em 400ms |
| Cache hit rate | Proporção de consultas respondidas pelo Redis em relação ao total | Gauge percentual |
| Fila RabbitMQ | Quantidade de mensagens aguardando consumo na fila principal | Gráfico de linha |
| Eventos processados | Taxa de eventos consumidos e processados com sucesso por segundo | Gráfico de linha |

---

## 7. O que este documento não cobre

| Tema | Justificativa | Caminho de evolução |
|------|--------------|---------------------|
| Agregação e retenção de logs de longo prazo | Fora do escopo do sistema em si; responsabilidade da plataforma | ELK Stack ou Azure Monitor Logs |
| APM de terceiros | Substituível por OpenTelemetry + Jaeger sem perda funcional | Datadog, New Relic ou Dynatrace se o time já tiver contrato |
| Alertas via PagerDuty ou OpsGenie | Integração de canal, não de estratégia | Grafana suporta integração nativa com ambos |
| Métricas de negócio avançadas | Volume por comerciante, ticket médio, sazonalidade | BI separado ou Grafana com datasource adicional |
