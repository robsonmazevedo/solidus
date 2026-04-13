# Requisitos Não Funcionais

## 1. Objetivo

Este documento especifica os requisitos de qualidade do sistema Solidus, transformando necessidades de negócio em metas mensuráveis com critérios de aceite definidos.

---

## 2. Disponibilidade

### RNF-001 Independência entre os domínios de registro e consolidado

O serviço de registro de movimentações financeiras deve permanecer disponível e operacional independentemente do estado do serviço de consolidado diário.

| Atributo | Valor |
|----------|-------|
| Métrica | Taxa de disponibilidade do serviço de registro medida de forma independente |
| Meta | O serviço de registro deve aceitar e confirmar lançamentos mesmo quando o serviço de consolidado estiver completamente indisponível |
| Critério de aceite | Com o serviço de consolidado parado, 100% das requisições de registro devem ser confirmadas com sucesso |

### RNF-002 Disponibilidade do serviço de registro

| Atributo | Valor |
|----------|-------|
| Métrica | Percentual de tempo em que o serviço responde requisições com sucesso |
| Meta | 99,9% de disponibilidade mensal |
| Critério de aceite | No máximo 43 minutos de indisponibilidade por mês |

### RNF-003 Disponibilidade do serviço de consolidado

| Atributo | Valor |
|----------|-------|
| Métrica | Percentual de tempo em que o serviço responde requisições com sucesso |
| Meta | 99,5% de disponibilidade mensal |
| Critério de aceite | No máximo 3,6 horas de indisponibilidade por mês. Períodos de indisponibilidade não devem resultar em perda de dados |

---

## 3. Desempenho

### RNF-004 Throughput do serviço de consolidado

| Atributo | Valor |
|----------|-------|
| Métrica | Número de requisições processadas por segundo |
| Meta | Mínimo de 50 requisições por segundo em condições de pico |
| Critério de aceite | Sob carga de 50 req/s sustentada por 5 minutos, o sistema deve processar com taxa de erro inferior a 5% |

### RNF-005 Taxa de perda de requisições

| Atributo | Valor |
|----------|-------|
| Métrica | Percentual de requisições que não obtêm resposta bem-sucedida |
| Meta | No máximo 5% de perda em condições de pico |
| Critério de aceite | Em teste de carga com 50 req/s, no máximo 150 requisições em cada bloco de 3.000 podem falhar |

### RNF-006 Latência do serviço de registro

| Atributo | Valor |
|----------|-------|
| Métrica | Tempo de resposta por requisição |
| Meta | p95 abaixo de 300ms; p99 abaixo de 600ms |
| Critério de aceite | Em teste de carga, 95% das requisições respondem em até 300ms e 99% em até 600ms |

### RNF-007 Latência do serviço de consolidado

| Atributo | Valor |
|----------|-------|
| Métrica | Tempo de resposta por requisição |
| Meta | p95 abaixo de 200ms; p99 abaixo de 400ms |
| Critério de aceite | Em teste de carga, 95% das requisições respondem em até 200ms e 99% em até 400ms |

---

## 4. Confiabilidade

### RNF-008 Garantia de entrega das movimentações

Nenhuma movimentação confirmada ao comerciante pode ser perdida, independentemente de falhas em componentes internos do sistema após a confirmação.

| Atributo | Valor |
|----------|-------|
| Métrica | Percentual de movimentações confirmadas que chegam ao consolidado |
| Meta | 100% das movimentações confirmadas devem ser refletidas no consolidado |
| Critério de aceite | Após restabelecimento de qualquer componente, todas as movimentações confirmadas durante o período de falha devem aparecer no consolidado |

### RNF-009 Consistência eventual do consolidado

O consolidado diário pode apresentar defasagem em relação aos últimos lançamentos registrados, desde que dentro de uma janela de tempo aceitável.

| Atributo | Valor |
|----------|-------|
| Métrica | Tempo entre o registro de uma movimentação e sua reflexão no consolidado |
| Meta | No máximo 60 segundos em condições normais de operação |
| Critério de aceite | 99% das movimentações registradas aparecem no consolidado em até 60 segundos |

---

## 5. Escalabilidade

### RNF-010 Crescimento horizontal

O sistema deve suportar aumento de carga por adição de instâncias, sem alteração na lógica de negócio ou na estrutura de dados.

| Atributo | Valor |
|----------|-------|
| Métrica | Throughput proporcional ao número de instâncias ativas |
| Meta | A adição de uma instância deve aumentar a capacidade de processamento de forma proporcional |
| Critério de aceite | Com o dobro de instâncias, o sistema deve suportar o dobro de requisições mantendo os SLAs de latência |

---

## 6. Segurança

### RNF-011 Isolamento de dados por comerciante

Nenhum comerciante pode acessar, visualizar ou modificar dados de outro comerciante.

| Atributo | Valor |
|----------|-------|
| Métrica | Ausência de vazamento de dados entre comerciantes |
| Meta | Zero ocorrências de acesso cruzado entre comerciantes |
| Critério de aceite | Testes de isolamento confirmam que requisições autenticadas como comerciante A não retornam dados do comerciante B |

### RNF-012 Controle de acesso

Todas as operações do sistema exigem autenticação prévia do comerciante.

| Atributo | Valor |
|----------|-------|
| Métrica | Percentual de operações que validam a identidade do solicitante |
| Meta | 100% das operações requerem autenticação válida |
| Critério de aceite | Requisições sem autenticação ou com autenticação inválida são rejeitadas em 100% dos casos |

---

## 7. Rastreabilidade

| Requisito de negócio | RNF que atende |
|----------------------|----------------|
| Registros não pode cair se consolidado cair | RNF-001 |
| 50 req/s com no máximo 5% de perda | RNF-004, RNF-005 |
