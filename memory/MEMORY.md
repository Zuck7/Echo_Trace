# Echo-Trace Project Memory

## Project Identity
- **Name:** Echo-Trace — Tiered Supply Chain Transparency & Audit Platform
- **Stack:** C# .NET 8 (Core API), Node.js/Express (File Service), React 18/TypeScript (Web), SQL Server 2022, Redis, Docker
- **Repo root:** `/Users/uauva/Desktop/Programming/Personal Projects/Echo_Trace/`
- **Current status:** Pre-implementation — full documentation suite written, no source code yet

## Architecture Decisions (locked)
- Clean Architecture in C# API (Domain / Application / Infrastructure / API layers)
- CQRS via MediatR — commands mutate, queries read
- Graph stored as Adjacency List in SQL Server; traversal via recursive CTEs (NOT a graph database)
- File uploads handled by dedicated Node.js microservice (decoupled from C# API)
- Audit log is append-only; chained SHA-256 hashing for tamper evidence; DB trigger prevents UPDATE/DELETE
- Multi-tenancy: shared schema + TenantID row-level isolation + EF Core Global Query Filters
- JWT auth: 15-min access tokens (in-memory) + 7-day refresh tokens (HTTP-only cookie)

## Key Documents Written (all in docs/)
- `docs/SRS.md` — full requirements, use cases, acceptance criteria
- `docs/ARCHITECTURE.md` — service breakdown, layer diagrams, security, deployment
- `docs/DATA_MODEL.md` — all table DDL, ER diagram, recursive CTE examples
- `docs/API_DESIGN.md` — full REST API spec for all endpoints
- `docs/ROADMAP.md` — 3-phase roadmap with milestone checklists
- `docs/PROJECT_STRUCTURE.md` — canonical folder structure and naming conventions
- `docs/ADR/001-004` — architecture decision records

## Phase 1 First Steps (when starting implementation)
1. Initialize solution: `dotnet new sln` + 4 projects (Domain, Application, Infrastructure, API)
2. Set up Docker Compose (SQL Server, Redis, MinIO)
3. First migration: Tenants, Users, Organizations tables
4. Auth endpoints before anything else (all other work requires JWT)

## Naming Conventions
- C# namespaces: `EchoTrace.<Layer>.<Feature>` e.g. `EchoTrace.Application.Organizations.Commands`
- SQL tables: PascalCase plural (`Organizations`, `SupplyChainEdges`)
- REST routes: kebab-case plural nouns (`/supply-chain/edges`, `/organizations`)
- React components: PascalCase; hooks: camelCase with `use` prefix
