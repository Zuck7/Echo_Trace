# System Architecture Document
## Echo-Trace: Tiered Supply Chain Transparency & Audit Platform

**Version:** 1.0.0
**Date:** 2026-02-26

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [Service Breakdown](#2-service-breakdown)
3. [Clean Architecture — Core API](#3-clean-architecture--core-api)
4. [File Microservice](#4-file-microservice)
5. [React Frontend](#5-react-frontend)
6. [Infrastructure & Deployment](#6-infrastructure--deployment)
7. [Cross-Cutting Concerns](#7-cross-cutting-concerns)
8. [Communication Patterns](#8-communication-patterns)
9. [Security Architecture](#9-security-architecture)

---

## 1. Architecture Overview

Echo-Trace follows a **polyglot microservices** architecture where each service is independently deployable, has a single responsibility, and communicates over well-defined HTTP contracts.

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Clients                                    │
│              Browser (React SPA)  |  External API Consumers          │
└────────────────────┬────────────────────────────┬────────────────────┘
                     │ HTTPS                       │ HTTPS
           ┌─────────▼──────────┐       ┌──────────▼──────────┐
           │   NGINX / API GW   │       │   NGINX / API GW    │
           │  (Rate Limit, TLS) │       │  (Rate Limit, TLS)  │
           └─────────┬──────────┘       └──────────┬──────────┘
                     │                             │
     ┌───────────────▼─────────────┐   ┌───────────▼───────────────┐
     │     EchoTrace.API           │   │  EchoTrace.FileService     │
     │     (C# .NET 8)             │   │  (Node.js / Express)       │
     │                             │◄──┤  multipart/form-data       │
     │  ┌────────────────────────┐ │   │  SHA-256 hash pinning      │
     │  │ Domain Layer           │ │   │  Blob Storage integration  │
     │  │ Application Layer      │ │   └───────────┬───────────────┘
     │  │ Infrastructure Layer   │ │               │
     │  │ API Layer              │ │               │ Azure Blob / MinIO
     │  └────────────────────────┘ │               ▼
     └──────────────┬──────────────┘   ┌────────────────────────┐
                    │                  │   Blob Storage          │
                    │ EF Core          │   (PDF, images)         │
                    ▼                  └────────────────────────┘
          ┌─────────────────┐
          │   SQL Server    │
          │   2022          │
          │                 │
          │  ┌───────────┐  │
          │  │  Tenants  │  │
          │  │  Orgs     │  │
          │  │  Edges    │  │
          │  │  BOMs     │  │
          │  │  Docs     │  │
          │  │  Audit    │  │
          │  └───────────┘  │
          └────────┬────────┘
                   │
          ┌────────▼────────┐
          │     Redis       │
          │  (Cache, Rate   │
          │   Limiting)     │
          └─────────────────┘
```

---

## 2. Service Breakdown

### 2.1 EchoTrace.API (C# .NET 8)

**Responsibility:** All business logic — organization management, supply chain graph, BOM, trace-back, DPP generation, RBAC, audit trail.

**Key choices:**
- Clean Architecture (Domain / Application / Infrastructure / API layers)
- CQRS via MediatR (commands mutate state; queries read state)
- EF Core 8 for SQL Server ORM
- Recursive CTEs delegated to SQL Server (not application-layer recursion)
- JWT authentication via ASP.NET Identity + custom claims

**Ports:** `5000` (HTTP internal), `5001` (HTTPS)

---

### 2.2 EchoTrace.FileService (Node.js / Express)

**Responsibility:** Document ingestion only — receive file, compute SHA-256 hash, store to blob, return metadata to Core API.

**Key choices:**
- Completely decoupled from Core API (no shared database)
- `multer` for multipart parsing
- `crypto` (Node built-in) for SHA-256 hashing
- `@azure/storage-blob` or MinIO SDK for storage
- Core API calls this service via internal HTTP; the file metadata (hash, blob path) is then stored in the Core API database

**Port:** `3000`

---

### 2.3 EchoTrace.Web (React 18 / TypeScript)

**Responsibility:** SPA portal — dashboard, supply chain graph visualizer, document management, DPP export.

**Key choices:**
- React 18 + TypeScript 5
- Vite build tool
- TanStack Query (React Query) v5 for server state
- Zustand for client state (auth, UI preferences)
- React Router v6 for routing
- Shadcn/ui + Tailwind CSS for UI components
- Cytoscape.js for interactive supply chain graph rendering
- PDF.js for in-browser certificate preview

**Port:** `8080` (served by NGINX in production)

---

## 3. Clean Architecture — Core API

The Core API enforces strict dependency inversion: outer layers depend on inner layers; inner layers are unaware of outer layers.

```
┌────────────────────────────────────────────────────────┐
│                     API Layer                           │
│  Controllers, Middleware, Filters, OpenAPI             │
│  Depends on: Application Layer                         │
└──────────────────────┬─────────────────────────────────┘
                       │ Calls (MediatR Send/Publish)
┌──────────────────────▼─────────────────────────────────┐
│                 Application Layer                       │
│  Commands, Queries, Handlers, DTOs, Validators         │
│  Interfaces: IRepository<T>, IBlobStorageService,      │
│              IEmailService, IAuditService              │
│  Depends on: Domain Layer only                         │
└──────────────────────┬─────────────────────────────────┘
                       │ Implements interfaces
┌──────────────────────▼─────────────────────────────────┐
│                Infrastructure Layer                     │
│  EF Core DbContext, Repository implementations,        │
│  External clients (File Service, Email, Blob)          │
│  Depends on: Application Layer (interfaces)            │
└──────────────────────┬─────────────────────────────────┘
                       │ Core business entities
┌──────────────────────▼─────────────────────────────────┐
│                   Domain Layer                          │
│  Entities, Value Objects, Domain Events,               │
│  Domain Exceptions, Repository Interfaces              │
│  No external dependencies                              │
└────────────────────────────────────────────────────────┘
```

### 3.1 Domain Layer Contents

```
Domain/
├── Entities/
│   ├── Organization.cs          # Core org entity, owns validation logic
│   ├── SupplyChainEdge.cs       # Directed edge between orgs
│   ├── Material.cs
│   ├── Product.cs
│   ├── BomEntry.cs              # Recursive BOM node
│   ├── Document.cs              # Uploaded certification
│   ├── AuditLogEntry.cs         # Immutable audit record
│   ├── User.cs
│   └── Tenant.cs
├── ValueObjects/
│   ├── OrgTier.cs               # Computed tier depth (1..N)
│   ├── DocumentType.cs          # Enum: ISO_14001, ESG_REPORT, etc.
│   ├── ComplianceScore.cs       # 0-100 computed score
│   └── ContentHash.cs           # SHA-256 wrapper with validation
├── Events/
│   ├── OrganizationOnboardedEvent.cs
│   ├── SupplierInvitedEvent.cs
│   ├── DocumentUploadedEvent.cs
│   ├── DocumentExpiredEvent.cs
│   └── BomVersionPublishedEvent.cs
├── Exceptions/
│   ├── CycleDetectedException.cs
│   ├── DocumentIntegrityViolationException.cs
│   ├── AuditIntegrityViolationException.cs
│   └── TenantAccessViolationException.cs
└── Interfaces/
    ├── IOrganizationRepository.cs
    ├── ISupplyChainRepository.cs
    ├── IBomRepository.cs
    ├── IDocumentRepository.cs
    └── IAuditLogRepository.cs
```

### 3.2 Application Layer Contents — CQRS Structure

```
Application/
├── Organizations/
│   ├── Commands/
│   │   ├── CreateOrganization/
│   │   │   ├── CreateOrganizationCommand.cs
│   │   │   ├── CreateOrganizationHandler.cs
│   │   │   └── CreateOrganizationValidator.cs
│   │   ├── UpdateOrganization/
│   │   └── InviteSupplier/
│   └── Queries/
│       ├── GetOrganizationById/
│       ├── GetOrganizationSuppliers/
│       └── GetOrganizationTree/
├── SupplyChain/
│   ├── Commands/
│   │   ├── AddEdge/
│   │   │   ├── AddEdgeCommand.cs
│   │   │   ├── AddEdgeHandler.cs       # Calls ICycleDetectionService
│   │   │   └── AddEdgeValidator.cs
│   │   └── RemoveEdge/
│   └── Queries/
│       ├── TraceBack/
│       │   ├── TraceBackQuery.cs
│       │   └── TraceBackHandler.cs     # Delegates to SQL CTE
│       └── GetComplianceScore/
├── BOM/
│   ├── Commands/
│   │   ├── CreateBomEntry/
│   │   └── PublishBomVersion/
│   └── Queries/
│       └── ExplodeBom/                 # Recursive BOM expansion
├── Documents/
│   ├── Commands/
│   │   ├── RegisterDocument/           # Called after File Service upload
│   │   └── DeleteDocument/
│   └── Queries/
│       ├── GetDocumentsByOrg/
│       └── VerifyDocumentIntegrity/
├── DPP/
│   └── Queries/
│       └── GenerateDigitalProductPassport/
├── Audit/
│   └── Queries/
│       ├── GetAuditLog/
│       └── VerifyAuditChain/
└── Services/
    ├── ICycleDetectionService.cs
    ├── IComplianceScoreService.cs
    ├── IDppGeneratorService.cs
    └── Interfaces/
        ├── IBlobStorageService.cs
        ├── IEmailService.cs
        └── IFileServiceClient.cs
```

### 3.3 Infrastructure Layer Contents

```
Infrastructure/
├── Persistence/
│   ├── EchoTraceDbContext.cs
│   ├── Configurations/             # EF Core IEntityTypeConfiguration
│   │   ├── OrganizationConfiguration.cs
│   │   ├── SupplyChainEdgeConfiguration.cs
│   │   ├── BomEntryConfiguration.cs
│   │   ├── DocumentConfiguration.cs
│   │   └── AuditLogEntryConfiguration.cs
│   ├── Repositories/
│   │   ├── OrganizationRepository.cs
│   │   ├── SupplyChainRepository.cs  # Contains recursive CTE queries
│   │   ├── BomRepository.cs
│   │   ├── DocumentRepository.cs
│   │   └── AuditLogRepository.cs
│   └── Migrations/
├── ExternalServices/
│   ├── FileServiceClient.cs         # HTTP client to Node.js file service
│   ├── BlobStorageService.cs        # Azure Blob or MinIO
│   └── EmailService.cs              # SMTP / SendGrid
└── Services/
    ├── CycleDetectionService.cs     # DFS-based cycle detection
    ├── ComplianceScoreService.cs
    ├── AuditHashService.cs          # Chained SHA-256 audit hashing
    └── DppGeneratorService.cs
```

---

## 4. File Microservice

```
fileservice/
├── src/
│   ├── server.js              # Express app entry point
│   ├── routes/
│   │   └── upload.routes.js
│   ├── middleware/
│   │   ├── auth.middleware.js  # Validates internal service token
│   │   ├── validate.middleware.js
│   │   └── errorHandler.js
│   ├── services/
│   │   ├── hashService.js     # crypto.createHash('sha256')
│   │   └── storageService.js  # Blob storage abstraction
│   └── config/
│       └── index.js
├── Dockerfile
└── package.json
```

### 4.1 Upload Flow

```
React Client
     │  multipart/form-data
     ▼
EchoTrace.API   ────────────────────────────────────────────┐
     │                                                       │
     │  POST /internal/upload                               │
     │  (service-to-service auth header)                    │
     ▼                                                       │
FileService                                                  │
  1. Receive file buffer (multer)                           │
  2. Compute SHA-256 hash                                   │
  3. Validate MIME type                                     │
  4. Stream to Blob Storage                                 │
  5. Return { blobPath, contentHash, fileSizeBytes }       │
     │                                                       │
     │ Return upload result                                  │
     ▼                                                       │
EchoTrace.API ◄────────────────────────────────────────────┘
  6. Store Document record with contentHash, blobPath
  7. Write audit log entry
  8. Return documentId to client
```

---

## 5. React Frontend

### 5.1 Application Structure

```
web/
├── src/
│   ├── app/
│   │   ├── App.tsx
│   │   ├── router.tsx
│   │   └── providers.tsx         # QueryClientProvider, AuthProvider
│   ├── features/
│   │   ├── auth/
│   │   │   ├── LoginPage.tsx
│   │   │   ├── RegisterPage.tsx
│   │   │   └── authStore.ts      # Zustand auth slice
│   │   ├── organizations/
│   │   │   ├── OrgDashboard.tsx
│   │   │   ├── OrgDetail.tsx
│   │   │   └── InviteSupplierModal.tsx
│   │   ├── supply-chain/
│   │   │   ├── SupplyChainGraph.tsx  # Cytoscape.js visualization
│   │   │   └── TraceBackPanel.tsx
│   │   ├── bom/
│   │   │   ├── BomExplorer.tsx
│   │   │   └── BomEditor.tsx
│   │   ├── documents/
│   │   │   ├── DocumentList.tsx
│   │   │   └── DocumentUpload.tsx
│   │   ├── dpp/
│   │   │   └── DppViewer.tsx
│   │   └── audit/
│   │       └── AuditLog.tsx
│   ├── shared/
│   │   ├── components/           # Shared UI components (Button, Table…)
│   │   ├── hooks/                # useAuth, useTenant, useDebounce…
│   │   ├── api/                  # Axios client + API hooks
│   │   └── utils/
│   └── types/
│       └── api.types.ts          # Mirrored API response shapes
├── public/
├── vite.config.ts
├── tailwind.config.ts
└── tsconfig.json
```

---

## 6. Infrastructure & Deployment

### 6.1 Docker Compose (Development)

```yaml
# docker-compose.yml (abbreviated)
services:
  api:
    build: ./src/EchoTrace.API
    ports: ["5001:8080"]
    depends_on: [sqlserver, redis]
    environment:
      - ConnectionStrings__Default=...
      - FileService__BaseUrl=http://fileservice:3000

  fileservice:
    build: ./src/EchoTrace.FileService
    ports: ["3000:3000"]
    environment:
      - STORAGE_TYPE=minio
      - MINIO_ENDPOINT=minio:9000

  web:
    build: ./src/EchoTrace.Web
    ports: ["8080:80"]
    depends_on: [api]

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    ports: ["1433:1433"]

  redis:
    image: redis:7-alpine
    ports: ["6379:6379"]

  minio:
    image: minio/minio
    ports: ["9000:9000", "9001:9001"]
    command: server /data --console-address ":9001"
```

### 6.2 Kubernetes (Production)

```
k8s/
├── namespace.yaml
├── api/
│   ├── deployment.yaml       # 2 replicas min, HPA to 10
│   ├── service.yaml
│   └── configmap.yaml
├── fileservice/
│   ├── deployment.yaml       # 2 replicas min
│   └── service.yaml
├── web/
│   ├── deployment.yaml
│   └── service.yaml
├── ingress/
│   └── ingress.yaml          # NGINX Ingress + cert-manager TLS
└── secrets/
    └── (managed via sealed-secrets or Azure Key Vault CSI)
```

### 6.3 CI/CD Pipeline (GitHub Actions)

```
.github/workflows/
├── ci.yml          # On PR: lint → test → build → security scan
└── cd.yml          # On main push: build images → push → deploy
```

**CI Pipeline stages:**
1. Restore / Install dependencies
2. Run unit tests (xUnit for C#, Jest for Node.js)
3. Run integration tests
4. Build Docker images
5. Run SAST (CodeQL)
6. Dependency vulnerability scan (Dependabot / npm audit / dotnet audit)

---

## 7. Cross-Cutting Concerns

### 7.1 Structured Logging

- C# API: Serilog → Console (JSON) + Seq (development) / Application Insights (production)
- Node.js: Winston → JSON stdout
- Correlation ID header (`X-Correlation-Id`) propagated across service calls and included in every log entry

### 7.2 Error Handling

- All unhandled exceptions caught by global middleware
- Response format follows RFC 7807 Problem Details:

```json
{
  "type": "https://echo-trace.com/errors/cycle-detected",
  "title": "Circular dependency detected",
  "status": 422,
  "detail": "Adding this edge would create a cycle: Org A → Org B → Org C → Org A",
  "instance": "/api/v1/supply-chain/edges",
  "traceId": "00-abc123..."
}
```

### 7.3 Caching Strategy

| Data | Cache TTL | Invalidation Trigger |
|------|-----------|----------------------|
| Organization metadata | 5 min | On update |
| Supply chain tree (read) | 1 min | On edge add/remove |
| Document list per org | 2 min | On upload/delete |
| Compliance score | 10 min | On cert change |

### 7.4 Health Checks

Each service exposes:
- `GET /health` — liveness (returns 200 if process is alive)
- `GET /health/ready` — readiness (checks DB connection, blob storage, downstream services)

---

## 8. Communication Patterns

### 8.1 Synchronous (Request/Response)

| Caller | Callee | Protocol | Purpose |
|--------|--------|----------|---------|
| React Web | Core API | HTTPS/REST | All user-initiated operations |
| Core API | File Service | HTTP/REST (internal) | Document upload delegation |
| Core API | Email Service | SMTP/SendGrid API | Notifications |

### 8.2 Internal Service Authentication

The Core API authenticates to the File Service via a shared internal API key passed in the `X-Internal-Key` header. This key is managed via environment secrets and rotated periodically.

### 8.3 No Message Queue (MVP)

For MVP, all communication is synchronous. A message broker (e.g., Azure Service Bus) may be introduced in a future phase for:
- Document processing events
- Certification expiry batch jobs
- Async DPP generation for large supply chains

---

## 9. Security Architecture

### 9.1 Authentication Flow

```
Client
  │  POST /api/v1/auth/login { email, password }
  ▼
Core API
  │  Validate credentials → bcrypt compare
  │  Issue: AccessToken (JWT, 15 min) + RefreshToken (opaque, 7 days, stored in DB)
  ▼
Client stores AccessToken in memory (NOT localStorage)
Client stores RefreshToken in HTTP-only cookie

Subsequent requests:
  Authorization: Bearer <AccessToken>

On token expiry:
  POST /api/v1/auth/refresh (sends HTTP-only cookie automatically)
  → New AccessToken issued
```

### 9.2 JWT Claims Structure

```json
{
  "sub": "user-uuid",
  "email": "user@example.com",
  "tenantId": "tenant-uuid",
  "orgId": "org-uuid",
  "role": "ORG_ADMIN",
  "iat": 1709000000,
  "exp": 1709000900
}
```

### 9.3 Tenant Isolation

Every API handler that touches tenant-scoped data must:
1. Extract `tenantId` from JWT claims
2. Append `WHERE TenantID = @tenantId` to all queries (enforced via EF Core Global Query Filters)
3. EF Core global query filters defined on all tenant-scoped entities in `DbContext`

This prevents cross-tenant data leakage even if an application bug exists in authorization logic.

### 9.4 Audit Log Tamper Protection

```
Entry[n].Hash = SHA256(
  Entry[n-1].Hash +
  Entry[n].EntityType +
  Entry[n].EntityId +
  Entry[n].Action +
  Entry[n].NewValue +
  Entry[n].Timestamp
)
```

The first entry uses a known genesis hash (e.g., SHA256("ECHO_TRACE_GENESIS")). To verify: recompute the entire chain; any discrepancy reveals tampering at the exact entry.
