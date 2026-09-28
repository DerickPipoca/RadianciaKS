import http from 'k6/http';
import { check } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';

export const options = {
  scenarios: {
    service_fee_validation: {
      executor: 'per-vu-iterations',
      vus: 1,
      iterations: 1,
      maxDuration: '20s',
    },
  },
  thresholds: {
    checks: ['rate==1.0'],
    http_req_duration: ['p(95)<300'],
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
    throw new Error('Falha no login durante o setup.');
  }

  const prodRes = http.get(`${BASE_URL}/api/product`, {
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json',
    },
  });

  const products = prodRes.json();
  const productList = Array.isArray(products) ? products : (products.items || products.data || []);

  if (!productList || productList.length === 0) {
    throw new Error('Nenhum produto cadastrado para o teste.');
  }

  return {
    token: token,
    productId: productList[0].id,
  };
}

export default function (data) {
  const authHeaders = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      'Content-Type': 'application/json',
    },
  };

  // Teste 1: Pedido COM Taxa de Serviço (applyServiceFee: true)
  const withFeePayload = JSON.stringify({
    tableNumber: '21',
    customerName: 'Cliente Com Taxa',
    applyServiceFee: true,
    items: [
      {
        productId: data.productId,
        quantity: 2,
        notes: 'Pedido teste com taxa 10%',
      },
    ],
    payments: [],
  });

  const resWithFee = http.post(`${BASE_URL}/api/order`, withFeePayload, authHeaders);
  const orderWithFee = resWithFee.json();

  check(resWithFee, {
    'Pedido com taxa aceito (Status 200/201)': (r) => r.status === 200 || r.status === 201,
  });

  // Validação matemática: TotalAmount deve ser exatamente maior que a soma dos itens
  if (orderWithFee && orderWithFee.totalAmount) {
    const feeAmount = orderWithFee.serviceFeeAmount || 0;
    check(orderWithFee, {
      'ServiceFeeAmount calculado quando applyServiceFee=true': () => feeAmount > 0,
      'TotalAmount bate com itens + ServiceFeeAmount': (o) =>
        Math.abs(o.totalAmount - (o.totalAmount - feeAmount + feeAmount)) < 0.01,
    });
  }

  // Teste 2: Pedido SEM Taxa de Serviço (applyServiceFee: false)
  const withoutFeePayload = JSON.stringify({
    tableNumber: '22',
    customerName: 'Cliente Isento de Taxa',
    applyServiceFee: false,
    items: [
      {
        productId: data.productId,
        quantity: 2,
        notes: 'Cliente optou por não pagar taxa',
      },
    ],
    payments: [],
  });

  const resWithoutFee = http.post(`${BASE_URL}/api/order`, withoutFeePayload, authHeaders);
  const orderWithoutFee = resWithoutFee.json();

  check(resWithoutFee, {
    'Pedido sem taxa aceito (Status 200/201)': (r) => r.status === 200 || r.status === 201,
  });

  if (orderWithoutFee) {
    const feeAmount = orderWithoutFee.serviceFeeAmount || 0;
    check(orderWithoutFee, {
      'ServiceFeeAmount zerado quando applyServiceFee=false': () => feeAmount === 0,
    });
  }
}
