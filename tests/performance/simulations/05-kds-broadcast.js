import ws from "k6/ws";
import http from "k6/http";
import { check, sleep } from "k6";
import { Counter } from "k6/metrics";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";
const WS_URL = BASE_URL.replace(/^http/, "ws");
const HUB_PATH = __ENV.HUB_PATH || "/hubs/kds";
const RECORD_SEPARATOR = String.fromCharCode(0x1e);

const broadcastReceived = new Counter("kds_broadcasts_received");

export const options = {
  scenarios: {
    // 5 telas KDS ouvindo os eventos em tempo real
    kds_monitors: {
      executor: "constant-vus",
      vus: 5,
      duration: "35s",
      exec: "kdsMonitor",
    },
    // Garçons emitindo pedidos após os monitores estarem conectados
    order_emitters: {
      executor: "ramping-vus",
      startTime: "5s",
      stages: [
        { duration: "5s", target: 5 },
        { duration: "20s", target: 15 },
        { duration: "5s", target: 0 },
      ],
      exec: "orderEmitter",
    },
  },
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
    throw new Error("Nenhum produto cadastrado para gerar pedidos.");
  }

  return {
    token: token,
    productIds: productList.map((p) => p.id),
  };
}

// Fluxo 1: Monitor de Cozinha via WebSocket
export function kdsMonitor(data) {
  const url = `${WS_URL}${HUB_PATH}?access_token=${data.token}`;

  ws.connect(url, null, function (socket) {
    socket.on("open", () => {
      socket.send(
        JSON.stringify({ protocol: "json", version: 1 }) + RECORD_SEPARATOR,
      );

      socket.setInterval(() => {
        socket.send(JSON.stringify({ type: 6 }) + RECORD_SEPARATOR);
      }, 5000);
    });

    socket.on("message", (msg) => {
      // Identifica mensagens com payload de dados (tipo 1 no SignalR)
      if (
        msg.includes('"type":1') ||
        msg.includes("Order") ||
        msg.includes("order")
      ) {
        broadcastReceived.add(1);
      }
    });

    socket.setTimeout(() => {
      socket.close();
    }, 32000);
  });
}

// Fluxo 2: Garçom emitindo pedidos via HTTP
export function orderEmitter(data) {
  const authHeaders = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      "Content-Type": "application/json",
    },
  };

  const randomProductId =
    data.productIds[Math.floor(Math.random() * data.productIds.length)];
  const randomTable = Math.floor(Math.random() * 30) + 1;

  const payload = JSON.stringify({
    tableNumber: randomTable.toString(),
    customerName: `Mesa ${randomTable}`,
    items: [
      {
        productId: randomProductId,
        quantity: 1,
        notes: "Broadcast Stress k6",
      },
    ],
  });

  const res = http.post(`${BASE_URL}/api/order`, payload, authHeaders);

  check(res, {
    "POST /api/order aceito sob broadcast": (r) =>
      r.status === 200 || r.status === 201,
  });

  sleep(0.4);
}
