# Critérios de Segurança

## 1. Premissa

Este documento define os critérios de segurança para o consumo e a integração dos serviços do Solidus. O escopo cobre proteção das APIs para consumo externo, isolamento de dados entre comerciantes e comunicação entre serviços internos.

Fora do escopo deste documento:

| Tema | Justificativa |
|------|--------------|
| Gestão de identidade do comerciante (IAM) | Responsabilidade do sistema de autenticação externo que emite os tokens |
| Emissão e rotação de certificados TLS | Responsabilidade da plataforma de infraestrutura |
| Conformidade regulatória (LGPD, PCI-DSS) | Requer análise jurídica e de compliance além do escopo arquitetural |

---

## 2. Autenticação

Todas as requisições às APIs externas exigem um token JWT válido no header `Authorization: Bearer {token}`.

### Campos obrigatórios no token

| Campo | Tipo | Descrição |
|-------|------|-----------|
| `sub` | string | Identificador único do comerciante no sistema de autenticação |
| `comerciante_id` | uuid | Identificador do comerciante no domínio do Solidus |
| `exp` | timestamp | Data de expiração do token |

### Regras de validação

| Regra | Comportamento em caso de falha |
|-------|-------------------------------|
| Token ausente | `401 Unauthorized` |
| Assinatura inválida | `401 Unauthorized` |
| Token expirado | `401 Unauthorized` |
| Campo `comerciante_id` ausente no payload | `401 Unauthorized` |

A validação é executada por middleware antes de qualquer handler de requisição. Nenhum endpoint é acessível sem autenticação válida, atendendo ao RNF-012.

---

## 3. Autorização

O `comerciante_id` é extraído exclusivamente do token JWT validado. Nenhum endpoint aceita `comerciante_id` como parâmetro de entrada na requisição.

```
Token JWT → middleware extrai comerciante_id → handler recebe comerciante_id como identidade resolvida
```

Essa decisão elimina uma classe inteira de vulnerabilidades: não é possível forjar a identidade passando um `comerciante_id` diferente no body ou na query string, pois o sistema ignora qualquer valor externo e usa apenas o que está no token assinado.

---

## 4. Isolamento de dados

O RNF-011 é explícito: nenhum comerciante pode acessar dados de outro. O isolamento é implementado na camada de Application, não na camada de API.

### Como funciona

Cada handler recebe o `comerciante_id` resolvido pelo middleware e o aplica como filtro obrigatório em todas as queries ao banco de dados:

| Operação | Filtro aplicado |
|----------|----------------|
| `POST /lancamentos` | `comerciante_id` do token é gravado no lançamento |
| `GET /posicao/diaria` | `WHERE comerciante_id = {id do token}` |
| Consultas internas do Processor | Filtradas pelo `comerciante_id` presente no payload do evento |

### Por que na camada de Application e não na API

Aplicar o filtro no handler garante que o isolamento funcione independente de como a requisição chegou. Um controller que esquecesse de repassar o filtro seria um bug silencioso. Um handler que usa diretamente o `comerciante_id` resolvido é a única fonte de verdade.

---

## 5. Comunicação interna entre serviços

O Posição Processor não expõe nenhuma porta HTTP. Ele consome exclusivamente do broker RabbitMQ, que trafega dentro da rede privada do cluster. Não há autenticação HTTP entre serviços internos porque não há chamadas HTTP entre serviços internos.

| Canal | Autenticação | Justificativa |
|-------|-------------|--------------|
| Comerciante → Registros API | JWT Bearer | Tráfego externo |
| Comerciante → Posição API | JWT Bearer | Tráfego externo |
| Registros API → RabbitMQ | Credenciais do broker | Tráfego interno, rede privada |
| RabbitMQ → Posição Processor | Credenciais do broker | Tráfego interno, rede privada |
| Posição Processor → PostgreSQL | Connection string | Tráfego interno, rede privada |
| Registros API → PostgreSQL (Registros) | Connection string | Tráfego interno, rede privada |
| Posição API → PostgreSQL (Posição) | Connection string | Tráfego interno, rede privada |
| Posição API → Redis | Connection string | Tráfego interno, rede privada |

As credenciais do broker e as connection strings são fornecidas via variáveis de ambiente, nunca embutidas no código.

---

## 6. Proteção contra abuso

### Rate limiting

Rate limiting aplicado por `comerciante_id` nas duas APIs. Um comerciante que exceda o limite recebe `429 Too Many Requests`. O limite não afeta outros comerciantes.

| API | Limite | Janela |
|-----|--------|--------|
| Registros API | 100 requisições | 1 minuto por comerciante |
| Posição API | 200 requisições | 1 minuto por comerciante |

### HTTPS

Todas as comunicações externas exigem HTTPS. Requisições HTTP são redirecionadas para HTTPS ou rejeitadas, conforme configuração da plataforma.

### Headers de segurança

| Header | Valor | Objetivo |
|--------|-------|----------|
| `X-Content-Type-Options` | `nosniff` | Impede interpretação incorreta do content-type |
| `X-Frame-Options` | `DENY` | Impede embedding em iframes |
| `Strict-Transport-Security` | `max-age=31536000` | Força HTTPS por um ano |

---

## 7. Variáveis sensíveis

Nenhuma credencial é embutida no código ou nos arquivos de configuração versionados. Todas as informações sensíveis são fornecidas via variáveis de ambiente.

| Variável | Descrição |
|----------|-----------|
| `REGISTROS_DB_CONNECTION` | Connection string do PostgreSQL do domínio de Registros |
| `POSICAO_DB_CONNECTION` | Connection string do PostgreSQL do domínio de Posição |
| `RABBITMQ_CONNECTION` | String de conexão com credenciais do RabbitMQ |
| `REDIS_CONNECTION` | String de conexão com o Redis |
| `JWT_SECRET` | Chave para validação da assinatura dos tokens JWT |
| `JWT_ISSUER` | Emissor esperado nos tokens recebidos |

O repositório contém um arquivo `config/.env.example` com todas as variáveis necessárias e valores fictícios como referência. O arquivo `config/.env` real está no `.gitignore` e nunca é versionado.
