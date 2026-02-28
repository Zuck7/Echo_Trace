# ADR-004: Immutable Audit Trail with Chained SHA-256 Hashing

**Date:** 2026-02-26
**Status:** Accepted
**Deciders:** Architecture Team

---

## Context

Echo-Trace operates in the regulatory compliance domain. Auditors, enterprise buyers, and regulators need to trust that:

1. The history of any supply chain entity (organization, document, edge) has not been altered retroactively
2. If a record was tampered with, the system can detect the tamper and identify the exact point of modification
3. The audit trail is complete — no entries can be silently deleted

This is a legal and compliance requirement, not just a "nice to have." Some customers (e.g., companies subject to SEC climate disclosure rules or EU CSRD) may need to produce the audit trail in regulatory proceedings.

## Decision

Implement an **append-only audit log** with **chained SHA-256 hashing** (a simplified blockchain-like structure):

1. **Append-only enforcement at the database layer:** A SQL Server `AFTER UPDATE, DELETE` trigger on `AuditLogEntries` raises an error and rolls back any modification or deletion. This cannot be bypassed by application bugs.

2. **Chained hash per entry:**
   ```
   Entry[0].ChainHash = SHA256("ECHO_TRACE_GENESIS" + Entry[0].data)
   Entry[n].ChainHash = SHA256(Entry[n-1].ChainHash + Entry[n].data)
   ```
   Where `Entry[n].data = EntityType + EntityId + Action + NewValue + OccurredAt`

3. **Integrity verification endpoint:** `GET /audit/verify/{entityType}/{entityId}` recomputes the entire chain and reports any mismatches.

## Considered Alternatives

### Option A: Simple append-only log (no hashing)
Prevent deletes via trigger but no hash chaining.

- **Pros:** Simpler implementation
- **Cons:** A determined attacker with database access could modify a row's content without changing the row count — the modification would be undetectable

### Option B: Chained SHA-256 hashing (chosen)
- **Pros:** Any modification to any stored field in any past entry is detectable by recomputing the chain; provides mathematical proof of integrity
- **Cons:** Verification is O(n) in the number of entries; computing the hash on every write adds a small amount of latency

### Option C: Blockchain / distributed ledger
- **Pros:** Tamper-evident by distributed consensus; no single point of trust
- **Cons:** Massive added complexity; transaction costs; overkill — a centralized platform with a chained hash provides sufficient tamper-evidence for the threat model (insider database modification, not distributed Byzantine actors)

### Option D: Write-ahead log shipped to immutable object storage (S3 Glacier)
- **Pros:** True immutability via write-once storage policies
- **Cons:** Introduces async consistency; verification is complex; cannot query the log in real time

## Consequences

### Positive
- The database-level trigger is a hard technical control that the application layer cannot bypass
- Chained hashing provides mathematical tamper evidence without external dependencies
- Verification endpoint gives auditors a clear pass/fail report
- The approach is understandable and auditable by security reviewers without requiring blockchain knowledge

### Negative
- Hash computation adds ~1ms per audit entry write (acceptable)
- Chain verification is O(n) — for entities with thousands of changes, verification takes longer. Mitigated by paginating the verification endpoint.
- If the genesis hash or algorithm is changed, old chains cannot be re-verified without migration

### Threat Model

This design protects against:
- Application-layer bugs that accidentally modify or delete audit entries
- Insider database access that modifies audit entries post-hoc

This design does NOT protect against:
- A DBA who can disable triggers — mitigated by monitoring trigger status in CI health checks
- Loss of the database entirely — mitigated by backups

The `AuditLogEntries` table intentionally has no `TenantID` foreign key constraint, ensuring audit records survive even if a tenant is deleted.
