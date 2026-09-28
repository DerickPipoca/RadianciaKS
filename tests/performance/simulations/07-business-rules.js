import http from "k6/http";
import { check } from "k6";

const BASE_URL = __ENV.BASE_URL || "http://localhost:8080";

export const options = {
  scenarios: {
    business_rules_validation: {
      executor: "per-vu-iterations",
      vus: 1,
      iterations: 1,
      maxDuration: "20s",
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

  if (!productList || productList.length < 2) {
    throw new Error(
      "Necessário ao menos 2 produtos cadastrados para o teste de regras de negócio.",
    );
  }

  return {
    token: token,
    productA: productList[0].id,
    productB: productList[1].id,
  };
}

export default function (data) {
  const authHeaders = {
    headers: {
      Authorization: `Bearer ${data.token}`,
      "Content-Type": "application/json",
    },
  };

  // Teste 1: Quantidade inválida (0) -> Deve retornar 400
  const zeroQtyRes = http.post(
    `${BASE_URL}/api/order`,
    JSON.stringify({
      tableNumber: "10",
      customerName: "Cliente Qtd Zero",
      items: [
        {
          productId: data.productA,
          quantity: 0,
          notes: "Deverá falhar por quantidade zero",
        },
      ],
    }),
    authHeaders,
  );

  check(zeroQtyRes, {
    "Rejeição por quantidade zero (Status 400)": (r) => r.status === 400,
  });

  // Teste 2: Pedido sem itens -> Deve retornar 400
  const emptyItemsRes = http.post(
    `${BASE_URL}/api/order`,
    JSON.stringify({
      tableNumber: "11",
      customerName: "Cliente Sem Itens",
      items: [],
    }),
    authHeaders,
  );

  check(emptyItemsRes, {
    "Rejeição por lista de itens vazia (Status 400)": (r) => r.status === 400,
  });

  // Teste 3: Produto inexistente -> Deve retornar 400 ou 404
  const invalidProdRes = http.post(
    `${BASE_URL}/api/order`,
    JSON.stringify({
      tableNumber: "12",
      customerName: "Cliente Produto Falso",
      items: [
        {
          productId: "00000000-0000-0000-0000-000000000000",
          quantity: 1,
          notes: "Produto inexistente",
        },
      ],
    }),
    authHeaders,
  );

  check(invalidProdRes, {
    "Rejeição por produto inexistente (Status 400 ou 404)": (r) =>
      r.status === 400 || r.status === 404,
  });

  // Teste 4: Pedido válido multi-itens com observações customizadas de preparo
  const validOrderRes = http.post(
    `${BASE_URL}/api/order`,
    JSON.stringify({
      tableNumber: "15",
      customerName: "Mesa Gourmet",
      items: [
        {
          productId: data.productA,
          quantity: 2,
          notes: "Sem cebola, maionese à parte",
        },
        {
          productId: data.productB,
          quantity: 1,
          notes: "Carne ao ponto para mal passada",
        },
      ],
    }),
    authHeaders,
  );

  check(validOrderRes, {
    "Pedido multi-itens customizado aceito (Status 200 ou 201)": (r) =>
      r.status === 200 || r.status === 201,
  });
}
