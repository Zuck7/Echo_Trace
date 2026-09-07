# Development Roadmap
## Echo-Trace: Tiered Supply Chain Transparency & Audit Platform

**Version:** 1.0.0
**Date:** 2026-02-26

---

## Phasing Philosophy

Echo-Trace is built in three phases:

- **Phase 1 (MVP Core):** The irreducible minimum to demonstrate value — multi-tenant orgs, supply chain graph, document upload, basic trace-back
- **Phase 2 (Compliance Engine):** Compliance scoring, DPP generation, audit chain verification, expiry alerts
- **Phase 3 (Scale & Integrations):** Performance hardening, third-party integrations, advanced analytics

Each phase ends with a shippable, tested, and deployed product increment.

---

## Phase 1 — MVP Core

**Goal:** A working multi-tenant platform where buying organizations can onboard Tier 1 suppliers, build a supply chain graph, and upload certifications.

### Milestone 1.1: Infrastructure & Project Setup

- [ ] Initialize repository structure (see `PROJECT_STRUCTURE.md`)
- [ ] Set up Docker Compose with SQL Server, Redis, MinIO
- [ ] Bootstrap C# .NET 8 solution with Clean Architecture projects
- [ ] Bootstrap Node.js file service with Express + multer
- [ ] Bootstrap React 18 + Vite + TypeScript project
- [ ] Configure GitHub Actions CI (lint, build, test gates)
- [ ] Set up EF Core DbContext with initial migrations (Tenants, Users, Organizations)
- [ ] Implement structured logging (Serilog) with correlation IDs

**Definition of Done:** `docker compose up` brings up all services; health checks pass; CI pipeline is green.

---

### Milestone 1.2: Authentication & Multi-Tenancy

- [ ] Implement `POST /auth/register` with invitation token validation
- [ ] Implement `POST /auth/login` with JWT (15-min access) + refresh token (7-day, HTTP-only cookie)
- [ ] Implement `POST /auth/refresh` and `POST /auth/logout`
- [ ] Implement `POST /auth/invite` (Org Admin generates invitation link)
- [ ] Set up ASP.NET Core Identity + custom JWT handler
- [ ] Implement `ICurrentTenantService` extracting `TenantID` from JWT claims
- [ ] Configure EF Core Global Query Filters on all tenant-scoped entities
- [ ] Implement RBAC authorization policies (`ORG_ADMIN`, `COMPLIANCE_OFFICER`, `SUPPLIER_REP`, `AUDITOR`, `PLATFORM_ADMIN`)
- [ ] Integration tests: cross-tenant access returns HTTP 403

**Definition of Done:** Full auth flow works end-to-end; cross-tenant isolation verified by tests.

---

### Milestone 1.3: Organization Management

- [ ] `GET/POST /organizations` — list and create orgs
- [ ] `GET/PUT /organizations/{orgId}` — detail and update
- [ ] `PATCH /organizations/{orgId}/status` — activate/suspend
- [ ] Materials CRUD (`GET/POST /materials`)
- [ ] Products CRUD (`GET/POST /products`)
- [ ] React: Organization dashboard, org detail page, invite supplier modal

**Definition of Done:** Platform Admin can create an org; Org Admin can update metadata; UI renders org list.

---

### Milestone 1.4: Supply Chain Graph

- [ ] `POST /supply-chain/edges` with cycle detection (`CycleDetectionService`)
- [ ] `DELETE /supply-chain/edges/{edgeId}` (soft-delete)
- [ ] `GET /supply-chain/tree/{orgId}` using recursive CTE
- [ ] `POST /supply-chain/cycle-check` pre-flight endpoint
- [ ] Unit tests: cycle detection for graphs of depth 1–10; self-referential edge rejection
- [ ] React: Cytoscape.js supply chain graph visualization

**Definition of Done:** Org Admin can add/remove supplier edges; cycle detection rejects invalid edges; graph renders in UI.

---

### Milestone 1.5: Document Ingestion

- [x] Node.js File Service: `POST /upload` with multer + SHA-256 hashing + MinIO storage
- [x] File Service: `GET /health` and internal auth middleware
- [x] Core API: `POST /documents/upload-request` → `POST /documents/confirm-upload` two-step flow
      (the file bytes are streamed through the Core API to the File Service rather than the
      browser calling the File Service directly, so its internal key never leaves the server —
      see `src/EchoTrace.API/EchoTrace.API/Controllers/DocumentsController.cs`)
- [x] Core API: `GET /documents` and `GET /documents/{id}`
- [x] Core API: `GET /documents/{id}/download` with hash re-verification
- [x] Core API: `DELETE /documents/{documentId}` (revoke)
- [x] Unit tests: SHA-256 hash computation; tampered file detection
- [x] React: Document list, upload form, certificate detail view

**Definition of Done:** Compliance Officer can upload a PDF; the system stores it with its hash; download re-verifies the hash.

---

### Milestone 1.6: Basic Audit Trail

- [ ] `AuditLogEntry` entity and repository
- [ ] `AuditHashService`: chained SHA-256 computation on every write
- [ ] Database trigger on `AuditLogEntries` preventing UPDATE/DELETE
- [ ] MediatR pipeline behavior: auto-audit-log on all commands that mutate state
- [ ] `GET /audit/logs` with filtering
- [ ] Unit tests: chain hash computation; tamper detection

**Definition of Done:** Every state change produces an audit entry with a valid chain hash; tampering with a stored hash is detectable.

---

## Phase 2 — Compliance Engine

**Goal:** Add compliance scoring, full trace-back reports, DPP generation, and certification expiry management.

### Milestone 2.1: Full Trace-Back & Compliance Score

- [ ] `GET /supply-chain/trace/{orgId}` — full provenance graph via CTE
- [ ] `ComplianceScoreService` — calculates score from cert validity across all nodes
- [ ] Caching of trace results in Redis (1-min TTL)
- [ ] React: Trace-back panel with compliance score badge, expandable org nodes

---

### Milestone 2.2: BOM Management

- [ ] `POST /products/{productId}/bom` — add BOM entries
- [ ] `POST /products/{productId}/bom/publish` — version lock
- [ ] `GET /products/{productId}/bom/explode` — recursive BOM explosion CTE
- [ ] Unit tests: BOM explosion with 5-level hierarchy; quantity accumulation
- [ ] React: BOM editor tree, BOM explosion viewer

---

### Milestone 2.3: Certification Expiry Management

- [ ] Background job (Hangfire or hosted `IHostedService`): daily scan for `ExpiresAt` within 30 and 7 days
- [ ] `EmailService` integration (SMTP or SendGrid)
- [ ] Email templates: expiry warning (30d and 7d), invitation, registration
- [ ] Auto-update document `Status` to `Expired` when `ExpiresAt` passes
- [ ] React: Expiring soon dashboard widget with count badges

---

### Milestone 2.4: Digital Product Passport

- [ ] `GET /dpp/{productId}` — JSON format with HMAC-SHA256 signature
- [ ] `GET /dpp/{productId}` — PDF format (QuestPDF or similar)
- [ ] `GET /dpp/{productId}/verify` — signature verification
- [ ] React: DPP viewer page with export buttons

---

### Milestone 2.5: Audit Chain Verification

- [ ] `GET /audit/verify/{entityType}/{entityId}` — chain integrity check endpoint
- [ ] React: Audit log page with verify button; pass/fail indicator per entry
- [ ] Auditor scoped access: `AuditorScopes` table + enforcement in RBAC middleware

---

## Phase 3 — Scale & Integrations

**Goal:** Production hardening, external integrations, and advanced features.

### Milestone 3.1: Performance & Observability

- [ ] Load testing (k6): trace-back at depth 20 with 500 concurrent users
- [ ] SQL Server read replica routing for trace-back queries
- [ ] Redis cache coverage for supply chain tree and compliance score
- [ ] OpenTelemetry tracing across Core API and File Service
- [ ] Grafana dashboard: P95 response times, error rates, cache hit rates

---

### Milestone 3.2: Production Infrastructure

- [ ] Kubernetes manifests (see `k8s/` directory)
- [ ] NGINX Ingress with TLS (cert-manager + Let's Encrypt)
- [ ] Kubernetes HPA for API and File Service pods
- [ ] Azure Key Vault CSI driver for secrets injection
- [ ] Database backup strategy and restore runbook

---

### Milestone 3.3: Advanced Features

- [ ] Auditor external access portal (scoped read-only with `AuditorScopes`)
- [ ] CSV/Excel export of supply chain data
- [ ] Supplier self-certification status dashboard (public-facing URL per org)
- [ ] Webhook notifications when a supplier's certification expires

---

### Milestone 3.4: Third-Party Integrations (Future)

- [ ] SAP / Oracle ERP data import for BOM auto-population
- [ ] GS1 EPCIS compliance for DPP format
- [ ] Carbon footprint API integration (Scope 3 emissions data)

---

## Testing Strategy

| Layer | Tooling | Target Coverage |
|-------|---------|-----------------|
| Domain logic (cycle detection, audit hashing, compliance score) | xUnit + NSubstitute | ≥ 90% |
| Application handlers | xUnit + in-memory DB | ≥ 80% |
| API integration tests | `WebApplicationFactory` + TestContainers (SQL Server) | Critical paths |
| File Service unit tests | Jest | ≥ 80% |
| React component tests | Vitest + React Testing Library | Key components |
| E2E (smoke tests) | Playwright | Happy paths per milestone |

---

## Definition of Done (Cross-Phase)

Every feature is considered done when:
1. Code passes all CI checks (lint, tests, build)
2. Unit tests written and passing with adequate coverage
3. API endpoint tested via integration test
4. Documentation updated (SRS acceptance criteria verified)
5. No open P0/P1 bugs related to the feature
