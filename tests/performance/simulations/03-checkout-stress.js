import http from 'k6/http';
import { check } from 'k6';
import exec from 'k6/execution';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

export const options = {
  scenarios: {
    concurrent_checkouts: {
      executor: 'shared-iterations',
      vus: 20,
      iterations: 500,
      maxDuration: '1m',
    },
  },
  thresholds: {
    http_req_duration: ['p(95)<300'],
    http_req_failed: ['rate<0.01'],
  },
};

export function setup() {
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({
      cpf: '11111111100',
      password: 'admin@2026',
    }),
    { headers: { 'Content-Type': 'application/json' } }
  );

  const loginData = loginRes.json();
  const token = loginData.token || (loginData.data && loginData.data.token);

  if (!token) {
    throw new Error('Falha no login durante o setup do checkout.');
  }

  const authHeaders = {
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  };

  // Busca estritamente pedidos pendentes (paymentStatus=1) ordenados pelos mais recentes
  const ordersRes = http.get(
    `${BASE_URL}/api/order?paymentStatus=1&pageSize=600&sortBy=createdAt&isDescending=true`,
    authHeaders
  );

  const ordersData = ordersRes.json();
  const orderList = Array.isArray(ordersData)
    ? ordersData
    : (ordersData.items || ordersData.data || []);

  if (!orderList || orderList.length < 500) {
    throw new Error(`Pedidos pendentes insuficientes para o teste (encontrados: ${orderList ? orderList.length : 0}). Execute o Cenário 2 antes.`);
  }

  console.log(`Carregados ${orderList.length} pedidos pendentes para liquidar no teste.`);

  return {
    token: token,
    orders: orderList.map((o) => ({ id: o.id, totalAmount: o.totalAmount })),
  };
}

export default function (data) {
  const authHeaders = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      'Content-Type': 'application/json',
    },
  };

  const orderIndex = exec.scenario.iterationInTest % data.orders.length;
  const order = data.orders[orderIndex];

  // Garante valor suficiente para cobrir taxa ou itens (Cash = 1)
  const checkoutPayload = JSON.stringify({
    applyServiceFee: false,
    payments: [
      {
        amount: order.totalAmount + 10.0,
        method: 1, // PaymentMethod.Cash
      },
    ],
  });

  const res = http.post(
    `${BASE_URL}/api/order/${order.id}/checkout`,
    checkoutPayload,
    authHeaders
  );

  check(res, {
    'POST /api/order/{id}/checkout retornou 200': (r) => r.status === 200,
  });
}
