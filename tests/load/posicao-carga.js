import { check } from 'k6';
import { Rate, Trend } from 'k6/metrics';
import { createLoadOptions, getPosicao, stringEnv } from './shared-load.js';

const duracaoPosicao = new Trend('posicao_carga_duracao', true);
const falhasPosicao = new Rate('posicao_carga_falhas');

const config = {
  baseUrl: stringEnv('BASE_URL_POSICAO'),
  token: stringEnv('TOKEN'),
  dataCompetencia: stringEnv('DATA_COMPETENCIA'),
};

export const options = createLoadOptions({
  scenarioName: 'posicao_carga',
  execName: 'posicao_carga',
  durationMetricName: 'posicao_carga_duracao',
  failureMetricName: 'posicao_carga_falhas',
  p95EnvName: 'POSICAO_P95_MS',
  p99EnvName: 'POSICAO_P99_MS',
});

export function posicao_carga() {
  const response = getPosicao(
    config.baseUrl,
    config.token,
    config.dataCompetencia,
    { scenario: 'posicao_carga', endpoint: 'posicao-diaria' }
  );

  duracaoPosicao.add(response.timings.duration);

  const ok = check(response, {
    'posicao responde 200': (res) => res.status === 200,
  });

  falhasPosicao.add(!ok);
}

export default posicao_carga;