import http from "k6/http";
import { check } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";
const MODE = __ENV.MODE || "ACTIVE"; // 'ACTIVE' ou 'SUSPENDED'

export const options = {
  scenarios: {
    license_check: {
      executor: "per-vu-iterations",
      vus: 1,
      iterations: 1,
      maxDuration: "10s",
    },
  },
  thresholds: {
    checks: ["rate==1.0"],
  },
};

export default function () {
  // 1. Testa a rota de Bypass (/api/auth/login) - SEMPRE deve responder 200
  const loginRes = http.post(
    `${BASE_URL}/api/auth/login`,
    JSON.stringify({
      cpf: "11111111100",
      password: "admin@2026",
    }),
    { headers: { "Content-Type": "application/json" } },
  );

  check(loginRes, {
    "Bypass: /api/auth/login respondeu 200 mesmo no modo atual": (r) =>
      r.status === 200,
  });

  const loginData = loginRes.json();
  const token = loginData.token || (loginData.data && loginData.data.token);

  const authHeaders = {
    headers: {
      Authorization: `Bearer ${token}`,
      "Content-Type": "application/json",
    },
  };

  // 2. Testa rota de negócio protegida (/api/product)
  const businessRes = http.get(`${BASE_URL}/api/product`, authHeaders);

  if (MODE === "ACTIVE") {
    check(businessRes, {
      "Licença Ativa: /api/product respondeu 200 OK": (r) => r.status === 200,
    });
  } else if (MODE === "SUSPENDED") {
    check(businessRes, {
      "Licença Suspensa: /api/product interceptado com 402": (r) =>
        r.status === 402,
      "Payload contém mensagem esperada de pagamento": (r) => {
        const body = r.json();
        return (
          body && body.statusCode === 402 && body.error === "Payment Required"
        );
      },
    });
  }
}
