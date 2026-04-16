import http from 'k6/http';

export function stringEnv(name) {
  const value = __ENV[name];

  if (typeof value !== 'string' || value.length === 0) {
    throw new Error(`Variavel de ambiente obrigatoria ausente: ${name}`);
  }

  return value;
}

export function numberEnv(name) {
  const rawValue = stringEnv(name);
  const normalizedValue = rawValue.replace(',', '.');
  const parsedValue = Number(normalizedValue);

  if (Number.isNaN(parsedValue)) {
    throw new Error(`Variavel de ambiente invalida para numero: ${name}=${rawValue}`);
  }

  return parsedValue;
}

export function createLoadOptions({ scenarioName, execName, durationMetricName, failureMetricName, p95EnvName, p99EnvName }) {
  return {
    scenarios: {
      [scenarioName]: {
        executor: 'constant-arrival-rate',
        exec: execName,
        rate: numberEnv('TAXA_ALVO_RPS'),
        timeUnit: '1s',
        duration: stringEnv('DURACAO_TESTE'),
        preAllocatedVUs: numberEnv('VUS_PRE_ALOCADOS'),
        maxVUs: numberEnv('VUS_MAXIMOS'),
      },
    },
    thresholds: {
      [failureMetricName]: [`rate<=${numberEnv('TAXA_ERRO_MAXIMA')}`],
      [durationMetricName]: [
        `p(95)<${numberEnv(p95EnvName)}`,
        `p(99)<${numberEnv(p99EnvName)}`,
      ],
    },
  };
}

export function authJsonHeaders(token) {
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
  };
}

export function authHeaders(token) {
  return {
    Authorization: `Bearer ${token}`,
  };
}

export function uniqueKey(prefix) {
  return `${prefix}-${__VU}-${__ITER}-${Date.now()}`;
}

export function postLancamento(baseUrl, token, payload, tags = {}) {
  return http.post(`${baseUrl}/lancamentos`, JSON.stringify(payload), {
    headers: authJsonHeaders(token),
    tags,
  });
}

export function getPosicao(baseUrl, token, data, tags = {}) {
  return http.get(`${baseUrl}/posicao/diaria?data=${data}`, {
    headers: authHeaders(token),
    tags,
  });
}