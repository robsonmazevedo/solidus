# ADR-004 — Redis como cache distribuído para a posição diária

## Contexto

A API de Posição precisa atender 50 req/s com baixa latência. O read model resolve o problema de desacoplamento entre domínios, mas consultar o banco a cada requisição ainda pode se tornar um gargalo em cenários de alta concorrência com múltiplas instâncias do serviço.

---

## Problema de negócio

O consolidado de um dia passado é imutável: após a meia-noite, nenhum novo lançamento pode ser registrado com aquela data de competência (RN-004 proíbe datas futuras). Esse perfil de dado, lido com alta frequência e raramente alterado, é ideal para cache. A questão é qual estratégia de cache adotar em um serviço que escala horizontalmente.

---

## Opções consideradas

**Opção 1: Cache em memória por instância (IMemoryCache)**
Cada instância do serviço mantém seu próprio cache em memória. Sem infraestrutura adicional.

**Opção 2: Sem cache, leitura direta no banco**
Cada requisição consulta a tabela `posicao_diaria` diretamente, sem camada de cache.

**Opção 3: Cache distribuído com Redis**
Um servidor Redis centralizado armazena os consolidados em memória. Todas as instâncias do serviço consultam e atualizam o mesmo cache.

---

## Decisão

**Opção 3: cache distribuído com Redis**

---

## Justificativa

O cache em memória (Opção 1) é problemático em um serviço escalado horizontalmente. Com múltiplas instâncias, cada uma teria seu próprio cache isolado. Uma atualização do consolidado invalida o cache de uma instância, mas as demais continuam servindo o valor antigo. O comportamento é inconsistente entre instâncias e invisível para o cliente.

A leitura direta no banco (Opção 2) não sustenta 50 req/s com a latência esperada quando há múltiplas consultas concorrentes, especialmente considerando que o dado raramente muda.

O Redis resolve os dois problemas: cache compartilhado entre todas as instâncias com latência de leitura em sub-milissegundo. O padrão Cache-Aside garante que, em caso de indisponibilidade do Redis, a API faz fallback transparente para o banco, mantendo a disponibilidade do serviço.

---

## Prós e contras

| Opção | Prós | Contras |
|-------|------|---------|
| Cache em memória | Zero infraestrutura adicional; latência mínima | Inconsistência entre instâncias; perdido no restart |
| Sem cache | Simplicidade; sempre consistente | Latência maior; carga no banco cresce linearmente com o tráfego |
| **Redis** | **Cache compartilhado entre instâncias; TTL configurável; fallback possível** | **Componente adicional na infraestrutura; ponto de falha adicional** |

---

## Limitações

- O Redis se torna um ponto de falha adicional. A implementação do padrão Cache-Aside no `ConsultarPosicaoDiariaHandler` garante fallback para o banco em caso de indisponibilidade, mas com latência maior.
- O TTL do cache precisa ser calibrado. Um TTL muito curto reduz o benefício do cache. Um TTL muito longo pode servir dados defasados além do limite de 60 segundos do RN-013 para o consolidado do dia corrente.

---

## Quando evoluir

Se o Redis se tornar um gargalo em cenários de volume extremamente alto, avaliar Redis Cluster para distribuição horizontal das chaves. Em ambientes AWS, o ElastiCache é a alternativa gerenciada equivalente, eliminando a operação do cluster.
