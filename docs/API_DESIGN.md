# API Design Document
## Echo-Trace: Tiered Supply Chain Transparency & Audit Platform

**Version:** 1.0.0
**Date:** 2026-02-26
**Base URL:** `https://api.echo-trace.com/api/v1`

---

## Table of Contents

1. [Conventions](#1-conventions)
2. [Authentication](#2-authentication)
3. [Organizations API](#3-organizations-api)
4. [Supply Chain API](#4-supply-chain-api)
5. [BOM API](#5-bom-api)
6. [Documents API](#6-documents-api)
7. [Digital Product Passport API](#7-digital-product-passport-api)
8. [Audit API](#8-audit-api)
9. [Admin API](#9-admin-api)
10. [File Service API](#10-file-service-api)
11. [Error Reference](#11-error-reference)

---

## 1. Conventions

### 1.1 Request/Response Format
- All request and response bodies are `application/json` unless noted (file upload uses `multipart/form-data`)
- All timestamps are ISO 8601 UTC strings: `"2026-02-26T14:30:00Z"`
- All IDs are UUIDs in hyphenated lowercase format: `"3fa85f64-5717-4562-b3fc-2c963f66afa6"`

### 1.2 Pagination
All list endpoints support cursor-based pagination:

```
GET /organizations?pageSize=20&cursor=<opaque_cursor>
```

Response includes:
```json
{
  "data": [...],
  "pagination": {
    "nextCursor": "eyJpZCI6IjEyMyJ9",
    "hasMore": true,
    "totalCount": 150
  }
}
```

### 1.3 Filtering and Sorting

```
GET /organizations?status=Active&country=US&sortBy=createdAt&sortDir=desc
```

### 1.4 HTTP Status Codes

| Code | Meaning |
|------|---------|
| 200 | OK |
| 201 | Created |
| 204 | No Content (successful delete) |
| 400 | Bad Request — validation error |
| 401 | Unauthorized — missing or invalid JWT |
| 403 | Forbidden — insufficient role/tenant |
| 404 | Not Found |
| 409 | Conflict — duplicate resource |
| 422 | Unprocessable Entity — business rule violation |
| 429 | Too Many Requests — rate limit exceeded |
| 500 | Internal Server Error |

### 1.5 Error Format (RFC 7807)

```json
{
  "type": "https://echo-trace.com/errors/cycle-detected",
  "title": "Circular dependency detected",
  "status": 422,
  "detail": "Adding Org B as a supplier of Org A would create a cycle: A → B → C → A",
  "instance": "/api/v1/supply-chain/edges",
  "traceId": "00-a1b2c3d4e5f6...",
  "errors": {}
}
```

---

## 2. Authentication

### POST /auth/register

Create a new user account. Used during supplier onboarding via invitation.

**Request:**
```json
{
  "invitationToken": "550e8400-e29b-41d4-a716-446655440000",
  "email": "admin@supplier-co.com",
  "password": "SecureP@ss123",
  "firstName": "Jane",
  "lastName": "Smith",
  "orgLegalName": "Supplier Co. Ltd",
  "orgCountry": "CA"
}
```

**Response 201:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "orgId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
  "tenantId": "1b914cfe-a81f-4ece-aa93-6bc7f1e5a3d2",
  "email": "admin@supplier-co.com",
  "role": "ORG_ADMIN"
}
```

---

### POST /auth/login

**Request:**
```json
{
  "email": "user@example.com",
  "password": "SecureP@ss123"
}
```

**Response 200:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

Refresh token set as HTTP-only cookie `echo_rt`.

---

### POST /auth/refresh

Reads refresh token from HTTP-only cookie automatically.

**Response 200:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 900,
  "tokenType": "Bearer"
}
```

---

### POST /auth/logout

Revokes the current refresh token.

**Response 204** (no body)

---

### POST /auth/invite

Generate an invitation link for a new supplier.

**Required Role:** `ORG_ADMIN`

**Request:**
```json
{
  "targetEmail": "contact@new-supplier.com",
  "supplierLegalName": "New Supplier LLC"
}
```

**Response 201:**
```json
{
  "invitationId": "...",
  "invitationLink": "https://app.echo-trace.com/register?token=550e8400...",
  "expiresAt": "2026-03-05T14:00:00Z"
}
```

---

## 3. Organizations API

### GET /organizations

List organizations within the caller's tenant.

**Query params:** `status`, `country`, `industrySector`, `pageSize`, `cursor`, `sortBy`, `sortDir`

**Response 200:**
```json
{
  "data": [
    {
      "orgId": "7c9e6679-...",
      "tenantId": "...",
      "legalName": "Acme Corp",
      "country": "US",
      "industrySector": "Electronics",
      "tierLevel": 1,
      "esgRiskScore": 72,
      "status": "Active",
      "createdAt": "2026-01-15T09:00:00Z"
    }
  ],
  "pagination": { "nextCursor": null, "hasMore": false, "totalCount": 1 }
}
```

---

### POST /organizations

Create a new organization (Platform Admin only — normal users join via invitation).

**Required Role:** `PLATFORM_ADMIN`

**Request:**
```json
{
  "legalName": "Buying Corp Inc.",
  "country": "US",
  "industrySector": "Retail",
  "contactEmail": "ops@buyingcorp.com",
  "registrationNumber": "EIN-12-3456789"
}
```

**Response 201:**
```json
{
  "orgId": "...",
  "tenantId": "...",
  "legalName": "Buying Corp Inc.",
  "status": "Pending"
}
```

---

### GET /organizations/{orgId}

Get full organization details.

**Response 200:**
```json
{
  "orgId": "7c9e6679-...",
  "tenantId": "...",
  "legalName": "Acme Corp",
  "country": "US",
  "industrySector": "Electronics",
  "registrationNumber": "12-3456789",
  "contactEmail": "contact@acme.com",
  "address": {
    "street": "123 Main St",
    "city": "New York",
    "state": "NY",
    "postalCode": "10001"
  },
  "tierLevel": 1,
  "esgRiskScore": 72,
  "status": "Active",
  "documentsCount": 5,
  "hasExpiredDocuments": false,
  "createdAt": "2026-01-15T09:00:00Z",
  "updatedAt": "2026-02-10T12:00:00Z"
}
```

---

### PUT /organizations/{orgId}

Update organization metadata.

**Required Role:** `ORG_ADMIN` (own org) or `PLATFORM_ADMIN`

**Request:**
```json
{
  "contactEmail": "newemail@acme.com",
  "esgRiskScore": 65,
  "address": { "street": "456 New Ave", "city": "Los Angeles", "state": "CA", "postalCode": "90001" }
}
```

**Response 200:** Returns updated organization object.

---

### PATCH /organizations/{orgId}/status

Change organization status.

**Required Role:** `PLATFORM_ADMIN`

**Request:**
```json
{ "status": "Suspended", "reason": "Audit non-compliance" }
```

**Response 200:** Returns updated organization object.

---

## 4. Supply Chain API

### GET /supply-chain/tree/{orgId}

Get the downstream supplier tree from a given organization.

**Query params:**
- `depth` (int, default: -1 = unlimited)
- `activeOnly` (bool, default: true)

**Response 200:**
```json
{
  "rootOrgId": "7c9e6679-...",
  "generatedAt": "2026-02-26T14:00:00Z",
  "totalNodes": 42,
  "maxDepthFound": 6,
  "nodes": [
    {
      "orgId": "...",
      "legalName": "Tier 1 Supplier",
      "country": "MX",
      "depth": 1,
      "status": "Active",
      "complianceStatus": "COMPLIANT",
      "path": "RootOrg → Tier1Supplier"
    }
  ],
  "edges": [
    {
      "edgeId": "...",
      "parentOrgId": "...",
      "childOrgId": "...",
      "relationshipType": "SOURCES",
      "isActive": true
    }
  ]
}
```

---

### POST /supply-chain/edges

Add a new supplier relationship.

**Required Role:** `ORG_ADMIN`

**Request:**
```json
{
  "childOrgId": "abc12345-...",
  "materialId": null,
  "relationshipType": "SOURCES",
  "validFrom": "2026-03-01"
}
```

**Response 201:**
```json
{
  "edgeId": "...",
  "parentOrgId": "...",
  "childOrgId": "...",
  "relationshipType": "SOURCES",
  "validFrom": "2026-03-01",
  "isActive": true,
  "createdAt": "2026-02-26T14:05:00Z"
}
```

**Error 422 (cycle detected):**
```json
{
  "type": "https://echo-trace.com/errors/cycle-detected",
  "title": "Circular dependency detected",
  "status": 422,
  "detail": "Adding this edge creates a cycle: OrgA → OrgB → OrgC → OrgA"
}
```

---

### DELETE /supply-chain/edges/{edgeId}

Deactivate (soft-delete) a supplier relationship.

**Required Role:** `ORG_ADMIN`

**Response 204**

---

### GET /supply-chain/trace/{orgId}

Full trace-back report for an organization — returns all contributing orgs, materials, and certifications.

**Query params:**
- `depth` (int, default: -1)
- `productId` (UUID, optional — scope to a specific product)

**Response 200:**
```json
{
  "orgId": "...",
  "productId": null,
  "generatedAt": "2026-02-26T14:10:00Z",
  "complianceScore": 85,
  "summary": {
    "totalOrgs": 18,
    "compliantOrgs": 15,
    "orgsWithExpiredCerts": 2,
    "orgsWithNoCerts": 1,
    "deepestTier": 6
  },
  "traceGraph": {
    "nodes": [...],
    "edges": [...]
  },
  "certifications": [
    {
      "documentId": "...",
      "orgId": "...",
      "documentType": "ISO_14001",
      "status": "Active",
      "expiresAt": "2027-01-01"
    }
  ]
}
```

---

### POST /supply-chain/cycle-check

Pre-flight cycle check without persisting the edge.

**Request:**
```json
{
  "parentOrgId": "...",
  "childOrgId": "..."
}
```

**Response 200:**
```json
{
  "wouldCreateCycle": false,
  "cyclePath": null
}
```

---

## 5. BOM API

### GET /products

List products for the calling tenant's organization.

**Response 200:** Paginated list of products.

---

### POST /products

Create a new product.

**Required Role:** `ORG_ADMIN`

**Request:**
```json
{
  "name": "EV Battery Pack Model X",
  "sku": "BATT-X-001",
  "description": "72V lithium battery assembly"
}
```

**Response 201:** Product object with `productId`.

---

### GET /products/{productId}/bom

Get the BOM for a specific product version.

**Query params:** `version` (int, defaults to current version)

**Response 200:**
```json
{
  "productId": "...",
  "productName": "EV Battery Pack Model X",
  "bomVersion": 3,
  "entries": [
    {
      "bomEntryId": "...",
      "materialId": "...",
      "materialName": "Lithium Carbonate",
      "parentBomEntryId": null,
      "quantity": 10.5,
      "unitOfMeasure": "kg",
      "level": 0,
      "children": [...]
    }
  ]
}
```

---

### POST /products/{productId}/bom

Add a BOM entry to a product's current draft version.

**Required Role:** `ORG_ADMIN`

**Request:**
```json
{
  "materialId": "...",
  "parentBomEntryId": null,
  "quantity": 10.5,
  "unitOfMeasure": "kg",
  "notes": "Primary active material"
}
```

**Response 201:** New BomEntry object.

---

### POST /products/{productId}/bom/publish

Publish the current draft BOM as a new immutable version.

**Required Role:** `ORG_ADMIN`

**Response 200:**
```json
{
  "productId": "...",
  "publishedVersion": 4,
  "publishedAt": "2026-02-26T15:00:00Z"
}
```

---

### GET /products/{productId}/bom/explode

Run a BOM explosion to get all leaf-level materials with quantities.

**Query params:** `version` (int, defaults to current)

**Response 200:**
```json
{
  "productId": "...",
  "bomVersion": 3,
  "leafMaterials": [
    {
      "materialId": "...",
      "materialName": "Lithium Carbonate",
      "hsTariffCode": "2836.20",
      "countryOfOrigin": "CL",
      "totalQuantity": 10.5,
      "unitOfMeasure": "kg",
      "level": 0,
      "isReachRegulated": true
    }
  ]
}
```

---

### GET /materials

List materials for the tenant.

**Response 200:** Paginated list of materials.

---

### POST /materials

Create a new material.

**Required Role:** `ORG_ADMIN` or `COMPLIANCE_OFFICER`

**Request:**
```json
{
  "name": "Lithium Carbonate",
  "hsTariffCode": "2836.20",
  "unitOfMeasure": "kg",
  "countryOfOrigin": "CL",
  "isReachRegulated": true,
  "isRohsRegulated": false,
  "hazardClassification": "GHS05"
}
```

**Response 201:** Material object.

---

## 6. Documents API

### GET /documents

List certification documents for an organization.

**Query params:** `orgId` (required), `documentType`, `status`, `pageSize`, `cursor`

**Response 200:** Paginated list of document metadata objects.

---

### POST /documents/upload-request

Initiate a document upload. Returns a signed upload URL or triggers the File Service flow.

**Required Role:** `ORG_ADMIN` or `COMPLIANCE_OFFICER`

**Request:**
```json
{
  "orgId": "...",
  "documentType": "ISO_14001",
  "originalFileName": "iso14001-cert-2026.pdf",
  "issuedAt": "2026-01-01",
  "expiresAt": "2027-01-01"
}
```

**Response 200:**
```json
{
  "uploadRequestId": "...",
  "uploadUrl": "http://fileservice/upload",
  "uploadHeaders": {
    "X-Upload-Request-Id": "...",
    "X-Internal-Key": "..."
  },
  "expiresAt": "2026-02-26T14:20:00Z"
}
```

---

### POST /documents/confirm-upload

Called by the client after the File Service upload succeeds. Registers the document in the Core API database.

**Request:**
```json
{
  "uploadRequestId": "...",
  "blobPath": "tenants/abc/docs/iso14001-cert-2026.pdf",
  "contentHash": "a3f5b7c9d1e2f4...",
  "fileSizeBytes": 204800
}
```

**Response 201:**
```json
{
  "documentId": "...",
  "contentHash": "a3f5b7c9d1e2f4...",
  "status": "Active",
  "uploadedAt": "2026-02-26T14:21:00Z"
}
```

---

### GET /documents/{documentId}

Get document metadata.

**Response 200:** Full document metadata object.

---

### GET /documents/{documentId}/download

Get a time-limited signed download URL. The system re-verifies the SHA-256 hash before issuing the URL.

**Response 200:**
```json
{
  "downloadUrl": "https://blob.example.com/...",
  "expiresAt": "2026-02-26T14:35:00Z",
  "contentHash": "a3f5b7c9d1e2f4...",
  "integrityVerified": true
}
```

**Error 422 (hash mismatch):**
```json
{
  "type": "https://echo-trace.com/errors/document-integrity-violation",
  "title": "Document integrity check failed",
  "status": 422,
  "detail": "The stored file hash does not match the blob content. Document may have been tampered with."
}
```

---

### DELETE /documents/{documentId}

Revoke (soft-delete) a document.

**Required Role:** `ORG_ADMIN`

**Response 204**

---

## 7. Digital Product Passport API

### GET /dpp/{productId}

Generate a Digital Product Passport for a product.

**Query params:** `format` (`json` | `pdf`, default `json`)

**For JSON format — Response 200:**
```json
{
  "passportId": "...",
  "schemaVersion": "1.0",
  "generatedAt": "2026-02-26T15:00:00Z",
  "signature": "hmac-sha256-hex-value",
  "product": {
    "productId": "...",
    "name": "EV Battery Pack Model X",
    "sku": "BATT-X-001",
    "manufacturingOrg": { ... }
  },
  "bomSummary": {
    "version": 3,
    "leafMaterials": [...]
  },
  "supplyChain": {
    "totalOrgs": 18,
    "maxDepth": 6,
    "complianceScore": 85,
    "nodes": [...],
    "edges": [...]
  },
  "certifications": [...],
  "complianceFlags": {
    "hasReachMaterials": true,
    "hasRohsMaterials": false,
    "allTier1CertsCurrent": true
  }
}
```

**For PDF format:** Returns `Content-Type: application/pdf` with the passport document.

---

### GET /dpp/{productId}/verify

Verify the HMAC signature of a previously generated DPP.

**Request:**
```json
{ "signature": "hmac-sha256-hex-value", "payload": "..." }
```

**Response 200:**
```json
{ "valid": true, "generatedAt": "2026-02-26T15:00:00Z" }
```

---

## 8. Audit API

### GET /audit/logs

Query audit log entries.

**Required Role:** `ORG_ADMIN`, `COMPLIANCE_OFFICER`, or `PLATFORM_ADMIN`

**Query params:** `entityType`, `entityId`, `action`, `userId`, `from`, `to`, `pageSize`, `cursor`

**Response 200:**
```json
{
  "data": [
    {
      "auditId": 1024,
      "entityType": "Document",
      "entityId": "...",
      "action": "CREATE",
      "oldValue": null,
      "newValue": { "documentType": "ISO_14001", "status": "Active" },
      "performedByUserId": "...",
      "performedByEmail": "user@example.com",
      "occurredAt": "2026-02-26T14:21:00Z",
      "chainHash": "a1b2c3..."
    }
  ],
  "pagination": { ... }
}
```

---

### GET /audit/verify/{entityType}/{entityId}

Verify chain integrity of all audit entries for a given entity.

**Required Role:** `PLATFORM_ADMIN` or `AUDITOR`

**Response 200:**
```json
{
  "entityType": "Document",
  "entityId": "...",
  "totalEntries": 12,
  "integrityStatus": "PASS",
  "firstAuditAt": "2026-01-15T09:00:00Z",
  "lastAuditAt": "2026-02-26T14:21:00Z",
  "violations": []
}
```

**Response 200 (with violation):**
```json
{
  "integrityStatus": "FAIL",
  "violations": [
    {
      "auditId": 1028,
      "reason": "Computed hash does not match stored hash",
      "occurredAt": "2026-02-10T08:00:00Z"
    }
  ]
}
```

---

## 9. Admin API

*All endpoints in this group require `PLATFORM_ADMIN` role.*

### GET /admin/tenants — List all tenants
### POST /admin/tenants — Create root tenant
### PATCH /admin/tenants/{tenantId}/status — Suspend/activate a tenant
### GET /admin/users — List users across tenants
### DELETE /admin/users/{userId} — Deactivate a user

---

## 10. File Service API

*Internal API — not exposed to the public internet. Called only by the Core API with `X-Internal-Key` header.*

**Base URL:** `http://fileservice:3000` (internal Docker network)

### POST /upload

Upload a file and receive its hash and storage path.

**Headers:**
```
Content-Type: multipart/form-data
X-Internal-Key: <shared secret>
X-Upload-Request-Id: <uuid>
```

**Body:** `file` (binary), `mimeType` (string), `maxSizeBytes` (number)

**Response 200:**
```json
{
  "blobPath": "tenants/abc/docs/iso14001-2026.pdf",
  "contentHash": "a3f5b7c9d1e2...",
  "fileSizeBytes": 204800,
  "mimeType": "application/pdf"
}
```

**Error 413:** File exceeds `maxSizeBytes`
**Error 415:** Unsupported MIME type

---

### GET /file/:blobPath

Retrieve a file from blob storage.

**Headers:** `X-Internal-Key: <shared secret>`

**Response 200:** File binary stream with correct Content-Type.

---

### GET /health

**Response 200:**
```json
{
  "status": "healthy",
  "storage": "connected",
  "uptime": 3600
}
```

---

## 11. Error Reference

| Error Type | HTTP Status | Description |
|-----------|-------------|-------------|
| `validation-error` | 400 | Request body failed validation |
| `unauthorized` | 401 | Missing or expired JWT |
| `forbidden` | 403 | Insufficient role or cross-tenant access |
| `not-found` | 404 | Resource does not exist |
| `conflict` | 409 | Duplicate resource (e.g., same edge already exists) |
| `cycle-detected` | 422 | Edge would create a circular dependency |
| `document-integrity-violation` | 422 | File hash mismatch |
| `audit-integrity-violation` | 422 | Audit chain hash mismatch |
| `invitation-expired` | 422 | Registration attempt with expired token |
| `invitation-already-used` | 422 | Invitation token was already consumed |
| `bom-version-published` | 422 | Cannot modify a published BOM version |
| `rate-limit-exceeded` | 429 | Too many requests |
| `internal-server-error` | 500 | Unexpected server error |
