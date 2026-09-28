import http from "k6/http";
import { check, sleep } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";

export const options = {
  scenarios: {
    cashshift_lifecycle: {
      executor: "per-vu-iterations",
      vus: 1,
      iterations: 1,
      maxDuration: "30s",
    },
  },
  thresholds: {
    checks: ["rate==1.0"],
    http_req_duration: ["p(95)<300"],
  },
};

export function setup() {
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({
      cpf: "11111111100",
      password: "admin@2026",
    }),
    { headers: { "Content-Type": "application/json" } },
  );

  const loginData = loginRes.json();
  const token = loginData.token || (loginData.data && loginData.data.token);

  if (!token) {
    throw new Error("Falha no login durante o setup do teste.");
  }

  // Busca 1 produto para tentar emitir pedido com caixa fechado
  const prodRes = http.get(`${BASE_URL}/api/product`, {
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  });

  const products = prodRes.json();
  const productList = Array.isArray(products)
    ? products
    : products.items || products.data || [];
  const productId = productList.length > 0 ? productList[0].id : null;

  return { token, productId };
}

export default function (data) {
  const authHeaders = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      "Content-Type": "application/json",
    },
  };

  // Etapa 1: Fechar o turno de caixa ativo
  console.log("1. Fechando o turno de caixa ativo...");
  const closeRes = http.post(
    `${BASE_URL}/api/cashshift/close`,
    JSON.stringify({
      actualClosingBalance: 500.0,
      notes: "Fechamento automatizado pelo teste Cenário 6",
    }),
    authHeaders,
  );

  check(closeRes, {
    "POST /api/cashshift/close retornou 200": (r) => r.status === 200,
  });

  sleep(1);

  // Etapa 2: Tentar criar pedido com caixa fechado (deve ser rejeitado pela regra de negócio)
  console.log("2. Testando tentativa de pedido com caixa fechado...");
  const blockedOrderRes = http.post(
    `${BASE_URL}/api/order`,
    JSON.stringify({
      tableNumber: "99",
      customerName: "Teste Caixa Fechado",
      items: [
        {
          productId: data.productId,
          quantity: 1,
          notes: "Deverá falhar",
        },
      ],
    }),
    authHeaders,
  );

  check(blockedOrderRes, {
    "Tentativa de pedido com caixa fechado rejeitada (Status != 200/201)": (
      r,
    ) => r.status !== 200 && r.status !== 201,
  });

  sleep(1);

  // Etapa 3: Reabrir o caixa para restabelecer o ambiente operacional
  console.log("3. Reabrindo turno de caixa com saldo inicial...");
  const reopenRes = http.post(
    `${BASE_URL}/api/cashshift/open`,
    JSON.stringify({
      openingBalance: 150.0,
      notes: "Reabertura automatizada pós-teste",
    }),
    authHeaders,
  );

  check(reopenRes, {
    "POST /api/cashshift/open retornou 200": (r) => r.status === 200,
  });
}
