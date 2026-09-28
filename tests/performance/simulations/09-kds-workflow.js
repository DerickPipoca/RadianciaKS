import http from "k6/http";
import { check, sleep } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";

// Valores padrão do enum KdsStatus no C# (Pending: 0, Preparing: 1, Done: 2)
const KDS_STATUS = {
  PREPARING: 2,
  DONE: 5,
};

export const options = {
  scenarios: {
    kds_workflow: {
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
    throw new Error("Falha no login durante o setup.");
  }

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

  if (!productList || productList.length === 0) {
    throw new Error("Nenhum produto cadastrado para o teste.");
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
      "Content-Type": "application/json",
    },
  };

  // Etapa 1: Garçom lança o pedido na mesa 33
  console.log("1. Garçom lançando novo pedido...");
  const createOrderPayload = JSON.stringify({
    tableNumber: "33",
    customerName: "Mesa Esteira KDS",
    applyServiceFee: false,
    items: [
      {
        productId: data.productId,
        quantity: 1,
        notes: "Ponto da carne: ao ponto",
      },
    ],
    payments: [],
  });

  const createRes = http.post(
    `${BASE_URL}/api/order`,
    createOrderPayload,
    authHeaders,
  );

  check(createRes, {
    "1. POST /api/order criado com sucesso (Status 200/201)": (r) =>
      r.status === 200 || r.status === 201,
  });

  const createdOrder = createRes.json();
  const orderId = createdOrder.id;
  const itemId =
    createdOrder.items && createdOrder.items.length > 0
      ? createdOrder.items[0].id
      : null;

  if (!orderId || !itemId) {
    throw new Error(
      `Falha ao capturar IDs do pedido gerado (orderId: ${orderId}, itemId: ${itemId})`,
    );
  }

  sleep(0.5);

  // Etapa 2: Monitor KDS consulta pedidos pendentes na cozinha
  console.log("2. Cozinha consultando fila pendente no KDS...");
  const pendingRes = http.get(`${BASE_URL}/api/kds/pending`, authHeaders);

  check(pendingRes, {
    "2. GET /api/kds/pending retornou 200": (r) => r.status === 200,
  });

  sleep(0.5);

  // Etapa 3: Cozinheiro assume o item -> Status: Preparing (1)
  console.log(`3. Cozinheiro iniciando preparo do item ${itemId}...`);
  const preparingRes = http.put(
    `${BASE_URL}/api/kds/${orderId}/items/${itemId}/status`,
    JSON.stringify(KDS_STATUS.PREPARING),
    authHeaders,
  );

  check(preparingRes, {
    "3. PUT /api/kds/.../status para Preparing retornou 200": (r) =>
      r.status === 200,
  });

  sleep(0.5);

  // Etapa 4: Cozinheiro finaliza o item -> Status: Done (2)
  console.log(`4. Cozinheiro concluindo preparo do item ${itemId}...`);
  const doneRes = http.put(
    `${BASE_URL}/api/kds/${orderId}/items/${itemId}/status`,
    JSON.stringify(KDS_STATUS.DONE),
    authHeaders,
  );

  check(doneRes, {
    "4. PUT /api/kds/.../status para Done retornou 200": (r) =>
      r.status === 200,
  });

  sleep(0.5);

  // Etapa 5: Garçom retira e entrega o pedido na mesa
  console.log(`5. Garçom entregando o pedido ${orderId} na mesa...`);
  const deliverRes = http.put(
    `${BASE_URL}/api/order/${orderId}/deliver`,
    null,
    authHeaders,
  );

  check(deliverRes, {
    "5. PUT /api/order/{id}/deliver retornou 200": (r) => r.status === 200,
  });
}
