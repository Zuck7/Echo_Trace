#!/usr/bin/env bash
# Walks through Echo-Trace's core Phase 1 flow against a running local stack
# (docker compose up --build, or `dotnet run` against a local SQL Server).
#
# Usage: BASE_URL=http://localhost:5150 ./scripts/demo.sh
set -euo pipefail

BASE="${BASE_URL:-http://localhost:5150}/api/v1"
EMAIL="demo-$(date +%s)@acme-buyer.com"
PASSWORD="SecureP@ss123"

step() { echo; echo "── $1 ──────────────────────────────────────"; }
json() { python3 -m json.tool 2>/dev/null || cat; }

step "Health check"
curl -sf "${BASE_URL:-http://localhost:5150}/health" | json

step "Register a buying organization (self-service onboarding)"
REGISTER=$(curl -sf -X POST "$BASE/auth/register" -H "Content-Type: application/json" -d "{
  \"email\":\"$EMAIL\",\"password\":\"$PASSWORD\",
  \"orgLegalName\":\"Acme Buyer Corp\",\"orgCountry\":\"US\"}")
echo "$REGISTER" | json

step "Log in as the new Org Admin"
LOGIN=$(curl -sf -X POST "$BASE/auth/login" -H "Content-Type: application/json" -d "{
  \"email\":\"$EMAIL\",\"password\":\"$PASSWORD\"}")
TOKEN=$(echo "$LOGIN" | python3 -c "import sys,json;print(json.load(sys.stdin)['accessToken'])")
AUTH="Authorization: Bearer $TOKEN"
echo "$LOGIN" | json

BUYER_ID=$(echo "$REGISTER" | python3 -c "import sys,json;print(json.load(sys.stdin)['orgId'])")

step "Onboard two Tier-1 suppliers"
S1=$(curl -sf -X POST "$BASE/organizations" -H "$AUTH" -H "Content-Type: application/json" \
  -d '{"legalName":"Global Textiles Ltd","country":"CN","contactEmail":"ops@globaltextiles.cn"}')
echo "$S1" | json
S1_ID=$(echo "$S1" | python3 -c "import sys,json;print(json.load(sys.stdin)['orgId'])")

S2=$(curl -sf -X POST "$BASE/organizations" -H "$AUTH" -H "Content-Type: application/json" \
  -d '{"legalName":"Raw Cotton Farms Co","country":"IN","contactEmail":"info@rawcotton.in"}')
echo "$S2" | json
S2_ID=$(echo "$S2" | python3 -c "import sys,json;print(json.load(sys.stdin)['orgId'])")

step "Link both suppliers into the supply chain graph"
curl -sf -X POST "$BASE/supply-chain/edges" -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"childOrgId\":\"$S1_ID\",\"relationshipType\":\"SOURCES\"}" | json
curl -sf -X POST "$BASE/supply-chain/edges" -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"childOrgId\":\"$S2_ID\",\"relationshipType\":\"MANUFACTURES\"}" | json

step "Query the downstream supplier tree (SQL Server recursive CTE)"
curl -sf "$BASE/supply-chain/tree/$BUYER_ID" -H "$AUTH" | json

step "Reject a self-referential edge (cycle guard)"
curl -s -w "\nHTTP %{http_code}\n" -X POST "$BASE/supply-chain/edges" -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"childOrgId\":\"$BUYER_ID\",\"relationshipType\":\"SOURCES\"}"

step "Pre-flight cycle check (would linking supplier -> buyer create a loop?)"
curl -sf -X POST "$BASE/supply-chain/cycle-check" -H "$AUTH" -H "Content-Type: application/json" \
  -d "{\"parentOrgId\":\"$S1_ID\",\"childOrgId\":\"$BUYER_ID\"}" | json

step "Tamper-evident audit trail (hash-chained; every write got logged automatically)"
curl -sf "$BASE/audit/logs" -H "$AUTH" | json

echo
echo "Done. Org Admin token for further exploration in Swagger (${BASE_URL:-http://localhost:5150}/swagger):"
echo "$TOKEN"
