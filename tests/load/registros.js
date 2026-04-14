import http from 'k6/http';
import { check } from 'k6';
import { Rate } from 'k6/metrics';

const erros = new Rate('erros');

export const options = {
  stages: [
    { duration: '30s', target: 20  }, // warm-up
    { duration: '60s', target: 100 }, // pico
    { duration: '15s', target: 0   }, // cool-down
  ],
  thresholds: {
    http_req_failed:   ['rate<0.05'],  // RNF-001: ≤ 5% de erro
    'http_req_duration{status:201}': ['p(95)<500'], // RNF-002: p95 < 500ms
    erros:             ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const TOKEN    = __ENV.TOKEN;

export default function () {
  const payload = JSON.stringify({
    tipo:              'CREDITO',
    valor:             100.00,
    dataCompetencia:   new Date().toISOString().split('T')[0],
    chaveIdempotencia: `k6-vu${__VU}-iter${__ITER}`,
  });

  const res = http.post(`${BASE_URL}/lancamentos`, payload, {
    headers: {
      'Content-Type':  'application/json',
      'Authorization': `Bearer ${TOKEN}`,
    },
  });

  const ok = check(res, {
    'status 201': (r) => r.status === 201,
  });

  erros.add(!ok);
}
