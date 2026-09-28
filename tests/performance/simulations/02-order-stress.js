import http from "k6/http";
import { check, sleep } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";

export const options = {
  stages: [
    { duration: "10s", target: 10 },
    { duration: "20s", target: 20 },
    { duration: "10s", target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<300"],
    http_req_failed: ["rate<0.01"],
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
    throw new Error("Falha no login durante o setup do teste de pedidos.");
  }

  const authHeaders = {
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  };

  const prodRes = http.get(`${BASE_URL}/api/product`, authHeaders);
  const products = prodRes.json();
  const productList = Array.isArray(products)
    ? products
    : products.items || products.data || [];

  if (!productList || productList.length === 0) {
    throw new Error(
      "Nenhum produto cadastrado para compor os pedidos de stress.",
    );
  }

  return {
    token: token,
    productIds: productList.map((p) => p.id),
  };
}

export default function (data) {
  const authHeaders = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      "Content-Type": "application/json",
    },
  };

  const randomProductId =
    data.productIds[Math.floor(Math.random() * data.productIds.length)];
  const randomTable = Math.floor(Math.random() * 30) + 1;

  const orderPayload = JSON.stringify({
    tableNumber: randomTable.toString(),
    customerName: `Mesa ${randomTable}`,
    items: [
      {
        productId: randomProductId,
        quantity: Math.floor(Math.random() * 3) + 1,
        notes: "Pedido automatizado k6",
      },
    ],
  });

  const res = http.post(`${BASE_URL}/api/order`, orderPayload, authHeaders);

  check(res, {
    "POST /api/order retornou 200 ou 201": (r) =>
      r.status === 200 || r.status === 201,
  });

  sleep(0.5);
}
