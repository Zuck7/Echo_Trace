# ADR-001: Clean Architecture for Core API

**Date:** 2026-02-26
**Status:** Accepted
**Deciders:** Architecture Team

---

## Context

The Core API is the heart of Echo-Trace. It handles supply chain graph management, BOM logic, compliance scoring, audit trail, and DPP generation. We need an architecture that:

1. Keeps business rules independent of infrastructure details (databases, HTTP, blob storage)
2. Enables unit testing of business logic without spinning up a database or HTTP server
3. Allows swapping infrastructure implementations (e.g., different blob providers, email services)
4. Scales with team growth — new developers can onboard to a well-known pattern

## Decision

Adopt **Clean Architecture** (also known as Onion Architecture / Hexagonal Architecture) organized into four layers:

1. **Domain** — Pure C# entities, value objects, domain events, repository interfaces. Zero external dependencies.
2. **Application** — Use cases as CQRS commands/queries via MediatR. Validates with FluentValidation. Depends only on Domain.
3. **Infrastructure** — EF Core repositories, external HTTP clients, blob/email integrations. Implements Application interfaces.
4. **API** — ASP.NET Core controllers, middleware, DI registration. Depends on Application (via MediatR).

Dependency flow: `API → Application → Domain ← Infrastructure`

## Considered Alternatives

### Option A: Traditional Layered Architecture (3-tier)
Simple Controller → Service → Repository. Familiar to most developers.

- **Pros:** Low ceremony, quick to set up
- **Cons:** Services often become bloated "god classes"; business logic leaks into infrastructure; difficult to unit test without mocking infrastructure

### Option B: Clean Architecture (chosen)
- **Pros:** Business logic is fully isolated and testable; infrastructure is replaceable; team alignment via well-established pattern; MediatR provides natural separation of commands vs queries
- **Cons:** More initial boilerplate; requires discipline to keep layers clean

### Option C: Vertical Slice Architecture
Feature-based folders with commands/queries but no strict layer separation.

- **Pros:** Very low coupling between features; easy to delete/add features
- **Cons:** Harder to enforce shared conventions; risk of code duplication for cross-cutting concerns; less natural for a domain-heavy application like Echo-Trace

## Consequences

### Positive
- Business logic (cycle detection, compliance scoring, audit hashing) lives in Domain/Application with no EF Core or HTTP dependencies — fully unit-testable
- Infrastructure can be swapped: e.g., replace Azure Blob with S3 without touching business logic
- Clear ownership: domain experts own Domain, infrastructure engineers own Infrastructure

### Negative
- More files and folders per feature compared to simple layered architecture
- MediatR adds slight overhead and makes call stacks harder to trace in debuggers (mitigated by logging correlation IDs)

### Mitigations
- Use project templates and code generators to reduce boilerplate
- Document the folder conventions clearly in `docs/PROJECT_STRUCTURE.md`
