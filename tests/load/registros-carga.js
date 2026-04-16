import { check } from 'k6';
import { Rate, Trend } from 'k6/metrics';
import { createLoadOptions, numberEnv, postLancamento, stringEnv, uniqueKey } from './shared-load.js';

const duracaoRegistros = new Trend('registros_carga_duracao', true);
const falhasRegistros = new Rate('registros_carga_falhas');

const config = {
  baseUrl: stringEnv('BASE_URL_REGISTROS'),
  token: stringEnv('TOKEN'),
  dataCompetencia: stringEnv('DATA_COMPETENCIA'),
  valor: numberEnv('VALOR_LANCAMENTO'),
  descricao: stringEnv('DESCRICAO_LANCAMENTO'),
};

export const options = createLoadOptions({
  scenarioName: 'registros_carga',
  execName: 'registros_carga',
  durationMetricName: 'registros_carga_duracao',
  failureMetricName: 'registros_carga_falhas',
  p95EnvName: 'REGISTROS_P95_MS',
  p99EnvName: 'REGISTROS_P99_MS',
});

export function registros_carga() {
  const response = postLancamento(
    config.baseUrl,
    config.token,
    {
      chaveIdempotencia: uniqueKey('carga-registro'),
      tipo: __ITER % 2 === 0 ? 'CREDITO' : 'DEBITO',
      valor: config.valor,
      dataCompetencia: config.dataCompetencia,
      descricao: config.descricao,
    },
    { scenario: 'registros_carga', endpoint: 'lancamentos' }
  );

  duracaoRegistros.add(response.timings.duration);

  const ok = check(response, {
    'registro responde sucesso': (res) => res.status === 201 || res.status === 200,
  });

  falhasRegistros.add(!ok);
}

export default registros_carga;