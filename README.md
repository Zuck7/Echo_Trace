# Echo-Trace

**Tiered Supply Chain Transparency & Audit Platform**

Echo-Trace enables large enterprises to verify ethical and regulatory compliance across their entire supply chain — not just direct (Tier 1) suppliers. It produces a cryptographically verifiable **Digital Product Passport (DPP)** that traces materials to their raw origin across an unlimited number of supplier tiers.

---

## The Problem

Large North American firms cannot verify ethical compliance beyond their direct suppliers. Tier 2, 3, and deeper suppliers may be producing goods using forced labor, unsafe conditions, or environmentally harmful processes — and the buying organization has no visibility into this.

## The Solution

Echo-Trace provides a verifiable Digital Product Passport by:

1. **Modeling the supply chain as a graph** — organizations are nodes, supplier relationships are edges
2. **Recursively tracing** all contributing organizations, materials, and certifications to any product
3. **Pinning ISO/ESG certifications** with SHA-256 hashes for tamper-evident document storage
4. **Scoring compliance** across the full supply chain with real-time expiry tracking
5. **Maintaining an immutable audit trail** with chained hashing for regulatory defensibility

---

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Core API | C# .NET 8, Clean Architecture, MediatR, EF Core 8 |
| Database | SQL Server 2022 (Recursive CTEs for graph traversal) |
| File Service | Node.js 20, Express, multer, SHA-256 |
| Document Storage | Azure Blob Storage / MinIO |
| Frontend | React 18, TypeScript, Vite, Tailwind CSS, Cytoscape.js |
| Auth | JWT (15-min access) + HTTP-only refresh tokens |
| Cache | Redis 7 |
| Containers | Docker + Kubernetes |
| CI/CD | GitHub Actions |

---

## Quick Start (Local Development)

### Prerequisites
- Docker Desktop
- .NET 8 SDK
- Node.js 20+

### Run with Docker Compose

---

## Documentation

All pre-implementation documentation lives in [`docs/`](docs/):

| Document | Description |
|----------|-------------|
| [SRS.md](docs/SRS.md) | Software Requirements Specification — functional, non-functional, use cases, acceptance criteria |
| [ARCHITECTURE.md](docs/ARCHITECTURE.md) | System architecture — service breakdown, Clean Architecture layers, security, deployment |
| [DATA_MODEL.md](docs/DATA_MODEL.md) | Database schema, ER diagram, SQL table definitions, recursive CTE patterns |
| [API_DESIGN.md](docs/API_DESIGN.md) | Full REST API specification for all endpoints |
| [ROADMAP.md](docs/ROADMAP.md) | Development phases, milestones, and testing strategy |
| [PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md) | Monorepo folder structure and naming conventions |

### Architecture Decision Records

| ADR | Decision |
|-----|----------|
| [ADR-001](docs/ADR/001-clean-architecture.md) | Clean Architecture for the Core API |
| [ADR-002](docs/ADR/002-graph-relational-schema.md) | Graph-based relational schema with recursive CTEs |
| [ADR-003](docs/ADR/003-file-microservice.md) | Decoupled Node.js microservice for file ingestion |
| [ADR-004](docs/ADR/004-immutable-audit-trail.md) | Immutable audit trail with chained SHA-256 hashing |

---

## Architecture Overview

```
Browser (React SPA)
        │ HTTPS
        ▼
   NGINX / API Gateway
   ┌────────────────────────────────────────────────┐
   │          EchoTrace.API  (C# .NET 8)            │
   │  Domain → Application → Infrastructure → API   │
   └──────────────┬─────────────────────────────────┘
                  │ EF Core          │ HTTP (internal)
                  ▼                  ▼
            SQL Server 2022    EchoTrace.FileService
            + Redis Cache        (Node.js / Express)
                                        │
                                        ▼
                                 Azure Blob / MinIO
```

---

## License

MIT — see [LICENSE](LICENSE).
