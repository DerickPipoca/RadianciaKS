import ws from "k6/ws";
import http from "k6/http";
import { check } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";
const WS_URL = BASE_URL.replace(/^http/, "ws");
const HUB_PATH = __ENV.HUB_PATH || "/hubs/kds";
const RECORD_SEPARATOR = String.fromCharCode(0x1e);

export const options = {
  stages: [
    { duration: "10s", target: 15 },
    { duration: "30s", target: 30 },
    { duration: "5s", target: 0 },
  ],
  thresholds: {
    ws_connecting: ["p(95)<300"],
    checks: ["rate>0.99"],
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
    throw new Error("Falha no login durante o setup do WebSocket.");
  }

  return { token };
}

export default function (data) {
  const url = `${WS_URL}${HUB_PATH}?access_token=${data.token}`;

  const res = ws.connect(url, null, function (socket) {
    socket.on("open", () => {
      // Handshake inicial do protocolo JSON do SignalR
      socket.send(
        JSON.stringify({ protocol: "json", version: 1 }) + RECORD_SEPARATOR,
      );

      // Keep-Alive periódico a cada 5s
      socket.setInterval(() => {
        socket.send(JSON.stringify({ type: 6 }) + RECORD_SEPARATOR);
      }, 5000);
    });

    socket.on("message", (msg) => {
      if (msg.includes("{}")) {
        check(msg, {
          "Handshake SignalR bem-sucedido": (m) => m.includes("{}"),
        });
      }
    });

    socket.on("error", (e) => {
      console.error(`Erro na conexão WebSocket: ${e.error()}`);
    });

    socket.setTimeout(() => {
      socket.close();
    }, 20000);
  });

  check(res, {
    "Conexão WebSocket estabelecida (Status 101)": (r) => r && r.status === 101,
  });
}
