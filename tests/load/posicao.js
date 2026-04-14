import http from 'k6/http';
import { check } from 'k6';
import { Rate } from 'k6/metrics';

const erros = new Rate('erros');

export const options = {
  stages: [
    { duration: '15s', target: 20 }, // warm-up
    { duration: '60s', target: 50 }, // pico
    { duration: '10s', target: 0  }, // cool-down
  ],
  thresholds: {
    http_req_failed:                 ['rate<0.05'],
    'http_req_duration{status:200}': ['p(95)<200'],
    erros:                           ['rate<0.05'],
  },
};

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8081';
const TOKEN    = __ENV.TOKEN;

export default function () {
  const hoje = new Date().toISOString().split('T')[0];
  const res  = http.get(`${BASE_URL}/posicao/diaria?data=${hoje}`, {
    headers: { 'Authorization': `Bearer ${TOKEN}` },
  });
  const ok = check(res, { 'status 200': (r) => r.status === 200 });
  erros.add(!ok);
}
