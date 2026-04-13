# Requisitos Funcionais

## 1. Objetivo

Este documento especifica as capacidades funcionais do sistema Solidus, as regras de negócio que governam cada capacidade e os cenários de exceção relevantes.

---

## 2. Atores

| Ator | Descrição |
|------|-----------|
| Comerciante | Usuário do sistema. Registra movimentações financeiras e consulta o saldo consolidado do seu negócio. |

---

## 3. Capacidades Funcionais

### RF-001 Registrar movimentação financeira

O sistema deve permitir que o comerciante registre uma movimentação financeira informando o tipo, o valor, a data de competência e uma descrição opcional.

#### Regras de negócio

| Código | Descrição |
|--------|-----------|
| RN-001 | O tipo da movimentação deve ser crédito ou débito. Nenhum outro valor é aceito. |
| RN-002 | O valor da movimentação deve ser maior que zero. |
| RN-003 | O valor é sempre informado como positivo. O tipo determina a direção da movimentação. |
| RN-004 | A data de competência não pode ser futura. Lançamentos com data retroativa são permitidos. |
| RN-005 | O sistema não deve registrar a mesma movimentação mais de uma vez. O comerciante deve fornecer uma chave de idempotência por operação para garantir esse controle. |
| RN-006 | Quando o sistema recebe uma operação com chave de idempotência já registrada, deve retornar o registro original sem criar duplicidade. |
| RN-007 | O registro da movimentação e a notificação dos demais módulos do sistema devem ocorrer de forma atômica. Não é aceitável registrar sem notificar, nem notificar sem registrar. |
| RN-008 | O comerciante tem acesso exclusivamente às suas próprias movimentações. |

#### Dados da movimentação

| Campo | Obrigatoriedade | Regras |
|-------|----------------|--------|
| Chave de idempotência | Obrigatório | Única por comerciante |
| Tipo | Obrigatório | Crédito ou débito |
| Valor | Obrigatório | Maior que zero; até duas casas decimais |
| Data de competência | Obrigatório | Não futura |
| Descrição | Opcional | Máximo 255 caracteres |

#### Cenários de exceção

| Cenário | Comportamento esperado |
|---------|------------------------|
| Valor igual a zero | Operação rejeitada |
| Valor negativo | Operação rejeitada |
| Data de competência futura | Operação rejeitada |
| Chave de idempotência já registrada | Retorna o registro original sem duplicação |
| Indisponibilidade do módulo de consolidado | Não afeta o registro da movimentação |

---

### RF-002 Consultar saldo consolidado diário

O sistema deve disponibilizar ao comerciante o saldo financeiro consolidado de um dia específico, contendo o total de créditos, o total de débitos e o saldo resultante.

#### Regras de negócio

| Código | Descrição |
|--------|-----------|
| RN-009 | O consolidado deve considerar todas as movimentações do comerciante registradas na data informada. |
| RN-010 | O saldo é calculado pela fórmula: saldo = total de créditos - total de débitos. |
| RN-011 | O saldo pode ser negativo. Essa é uma informação válida de negócio. |
| RN-012 | Quando não há movimentações na data informada, o sistema deve retornar saldo zero. Ausência de lançamentos não é uma condição de erro. |
| RN-013 | O consolidado pode apresentar defasagem de até 60 segundos em relação ao último lançamento registrado. |
| RN-014 | O comerciante tem acesso exclusivamente ao consolidado das suas próprias movimentações. |

#### Dados da consulta

| Campo | Obrigatoriedade | Regras |
|-------|----------------|--------|
| Data | Obrigatório | Não futura |

#### Dados da resposta

| Campo | Descrição |
|-------|-----------|
| Data | Data consultada |
| Total de créditos | Soma de todas as movimentações de crédito da data |
| Total de débitos | Soma de todas as movimentações de débito da data |
| Saldo | Total de créditos menos total de débitos |
| Atualizado em | Momento da última atualização do consolidado |

#### Cenários de exceção

| Cenário | Comportamento esperado |
|---------|------------------------|
| Data futura | Operação rejeitada |
| Data sem movimentações | Retorna saldo zero |
| Indisponibilidade do módulo de registro | Não afeta a consulta do consolidado |

---

## 4. Rastreabilidade

| Requisito de negócio | Capacidade funcional |
|----------------------|----------------------|
| Serviço de controle de lançamentos | RF-001 |
| Serviço do consolidado diário | RF-002 |
| Registros independente da disponibilidade do consolidado | RN-007, cenários de exceção do RF-001 |
