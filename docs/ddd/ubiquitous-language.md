# Linguagem Ubíqua

## 1. Visão geral

Este glossário reúne os termos que compõem a linguagem ubíqua do sistema Solidus. Cada termo possui um significado preciso dentro do contexto em que aparece e deve ser usado de forma consistente em conversas, documentos, código e testes. Quando o mesmo termo aparece em contextos diferentes com significados distintos, isso é explicitado.

---

## 2. Bounded Context: Registros

| Termo | Definição | O que não é |
|-------|-----------|-------------|
| Movimentação | Entrada ou saída financeira informada pelo comerciante, ainda sujeita a validação | Não é um registro confirmado; pode ser rejeitada se violar regras de negócio |
| Lançamento | Movimentação que passou por todas as validações e foi registrada com sucesso no sistema | Não é sinônimo de movimentação; representa apenas o que foi aceito |
| Crédito | Lançamento que representa entrada de valor para o comerciante | Não é um saldo positivo; é apenas a classificação do tipo do lançamento |
| Débito | Lançamento que representa saída de valor para o comerciante | Não é um saldo negativo; é apenas a classificação do tipo do lançamento |
| Data de competência | Data à qual o lançamento pertence, informada pelo comerciante no momento do registro | Não é a data em que o sistema processou o lançamento |
| Chave de idempotência | Identificador informado pelo comerciante que garante que a mesma movimentação não seja registrada mais de uma vez | Não é o identificador interno do lançamento gerado pelo sistema |

---

## 3. Bounded Context: Posição

| Termo | Definição | O que não é |
|-------|-----------|-------------|
| Posição | Saldo financeiro consolidado de um dia específico, calculado a partir dos lançamentos daquele dia | Não é uma lista de movimentações; é o resultado final do dia |
| Consolidado | Resultado do processamento de todos os lançamentos de um dia, expresso como um único valor de saldo | Não é uma estimativa; representa o estado definitivo do dia processado |
| Saldo | Valor obtido pela soma dos créditos menos os débitos dos lançamentos de um dia | Não é o acumulado histórico; refere-se sempre a um dia específico |
| Data de posição | Data à qual o consolidado se refere, correspondente à data de competência dos lançamentos processados | Não é a data em que o consolidado foi calculado pelo sistema |
| Consolidação | Processo de cálculo do saldo diário a partir dos lançamentos recebidos | Não é o ato de registrar movimentações; é exclusivamente o cálculo do saldo |

---

## 4. Termos compartilhados

Termos com o mesmo significado nos dois contextos.

| Termo | Definição | O que não é |
|-------|-----------|-------------|
| Comerciante | Pessoa ou empresa que utiliza o sistema para registrar movimentações financeiras e consultar sua posição diária | Não é um operador técnico do sistema; não é um administrador |
| Data de competência | Data à qual uma movimentação financeira pertence, conforme informada pelo comerciante | Não é a data de processamento pelo sistema; não é a data de liquidação financeira |
