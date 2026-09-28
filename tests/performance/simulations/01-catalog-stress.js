import http from "k6/http";
import { check, sleep } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";

export const options = {
  stages: [
    { duration: "10s", target: 15 },
    { duration: "20s", target: 30 },
    { duration: "10s", target: 0 },
  ],
  thresholds: {
    http_req_duration: ["p(95)<200"],
    http_req_failed: ["rate==0"],
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
    throw new Error("Falha no login durante o setup do teste de catálogo.");
  }

  return { token };
}

export default function (data) {
  const authHeaders = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      "Content-Type": "application/json",
    },
  };

  const resProducts = http.get(`${BASE_URL}/api/product`, authHeaders);
  const resCategories = http.get(`${BASE_URL}/api/category`, authHeaders);

  check(resProducts, {
    "GET /api/product retornou 200": (r) => r.status === 200,
  });

  check(resCategories, {
    "GET /api/category retornou 200": (r) => r.status === 200,
  });

  sleep(1);
}
