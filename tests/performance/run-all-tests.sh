#!/usr/bin/env bash
set -e

BASE_URL="${BASE_URL:-http://localhost:8080}"
DB_CONTAINER="${DB_CONTAINER:-radianciaks_postgres}"
DB_USER="${DB_USER:-radianciaKS}"
DB_NAME="${DB_NAME:-RadianciaDb}"

GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log_step() {
  echo -e "\n${BLUE}======================================================================${NC}"
  echo -e "${YELLOW}>>> [$1] $2${NC}"
  echo -e "${BLUE}======================================================================${NC}"
}

success_step() {
  echo -e "${GREEN}✓ $1 concluído com sucesso!${NC}\n"
}

# 1. Leitura de Catálogo
log_step "CENÁRIO 1" "Leitura Massiva do Catálogo (High Concurrency GET)"
k6 run -q -e BASE_URL="$BASE_URL" simulations/01-catalog-stress.js
success_step "Cenário 1"

# 2. Lançamento Concorrente de Pedidos
log_step "CENÁRIO 2" "Lançamento Concorrente de Pedidos (Burst Order Creation)"
k6 run -q -e BASE_URL="$BASE_URL" simulations/02-order-stress.js
success_step "Cenário 2"

# 3. Checkout Simultâneo
log_step "CENÁRIO 3" "Pico de Checkout Simultâneo (Payment Stress)"
k6 run -q -e BASE_URL="$BASE_URL" simulations/03-checkout-stress.js
success_step "Cenário 3"

# 4. Saturação WebSocket
log_step "CENÁRIO 4" "Saturação de Conexões WebSocket (SignalR Handshake & Hubs)"
k6 run -q -e BASE_URL="$BASE_URL" simulations/04-kds-saturation.js
success_step "Cenário 4"

# 5. Broadcast sob Carga
log_step "CENÁRIO 5" "Broadcast SignalR sob Carga Contínua (KDS Real-Time)"
k6 run -q -e BASE_URL="$BASE_URL" simulations/05-kds-broadcast.js
success_step "Cenário 5"

# Limpeza preventiva de comandas abertas para não travar o fechamento do caixa no Cenário 6
docker exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -q -c \
  'UPDATE "Orders" SET "PaymentStatus" = 3 WHERE "PaymentStatus" = 1;' >/dev/null

# 6. Ciclo de Vida do Turno de Caixa
log_step "CENÁRIO 6" "Ciclo de Vida do Turno de Caixa (Lock & Reopen)"
k6 run -q -e BASE_URL="$BASE_URL" simulations/06-cashshift-lifecycle.js
success_step "Cenário 6"

# 7. Regras de Negócio e Casos de Borda
log_step "CENÁRIO 7" "Regras de Negócio e Modificadores (Validation Rules)"
k6 run -q -e BASE_URL="$BASE_URL" simulations/07-business-rules.js
success_step "Cenário 7"

# 8. Taxa de Serviço e Cálculos Decimais
log_step "CENÁRIO 8" "Motor de Promoções e Taxa de Serviço (Financial Precision)"
k6 run -q -e BASE_URL="$BASE_URL" simulations/08-service-fee.js
success_step "Cenário 8"

# 9. Linha de Preparo KDS
log_step "CENÁRIO 9" "Fluxo da Linha de Preparo da Cozinha (KDS Lifecycle)"
k6 run -q -e BASE_URL="$BASE_URL" simulations/09-kds-workflow.js
success_step "Cenário 9"

# 10. Resiliência de Licença
log_step "CENÁRIO 10" "Resiliência Offline e Licença (Middleware 402 & Bypass)"
k6 run -q -e BASE_URL="$BASE_URL" -e MODE=ACTIVE simulations/10-license-resilience.js
docker exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -q -c 'UPDATE "SystemLicenses" SET "Status" = '\''SUSPENDED'\'';' >/dev/null
k6 run -q -e BASE_URL="$BASE_URL" -e MODE=SUSPENDED simulations/10-license-resilience.js
docker exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -q -c 'UPDATE "SystemLicenses" SET "Status" = '\''ACTIVE'\'';' >/dev/null
k6 run -q -e BASE_URL="$BASE_URL" -e MODE=ACTIVE simulations/10-license-resilience.js
success_step "Cenário 10"

# 11. Integridade Relacional e Auditoria
log_step "CENÁRIO 11" "Integridade de Dados e Trilha de Auditoria (PostgreSQL Check)"
docker exec -i "$DB_CONTAINER" psql -U "$DB_USER" -d "$DB_NAME" -c "
DO \$\$
DECLARE
  v_orphan_items INTEGER;
  v_orphan_payments INTEGER;
  v_orphan_modifiers INTEGER;
  v_divergent_orders INTEGER;
  v_audit_count INTEGER;
BEGIN
  SELECT COUNT(*) INTO v_orphan_items FROM \"OrderItems\" oi LEFT JOIN \"Orders\" o ON oi.\"OrderId\" = o.\"Id\" WHERE o.\"Id\" IS NULL;
  SELECT COUNT(*) INTO v_orphan_payments FROM \"Payments\" p LEFT JOIN \"Orders\" o ON p.\"OrderId\" = o.\"Id\" WHERE o.\"Id\" IS NULL;
  SELECT COUNT(*) INTO v_orphan_modifiers FROM \"OrderItemModifiers\" oim LEFT JOIN \"OrderItems\" oi ON oim.\"OrderItemId\" = oi.\"Id\" WHERE oi.\"Id\" IS NULL;
  SELECT COUNT(*) INTO v_divergent_orders FROM (
    SELECT o.\"Id\", o.\"TotalAmount\", o.\"ServiceFeeAmount\", COALESCE(SUM(oi.\"UnitPrice\" * oi.\"Quantity\"), 0) AS items_total
    FROM \"Orders\" o JOIN \"OrderItems\" oi ON o.\"Id\" = oi.\"OrderId\"
    GROUP BY o.\"Id\", o.\"TotalAmount\", o.\"ServiceFeeAmount\"
    HAVING ABS(o.\"TotalAmount\" - (COALESCE(SUM(oi.\"UnitPrice\" * oi.\"Quantity\"), 0) + COALESCE(o.\"ServiceFeeAmount\", 0))) > 0.05
  ) sub;
  SELECT COUNT(*) INTO v_audit_count FROM \"AuditLogs\";

  RAISE NOTICE 'Itens órfãos: % | Pagamentos órfãos: % | Modificadores órfãos: %', v_orphan_items, v_orphan_payments, v_orphan_modifiers;
  RAISE NOTICE 'Divergências de Total: % | Total de Logs de Auditoria: %', v_divergent_orders, v_audit_count;

  IF v_orphan_items > 0 OR v_orphan_payments > 0 OR v_orphan_modifiers > 0 OR v_divergent_orders > 0 THEN
    RAISE EXCEPTION 'FALHA DE INTEGRIDADE RELACIONAL!';
  END IF;
END \$\$;"
success_step "Cenário 11"

echo -e "\n${GREEN}======================================================================${NC}"
echo -e "${GREEN}SUÍTE COMPLETA EXECUTADA COM 100% DE SUCESSO NO RADIANCIAKS!${NC}"
echo -e "${GREEN}======================================================================${NC}\n"
