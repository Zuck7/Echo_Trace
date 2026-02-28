# Software Requirements Specification (SRS)
## Echo-Trace: Tiered Supply Chain Transparency & Audit Platform

**Version:** 1.0.0
**Date:** 2026-02-26
**Status:** Approved for Development

---

## Table of Contents

1. [Introduction](#1-introduction)
2. [Overall Description](#2-overall-description)
3. [Functional Requirements](#3-functional-requirements)
4. [Non-Functional Requirements](#4-non-functional-requirements)
5. [System Constraints](#5-system-constraints)
6. [External Interface Requirements](#6-external-interface-requirements)
7. [Use Cases](#7-use-cases)
8. [Acceptance Criteria](#8-acceptance-criteria)

---

## 1. Introduction

### 1.1 Purpose

This SRS defines the requirements for Echo-Trace, a multi-tenant SaaS platform that enables large North American enterprises to achieve end-to-end supply chain transparency. The system produces a verifiable "Digital Product Passport" that traces materials to their raw origin across an unlimited number of supplier tiers.

### 1.2 Scope

Echo-Trace addresses the inability of buying organizations to verify ethical and regulatory compliance beyond their direct (Tier 1) suppliers. The platform provides:

- Recursive bill-of-materials (BOM) management across N supplier tiers
- Multi-tenant supplier onboarding and relationship management
- Secure ingestion and verification of ISO/ESG audit certifications
- Immutable audit trails with cryptographic integrity verification
- Graph-traversal queries to detect circular dependencies and produce trace-back reports

### 1.3 Definitions and Acronyms

| Term | Definition |
|------|------------|
| Tier 1 | Direct supplier to the buying organization |
| Tier N | A supplier at depth N in the supply chain graph |
| BOM | Bill of Materials — hierarchical list of components in a product |
| DPP | Digital Product Passport — cryptographically verifiable provenance record |
| ESG | Environmental, Social, and Governance compliance documents |
| ISO | International Standards Organization certification documents |
| CTE | Common Table Expression — recursive SQL query pattern |
| Tenant | An organization with its own isolated data partition |
| Edge | A directed supplier relationship between two organizations |
| Node | An organization entity in the supply chain graph |

### 1.4 References

- ISO 14001: Environmental Management Systems
- ISO 45001: Occupational Health & Safety
- GRI Standards: Global Reporting Initiative ESG framework
- EU Corporate Sustainability Reporting Directive (CSRD)
- US SEC Climate Disclosure Rules

---

## 2. Overall Description

### 2.1 Product Perspective

Echo-Trace is a cloud-native SaaS platform composed of three services:

1. **Core API** (C# .NET 8) — business logic, supply chain graph, BOM management
2. **File Microservice** (Node.js) — document ingestion, hash verification, blob storage
3. **Web Application** (React 18) — portal for all tenant roles

The platform is multi-tenant: each organization onboards as a tenant. A "buying organization" (the primary customer) invites their Tier 1 suppliers, who in turn invite their own suppliers, building the supply chain graph recursively.

### 2.2 Product Functions Summary

| ID | Function |
|----|----------|
| F-01 | Multi-tenant organization onboarding |
| F-02 | Supplier relationship management (graph edges) |
| F-03 | Recursive BOM creation and management |
| F-04 | Supply chain trace-back (full lineage query) |
| F-05 | Circular dependency detection |
| F-06 | Document upload with SHA-256 hash pinning |
| F-07 | Certification expiry tracking and alerts |
| F-08 | Role-based access control per tenant |
| F-09 | Immutable audit log with chained hashing |
| F-10 | Digital Product Passport export (PDF/JSON) |

### 2.3 User Classes and Characteristics

| Role | Description | Permissions |
|------|-------------|-------------|
| Platform Admin | Anthropic internal staff; manages tenants | Full system access |
| Org Admin | Administrator of a tenant organization | Manage own org, invite suppliers |
| Compliance Officer | Reviews certifications and audit reports | Read all, upload documents |
| Supplier Rep | Representative of a supplier org | Manage own org data, upload docs |
| Auditor (Read-Only) | External auditor with scoped access | Read-only on assigned scope |

### 2.4 Operating Environment

- Cloud: Azure (primary) or AWS
- Runtime: Docker containers on Kubernetes
- Database: SQL Server 2022
- Storage: Azure Blob Storage (or MinIO for self-hosted)
- Cache: Redis 7
- CI/CD: GitHub Actions

### 2.5 Assumptions and Dependencies

- Each supplier org self-manages their own data after onboarding invitation
- SSL/TLS is enforced on all endpoints (no HTTP)
- Document files are PDF, maximum 50 MB per file
- Tenant isolation is enforced at the database row level (TenantID column)
- Circular supply chain references are invalid and must be prevented

---

## 3. Functional Requirements

### FR-01: Organization Onboarding

**FR-01.1** The system shall allow a Platform Admin to create a root tenant organization.

**FR-01.2** An Org Admin shall be able to generate a time-limited invitation link (TTL: 7 days) for a supplier organization.

**FR-01.3** Upon accepting an invitation, a new tenant organization shall be created with the inviting org assigned as a parent node in the supply chain graph.

**FR-01.4** Each organization shall store the following metadata:
- Legal name, country, industry sector, registration number
- Contact email, address
- Tier level (auto-calculated from graph depth)
- ESG risk score (manually assigned, 1–100)
- Status: `Active`, `Suspended`, `Pending`

**FR-01.5** The system shall prevent an organization from onboarding itself as its own supplier (self-referential edge).

---

### FR-02: Supply Chain Graph Management

**FR-02.1** The system shall model supplier relationships as a directed acyclic graph (DAG) where:
- Each node = one organization
- Each directed edge = "OrgA sources from OrgB" (ParentOrgID → ChildOrgID)
- Each edge may be scoped to a specific material or product

**FR-02.2** Before creating a new edge, the system shall run a cycle-detection algorithm. If adding the edge would create a cycle, the operation must be rejected with error code `CYCLE_DETECTED`.

**FR-02.3** An Org Admin shall be able to deactivate (soft-delete) an edge without losing historical audit data.

**FR-02.4** The system shall support querying the full downstream supplier tree from any node, with configurable depth limit (default: unlimited).

**FR-02.5** The system shall support querying the full upstream customer tree from any node (reverse traversal).

---

### FR-03: Bill of Materials (BOM) Management

**FR-03.1** An Org Admin shall be able to define a Product with a hierarchical BOM.

**FR-03.2** A BOM entry shall reference a Material and may reference a parent BOM entry, enabling recursive tree structures.

**FR-03.3** The system shall support BOM versioning. Older versions shall be read-only after a new version is published.

**FR-03.4** The system shall provide a "BOM Explosion" query that recursively expands all sub-components to leaf-level materials, including quantities.

**FR-03.5** Materials shall include:
- Name, description, HS tariff code, unit of measure
- Hazardous classification (REACH, RoHS flags)
- Country of origin

---

### FR-04: Supply Chain Trace-Back

**FR-04.1** A user shall be able to initiate a "Trace-Back" from any product, returning the full provenance graph including all contributing organizations, materials, and certifications.

**FR-04.2** Trace results shall be computed via recursive CTEs in SQL Server for performance, not application-layer recursion.

**FR-04.3** The system shall calculate a "Compliance Score" for any trace result, based on the percentage of nodes with valid (non-expired) certifications.

**FR-04.4** Trace results shall be exportable as:
- JSON (machine-readable DPP)
- PDF report (human-readable Digital Product Passport)

**FR-04.5** Trace queries for supply chains deeper than 20 tiers shall execute within 5 seconds (P95).

---

### FR-05: Document Ingestion and Verification

**FR-05.1** The File Microservice shall accept document uploads via `multipart/form-data`.

**FR-05.2** Upon upload, the system shall:
1. Compute SHA-256 hash of the file content
2. Store the hash alongside the document metadata
3. Save the file to blob storage
4. Return the `documentId` and `contentHash` to the caller

**FR-05.3** Supported document types: `ISO_14001`, `ISO_45001`, `ESG_REPORT`, `LABOR_AUDIT`, `ENVIRONMENTAL_PERMIT`, `CUSTOMS_CERT`, `OTHER`.

**FR-05.4** Each document shall have an `ExpiresAt` date. The system shall flag documents as `EXPIRED` automatically when the date passes.

**FR-05.5** The system shall send email notifications 30 days and 7 days before a certification expires to the Org Admin.

**FR-05.6** Document downloads shall re-verify SHA-256 hash against the stored value. If hashes differ, the download is rejected with error `DOCUMENT_INTEGRITY_VIOLATION`.

**FR-05.7** File size limit: 50 MB. Accepted MIME types: `application/pdf`, `image/png`, `image/jpeg`.

---

### FR-06: Role-Based Access Control (RBAC)

**FR-06.1** The system shall implement tenant-scoped RBAC with the following roles:
- `PLATFORM_ADMIN` — cross-tenant access
- `ORG_ADMIN` — full access within own tenant
- `COMPLIANCE_OFFICER` — read all + upload documents within own tenant
- `SUPPLIER_REP` — manage own org data, upload documents
- `AUDITOR` — read-only on explicitly granted scope

**FR-06.2** All API endpoints shall validate that the requesting user's `TenantID` matches the resource's `TenantID`, unless the user holds `PLATFORM_ADMIN` role.

**FR-06.3** An Org Admin may grant an Auditor scoped read access to a specific supply chain sub-tree, defined by a root node and depth.

---

### FR-07: Immutable Audit Trail

**FR-07.1** Every state-changing operation (CREATE, UPDATE, DELETE) on core entities shall produce an immutable audit log entry.

**FR-07.2** Audit log entries shall never be deleted or updated. The `AuditLogs` table shall be append-only, enforced at the database layer via a trigger that rejects UPDATE/DELETE.

**FR-07.3** Each audit entry shall store:
- Entity type and ID
- Action performed
- Old state (JSON snapshot)
- New state (JSON snapshot)
- Performing user ID and IP address
- UTC timestamp
- SHA-256 hash of the previous entry + current entry data (chained hash for tamper evidence)

**FR-07.4** An Auditor shall be able to verify the chain integrity of the audit log for any entity, confirming no entries were tampered with.

---

### FR-08: Digital Product Passport (DPP) Generation

**FR-08.1** A DPP shall be generated on demand for any Product in the system.

**FR-08.2** The DPP JSON payload shall conform to a versioned schema and include:
- Product metadata
- Full BOM with material origins
- All contributing organizations with compliance status
- All linked certifications with validity status
- Trace-back timestamp and requesting user

**FR-08.3** The DPP JSON payload shall be cryptographically signed using an HMAC-SHA256 key managed by the platform.

**FR-08.4** A DPP PDF report shall be generated from the JSON payload, containing a QR code linking to the live trace result endpoint.

---

## 4. Non-Functional Requirements

### 4.1 Performance

| Metric | Target |
|--------|--------|
| API P95 response time (simple CRUD) | < 200ms |
| Trace-back query (depth ≤ 20) | < 5s P95 |
| Document upload (≤ 50 MB) | < 30s |
| System uptime SLA | 99.9% |
| Concurrent users per tenant | 500 |

### 4.2 Security

- **NFR-SEC-01:** All data in transit encrypted via TLS 1.3 minimum.
- **NFR-SEC-02:** All data at rest encrypted (AES-256) in blob storage and database.
- **NFR-SEC-03:** Authentication via JWT (HS256, 15-min access tokens, 7-day refresh tokens).
- **NFR-SEC-04:** Passwords hashed with bcrypt (cost factor ≥ 12).
- **NFR-SEC-05:** SQL parameterized queries enforced via EF Core (no raw string interpolation).
- **NFR-SEC-06:** API rate limiting: 1000 req/min per tenant, 100 req/min per user.
- **NFR-SEC-07:** Document hashes stored and verified on every retrieval.
- **NFR-SEC-08:** Audit log is append-only with database-level enforcement.
- **NFR-SEC-09:** All secrets managed via environment variables or Azure Key Vault (never in code).

### 4.3 Scalability

- **NFR-SCALE-01:** Stateless API pods; horizontal scaling via Kubernetes HPA.
- **NFR-SCALE-02:** Database read replicas for trace-back queries.
- **NFR-SCALE-03:** File microservice is independently scalable.
- **NFR-SCALE-04:** System must support up to 10,000 tenant organizations.

### 4.4 Maintainability

- **NFR-MAINT-01:** Code coverage ≥ 80% for domain and application layers.
- **NFR-MAINT-02:** All API changes must be versioned (`/api/v1/`, `/api/v2/`).
- **NFR-MAINT-03:** CI pipeline must include lint, test, and security scan gates.
- **NFR-MAINT-04:** All migrations managed via EF Core Migrations; no manual schema changes.

### 4.5 Availability & Disaster Recovery

- **NFR-DR-01:** Daily automated database backups retained for 30 days.
- **NFR-DR-02:** RPO (Recovery Point Objective): 1 hour.
- **NFR-DR-03:** RTO (Recovery Time Objective): 4 hours.

---

## 5. System Constraints

- **CON-01:** The platform must run entirely within Docker containers for portability.
- **CON-02:** No client-side storage of sensitive data (localStorage or sessionStorage forbidden for auth tokens; use HTTP-only cookies or in-memory).
- **CON-03:** Audit log entries must be immutable — enforced at both application and database layers.
- **CON-04:** Circular dependency detection must occur synchronously before any edge is persisted.
- **CON-05:** The File Microservice must be decoupled from the Core API — they communicate only via HTTP, never sharing a database.
- **CON-06:** All timestamps stored in UTC.

---

## 6. External Interface Requirements

### 6.1 User Interface

- Responsive web application supporting Chrome 120+, Firefox 121+, Safari 17+, Edge 120+
- Minimum viewport: 1280px wide
- Accessible per WCAG 2.1 AA

### 6.2 API Interface

- RESTful JSON API, versioned at `/api/v1/`
- File upload via `multipart/form-data`
- Error responses follow RFC 7807 Problem Details format
- OpenAPI 3.1 specification auto-generated and served at `/api/docs`

### 6.3 Storage Interface

- Blob storage abstracted via a `IBlobStorageService` interface
- Default implementation: Azure Blob Storage
- Alternate implementation: MinIO (for local development via Docker)

### 6.4 Email Interface

- Email notifications via SMTP or SendGrid
- Templates: certification expiry warning, supplier invitation, user registration

---

## 7. Use Cases

### UC-01: Onboard a New Supplier

**Actor:** Org Admin
**Precondition:** Org Admin is authenticated; the supplier does not yet exist in the system.
**Main Flow:**
1. Org Admin navigates to "Suppliers" → "Invite Supplier"
2. Enters supplier legal name and admin email
3. System generates a 7-day invitation token and sends an email
4. Supplier Admin clicks link, completes registration form
5. System creates new Tenant + Organization record
6. System creates a directed edge: `[Buyer Org] → [Supplier Org]`
7. Org Admin receives confirmation

**Exception:** If invitation token is expired → system returns `INVITATION_EXPIRED` and Org Admin must re-issue.

---

### UC-02: Upload Certification Document

**Actor:** Compliance Officer or Org Admin
**Precondition:** User is authenticated; organization is Active.
**Main Flow:**
1. User navigates to "Documents" → "Upload Certification"
2. Selects file (PDF) and document type
3. Sets expiry date
4. System validates file type and size
5. File Microservice computes SHA-256 hash
6. File stored in blob storage; metadata stored in Core API database
7. Audit log entry created
8. User receives `documentId` and confirmation

**Exception:** File exceeds 50 MB → `FILE_TOO_LARGE` error returned.

---

### UC-03: Run a Trace-Back Report

**Actor:** Org Admin or Compliance Officer
**Precondition:** Supply chain graph has at least one edge.
**Main Flow:**
1. User selects a Product and clicks "Generate Trace"
2. System executes recursive CTE from product's organization node
3. System collects all nodes, edges, materials, and certifications in scope
4. System calculates compliance score
5. Result rendered as interactive supply chain graph in UI
6. User may export as JSON (DPP) or PDF

---

### UC-04: Detect Circular Dependency

**Actor:** System (triggered by Org Admin attempting to add a supplier edge)
**Precondition:** Edge creation is requested.
**Main Flow:**
1. Org Admin adds a new supplier relationship
2. System runs cycle detection before persisting the edge
3. If a cycle is found, system rejects with `CYCLE_DETECTED` + the path that would form the cycle
4. If no cycle, edge is persisted and audit log entry created

---

### UC-05: Verify Audit Log Integrity

**Actor:** Platform Admin or Auditor
**Precondition:** Audit entries exist for the target entity.
**Main Flow:**
1. Auditor requests integrity check for an entity
2. System iterates all audit entries in chronological order
3. For each entry, recomputes the chain hash
4. Compares recomputed hash to stored hash
5. Reports pass/fail for each entry
6. If any entry fails → `AUDIT_INTEGRITY_VIOLATION` reported with entry index

---

## 8. Acceptance Criteria

| Req ID | Acceptance Criterion |
|--------|----------------------|
| FR-01 | Given a valid invitation link, a new organization is created and linked as a supplier within 60 seconds |
| FR-02 | Given an edge that would create a cycle, the system rejects it with `CYCLE_DETECTED` in < 500ms |
| FR-03 | Given a product BOM with 5 levels of sub-components, the BOM explosion query returns all leaf materials with correct quantities |
| FR-04 | Given a supply chain of depth 15, trace-back completes and returns full graph in < 5 seconds |
| FR-05 | Given a PDF upload, the system stores and returns the correct SHA-256 hash; a tampered download is rejected |
| FR-06 | Given a Supplier Rep JWT, API calls to another tenant's resources return HTTP 403 |
| FR-07 | Given 100 audit entries, the chain integrity check returns pass; modifying a stored hash causes the check to return fail at the modified entry |
| FR-08 | Given a product with full supply chain data, a DPP PDF is generated with correct provenance data and a valid QR code |
| NFR-SEC-03 | JWT tokens expire after 15 minutes; refresh tokens expire after 7 days |
| NFR-SEC-06 | Exceeding 100 requests/minute per user returns HTTP 429 |
