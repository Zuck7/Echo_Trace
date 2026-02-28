# Data Model Document
## Echo-Trace: Tiered Supply Chain Transparency & Audit Platform

**Version:** 1.0.0
**Date:** 2026-02-26

---

## Table of Contents

1. [Overview](#1-overview)
2. [Entity-Relationship Diagram](#2-entity-relationship-diagram)
3. [Table Definitions](#3-table-definitions)
4. [Graph Schema Design](#4-graph-schema-design)
5. [Key SQL Patterns](#5-key-sql-patterns)
6. [Indexes & Constraints](#6-indexes--constraints)
7. [Multi-Tenancy Strategy](#7-multi-tenancy-strategy)

---

## 1. Overview

Echo-Trace uses a **Graph-Based Relational Schema** implemented on SQL Server 2022. The supply chain is modeled as a directed acyclic graph (DAG) using standard relational tables (`Organizations` as nodes, `SupplyChainEdges` as directed edges). This enables high-performance recursive CTE queries without requiring a dedicated graph database.

All tables with tenant-scoped data include a `TenantID` foreign key. EF Core Global Query Filters enforce row-level tenant isolation automatically.

---

## 2. Entity-Relationship Diagram

```
┌──────────────┐       ┌──────────────────────────┐
│   Tenants    │       │         Users             │
│──────────────│       │──────────────────────────│
│ TenantID (PK)│◄──────│ UserID (PK)              │
│ Name         │  1:N  │ TenantID (FK)             │
│ PlanTier     │       │ OrgID (FK)                │
│ CreatedAt    │       │ Email                     │
│ Status       │       │ PasswordHash              │
└──────┬───────┘       │ Role                      │
       │               │ CreatedAt                 │
       │ 1:N           │ IsActive                  │
       ▼               └──────────────────────────┘
┌──────────────────────────────────────────────────┐
│                  Organizations                    │
│──────────────────────────────────────────────────│
│ OrgID (PK)                                        │
│ TenantID (FK)                                     │
│ LegalName                                         │
│ Country                                           │
│ IndustrySector                                    │
│ RegistrationNumber                                │
│ ContactEmail                                      │
│ Address (JSON)                                    │
│ TierLevel (computed)                              │
│ EsgRiskScore                                      │
│ Status [Active|Suspended|Pending]                 │
│ CreatedAt / UpdatedAt                             │
└───────┬──────────────────────┬────────────────────┘
        │                      │
        │ ParentOrgID           │ ChildOrgID
        │                      │
        ▼                      ▼
┌────────────────────────────────────────────────────┐
│                SupplyChainEdges                     │
│────────────────────────────────────────────────────│
│ EdgeID (PK)                                         │
│ TenantID (FK)                                       │
│ ParentOrgID (FK → Organizations)                    │
│ ChildOrgID (FK → Organizations)                     │
│ MaterialID (FK → Materials, nullable)               │
│ RelationshipType [SOURCES|MANUFACTURES|DISTRIBUTES] │
│ ValidFrom                                           │
│ ValidTo (nullable)                                  │
│ IsActive                                            │
│ CreatedAt                                           │
└────────────────────────────────────────────────────┘
        │
        │ MaterialID
        ▼
┌───────────────────────┐     ┌───────────────────────┐
│       Materials       │     │       Products        │
│───────────────────────│     │───────────────────────│
│ MaterialID (PK)       │     │ ProductID (PK)        │
│ TenantID (FK)         │     │ TenantID (FK)         │
│ Name                  │     │ OrgID (FK)            │
│ Description           │     │ Name                  │
│ HsTariffCode          │     │ SKU                   │
│ UnitOfMeasure         │     │ Description           │
│ CountryOfOrigin       │     │ CurrentBomVersion     │
│ IsReachRegulated      │     │ CreatedAt             │
│ IsRohsRegulated       │     └──────────┬────────────┘
│ HazardClassification  │                │
│ CreatedAt             │                │ 1:N
└───────────────────────┘                ▼
        ▲                    ┌───────────────────────┐
        │ MaterialID         │       BomEntries      │
        │                    │───────────────────────│
        └────────────────────│ BomEntryID (PK)       │
                             │ TenantID (FK)         │
                             │ ProductID (FK)        │
                             │ MaterialID (FK)       │
                             │ ParentBomEntryID (FK) │◄─┐
                             │ BomVersion            │  │ self-ref
                             │ Quantity              │  │
                             │ UnitOfMeasure         │  │
                             │ Notes                 │  │
                             │ CreatedAt             │  │
                             └───────────────────────┘──┘

┌───────────────────────────────────────────────────┐
│                    Documents                       │
│───────────────────────────────────────────────────│
│ DocumentID (PK)                                    │
│ TenantID (FK)                                      │
│ OrgID (FK → Organizations)                         │
│ DocumentType [ISO_14001|ISO_45001|ESG_REPORT|...]  │
│ OriginalFileName                                   │
│ BlobPath                                           │
│ ContentHash (SHA-256)                              │
│ FileSizeBytes                                      │
│ MimeType                                           │
│ IssuedAt                                           │
│ ExpiresAt                                          │
│ Status [Active|Expired|Revoked]                    │
│ UploadedByUserID (FK → Users)                      │
│ UploadedAt                                         │
└───────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────┐
│                     AuditLogEntries                     │
│────────────────────────────────────────────────────────│
│ AuditID (PK, BIGINT IDENTITY)                           │
│ TenantID (FK)                                           │
│ EntityType (VARCHAR 100)                                │
│ EntityID (VARCHAR 100)                                  │
│ Action [CREATE|UPDATE|DELETE|VERIFY]                    │
│ OldValue (NVARCHAR MAX, JSON)                           │
│ NewValue (NVARCHAR MAX, JSON)                           │
│ PerformedByUserID (FK → Users, nullable)                │
│ PerformedByIpAddress (VARCHAR 45)                       │
│ OccurredAt (DATETIME2, UTC)                             │
│ ChainHash (CHAR 64) ← SHA-256 chained                   │
└────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────┐
│                 InvitationTokens                        │
│────────────────────────────────────────────────────────│
│ TokenID (PK)                                            │
│ IssuingOrgID (FK → Organizations)                       │
│ IssuingTenantID (FK → Tenants)                          │
│ TargetEmail                                             │
│ Token (UNIQUEIDENTIFIER)                                │
│ ExpiresAt (DATETIME2)                                   │
│ UsedAt (DATETIME2, nullable)                            │
│ CreatedAt                                               │
└────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────┐
│                   RefreshTokens                         │
│────────────────────────────────────────────────────────│
│ TokenID (PK)                                            │
│ UserID (FK → Users)                                     │
│ TokenHash (CHAR 64, SHA-256 of raw token)               │
│ ExpiresAt                                               │
│ RevokedAt (nullable)                                    │
│ CreatedAt                                               │
└────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────┐
│                  AuditorScopes                          │
│────────────────────────────────────────────────────────│
│ ScopeID (PK)                                            │
│ AuditorUserID (FK → Users)                              │
│ RootOrgID (FK → Organizations)                          │
│ MaxDepth (INT, -1 = unlimited)                          │
│ GrantedByUserID (FK → Users)                            │
│ ExpiresAt (nullable)                                    │
│ CreatedAt                                               │
└────────────────────────────────────────────────────────┘
```

---

## 3. Table Definitions

### 3.1 Tenants

```sql
CREATE TABLE Tenants (
    TenantID    UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    Name        NVARCHAR(200)    NOT NULL,
    PlanTier    VARCHAR(50)      NOT NULL DEFAULT 'STANDARD',
    CreatedAt   DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    Status      VARCHAR(20)      NOT NULL DEFAULT 'Active'
                                 CHECK (Status IN ('Active', 'Suspended', 'Archived')),
    CONSTRAINT PK_Tenants PRIMARY KEY (TenantID)
);
```

### 3.2 Organizations

```sql
CREATE TABLE Organizations (
    OrgID              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantID           UNIQUEIDENTIFIER NOT NULL,
    LegalName          NVARCHAR(300)    NOT NULL,
    Country            CHAR(2)          NOT NULL,   -- ISO 3166-1 alpha-2
    IndustrySector     VARCHAR(100)     NULL,
    RegistrationNumber VARCHAR(100)     NULL,
    ContactEmail       NVARCHAR(320)    NOT NULL,
    Address            NVARCHAR(MAX)    NULL,        -- JSON
    EsgRiskScore       TINYINT          NULL,        -- 1-100
    Status             VARCHAR(20)      NOT NULL DEFAULT 'Pending'
                                        CHECK (Status IN ('Active', 'Suspended', 'Pending')),
    CreatedAt          DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt          DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Organizations PRIMARY KEY (OrgID),
    CONSTRAINT FK_Organizations_Tenants FOREIGN KEY (TenantID) REFERENCES Tenants(TenantID)
);
```

### 3.3 SupplyChainEdges

```sql
CREATE TABLE SupplyChainEdges (
    EdgeID           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantID         UNIQUEIDENTIFIER NOT NULL,
    ParentOrgID      UNIQUEIDENTIFIER NOT NULL,   -- The buyer/customer
    ChildOrgID       UNIQUEIDENTIFIER NOT NULL,   -- The supplier
    MaterialID       UNIQUEIDENTIFIER NULL,
    RelationshipType VARCHAR(30)      NOT NULL DEFAULT 'SOURCES'
                                      CHECK (RelationshipType IN ('SOURCES', 'MANUFACTURES', 'DISTRIBUTES')),
    ValidFrom        DATE             NOT NULL DEFAULT CAST(SYSUTCDATETIME() AS DATE),
    ValidTo          DATE             NULL,
    IsActive         BIT              NOT NULL DEFAULT 1,
    CreatedAt        DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_SupplyChainEdges   PRIMARY KEY (EdgeID),
    CONSTRAINT FK_Edges_Tenant       FOREIGN KEY (TenantID)    REFERENCES Tenants(TenantID),
    CONSTRAINT FK_Edges_ParentOrg    FOREIGN KEY (ParentOrgID) REFERENCES Organizations(OrgID),
    CONSTRAINT FK_Edges_ChildOrg     FOREIGN KEY (ChildOrgID)  REFERENCES Organizations(OrgID),
    CONSTRAINT FK_Edges_Material     FOREIGN KEY (MaterialID)  REFERENCES Materials(MaterialID),
    CONSTRAINT CK_Edges_NoSelfRef    CHECK (ParentOrgID <> ChildOrgID)
);
```

### 3.4 Materials

```sql
CREATE TABLE Materials (
    MaterialID           UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantID             UNIQUEIDENTIFIER NOT NULL,
    Name                 NVARCHAR(300)    NOT NULL,
    Description          NVARCHAR(MAX)    NULL,
    HsTariffCode         VARCHAR(20)      NULL,
    UnitOfMeasure        VARCHAR(30)      NOT NULL DEFAULT 'kg',
    CountryOfOrigin      CHAR(2)          NULL,
    IsReachRegulated     BIT              NOT NULL DEFAULT 0,
    IsRohsRegulated      BIT              NOT NULL DEFAULT 0,
    HazardClassification VARCHAR(100)     NULL,
    CreatedAt            DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Materials PRIMARY KEY (MaterialID),
    CONSTRAINT FK_Materials_Tenants FOREIGN KEY (TenantID) REFERENCES Tenants(TenantID)
);
```

### 3.5 Products

```sql
CREATE TABLE Products (
    ProductID          UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantID           UNIQUEIDENTIFIER NOT NULL,
    OrgID              UNIQUEIDENTIFIER NOT NULL,
    Name               NVARCHAR(300)    NOT NULL,
    SKU                VARCHAR(100)     NULL,
    Description        NVARCHAR(MAX)    NULL,
    CurrentBomVersion  INT              NOT NULL DEFAULT 0,
    CreatedAt          DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Products    PRIMARY KEY (ProductID),
    CONSTRAINT FK_Products_T  FOREIGN KEY (TenantID) REFERENCES Tenants(TenantID),
    CONSTRAINT FK_Products_O  FOREIGN KEY (OrgID)    REFERENCES Organizations(OrgID)
);
```

### 3.6 BomEntries

```sql
CREATE TABLE BomEntries (
    BomEntryID       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantID         UNIQUEIDENTIFIER NOT NULL,
    ProductID        UNIQUEIDENTIFIER NOT NULL,
    MaterialID       UNIQUEIDENTIFIER NOT NULL,
    ParentBomEntryID UNIQUEIDENTIFIER NULL,        -- NULL = root-level entry
    BomVersion       INT              NOT NULL,
    Quantity         DECIMAL(18, 6)   NOT NULL,
    UnitOfMeasure    VARCHAR(30)      NOT NULL,
    Notes            NVARCHAR(MAX)    NULL,
    CreatedAt        DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_BomEntries          PRIMARY KEY (BomEntryID),
    CONSTRAINT FK_Bom_Tenant          FOREIGN KEY (TenantID)         REFERENCES Tenants(TenantID),
    CONSTRAINT FK_Bom_Product         FOREIGN KEY (ProductID)        REFERENCES Products(ProductID),
    CONSTRAINT FK_Bom_Material        FOREIGN KEY (MaterialID)       REFERENCES Materials(MaterialID),
    CONSTRAINT FK_Bom_Parent          FOREIGN KEY (ParentBomEntryID) REFERENCES BomEntries(BomEntryID),
    CONSTRAINT CK_Bom_NoSelfRef       CHECK (BomEntryID <> ParentBomEntryID)
);
```

### 3.7 Documents

```sql
CREATE TABLE Documents (
    DocumentID         UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
    TenantID           UNIQUEIDENTIFIER NOT NULL,
    OrgID              UNIQUEIDENTIFIER NOT NULL,
    DocumentType       VARCHAR(50)      NOT NULL,
    OriginalFileName   NVARCHAR(500)    NOT NULL,
    BlobPath           NVARCHAR(1000)   NOT NULL,
    ContentHash        CHAR(64)         NOT NULL,  -- SHA-256 hex
    FileSizeBytes      BIGINT           NOT NULL,
    MimeType           VARCHAR(100)     NOT NULL,
    IssuedAt           DATE             NULL,
    ExpiresAt          DATE             NULL,
    Status             VARCHAR(20)      NOT NULL DEFAULT 'Active'
                                        CHECK (Status IN ('Active', 'Expired', 'Revoked')),
    UploadedByUserID   UNIQUEIDENTIFIER NULL,
    UploadedAt         DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Documents       PRIMARY KEY (DocumentID),
    CONSTRAINT FK_Docs_Tenant     FOREIGN KEY (TenantID)         REFERENCES Tenants(TenantID),
    CONSTRAINT FK_Docs_Org        FOREIGN KEY (OrgID)            REFERENCES Organizations(OrgID),
    CONSTRAINT FK_Docs_User       FOREIGN KEY (UploadedByUserID) REFERENCES Users(UserID)
);
```

### 3.8 AuditLogEntries

```sql
CREATE TABLE AuditLogEntries (
    AuditID             BIGINT           NOT NULL IDENTITY(1,1),
    TenantID            UNIQUEIDENTIFIER NOT NULL,
    EntityType          VARCHAR(100)     NOT NULL,
    EntityID            VARCHAR(100)     NOT NULL,
    Action              VARCHAR(20)      NOT NULL
                                         CHECK (Action IN ('CREATE','UPDATE','DELETE','VERIFY','LOGIN','LOGOUT')),
    OldValue            NVARCHAR(MAX)    NULL,       -- JSON
    NewValue            NVARCHAR(MAX)    NULL,       -- JSON
    PerformedByUserID   UNIQUEIDENTIFIER NULL,
    PerformedByIpAddress VARCHAR(45)     NULL,
    OccurredAt          DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    ChainHash           CHAR(64)         NOT NULL,   -- SHA-256 chain
    CONSTRAINT PK_AuditLog PRIMARY KEY (AuditID)
    -- No FK on TenantID intentionally: audit log survives tenant deletion
);

-- Prevent UPDATE and DELETE on audit log at the database layer
CREATE TRIGGER trg_AuditLog_NoModify
ON AuditLogEntries
AFTER UPDATE, DELETE
AS
BEGIN
    RAISERROR('AuditLogEntries is append-only. UPDATE and DELETE are not permitted.', 16, 1);
    ROLLBACK TRANSACTION;
END;
```

---

## 4. Graph Schema Design

The supply chain graph uses **Adjacency List** representation, which SQL Server CTEs handle efficiently.

### 4.1 Why Adjacency List (not Nested Sets or Closure Table)?

| Pattern | Pros | Cons |
|---------|------|------|
| **Adjacency List** (chosen) | Simple inserts/deletes, natural edge metadata, CTE traversal | Requires CTE for tree queries |
| Closure Table | Fast reads, no CTE needed | Write overhead for large trees; separate table for paths |
| Nested Sets | Fast subtree queries | Expensive updates (rebalancing) — poor fit for dynamic supply chains |

The Adjacency List was chosen because:
1. Supply chain relationships change frequently (edges added/removed)
2. SQL Server CTE performance is excellent up to depth ~30
3. Edge metadata (MaterialID, RelationshipType, validity dates) is natural on a single row
4. Cycle detection is implemented at the application layer before writes

---

## 5. Key SQL Patterns

### 5.1 Recursive CTE — Downstream Supplier Tree

Returns all supplier organizations reachable from a given `@RootOrgId`, with depth and path.

```sql
WITH SupplierTree AS (
    -- Anchor: the root organization itself
    SELECT
        o.OrgID,
        o.LegalName,
        o.Country,
        o.Status,
        e.EdgeID,
        e.ParentOrgID,
        0                      AS Depth,
        CAST(o.OrgID AS NVARCHAR(MAX)) AS Path
    FROM Organizations o
    LEFT JOIN SupplyChainEdges e ON e.ChildOrgID = o.OrgID
    WHERE o.OrgID = @RootOrgId
      AND o.TenantID = @TenantId

    UNION ALL

    -- Recursive: walk downstream edges
    SELECT
        child.OrgID,
        child.LegalName,
        child.Country,
        child.Status,
        e.EdgeID,
        e.ParentOrgID,
        st.Depth + 1,
        st.Path + N' → ' + CAST(child.OrgID AS NVARCHAR(MAX))
    FROM SupplyChainEdges e
    INNER JOIN Organizations child ON child.OrgID = e.ChildOrgID
    INNER JOIN SupplierTree st     ON st.OrgID    = e.ParentOrgID
    WHERE e.IsActive = 1
      AND e.TenantID = @TenantId
      AND st.Depth < @MaxDepth       -- optional depth limit
)
SELECT DISTINCT
    OrgID, LegalName, Country, Status, Depth, Path
FROM SupplierTree
ORDER BY Depth, LegalName;
```

### 5.2 Recursive CTE — BOM Explosion

Returns all leaf-level materials in a product's BOM with accumulated quantities.

```sql
WITH BomExplosion AS (
    -- Anchor: root BOM entries (no parent)
    SELECT
        b.BomEntryID,
        b.MaterialID,
        b.ParentBomEntryID,
        b.Quantity,
        b.UnitOfMeasure,
        b.BomVersion,
        0 AS Level
    FROM BomEntries b
    WHERE b.ProductID = @ProductId
      AND b.ParentBomEntryID IS NULL
      AND b.BomVersion = @Version
      AND b.TenantID = @TenantId

    UNION ALL

    -- Recursive: expand sub-components
    SELECT
        child.BomEntryID,
        child.MaterialID,
        child.ParentBomEntryID,
        -- Multiply quantities up the tree
        be.Quantity * child.Quantity AS Quantity,
        child.UnitOfMeasure,
        child.BomVersion,
        be.Level + 1
    FROM BomEntries child
    INNER JOIN BomExplosion be ON be.BomEntryID = child.ParentBomEntryID
    WHERE child.TenantID = @TenantId
)
SELECT
    be.BomEntryID,
    m.Name        AS MaterialName,
    m.HsTariffCode,
    m.CountryOfOrigin,
    be.Quantity,
    be.UnitOfMeasure,
    be.Level
FROM BomExplosion be
INNER JOIN Materials m ON m.MaterialID = be.MaterialID
ORDER BY be.Level, m.Name;
```

### 5.3 Cycle Detection (Application Layer — DFS)

Before persisting a new edge `(ParentOrgID, ChildOrgID)`, the application runs a depth-first search:

```csharp
// Pseudocode: CycleDetectionService.cs
public async Task<CycleDetectionResult> WouldCreateCycleAsync(
    Guid parentOrgId,
    Guid childOrgId,
    Guid tenantId)
{
    // Load all active edges for the tenant into an adjacency list
    var edges = await _repo.GetActiveEdgesAsync(tenantId);
    var graph = BuildAdjacencyList(edges);

    // Add the proposed edge temporarily
    graph[parentOrgId].Add(childOrgId);

    // DFS from childOrgId; if we can reach parentOrgId, a cycle exists
    var visited = new HashSet<Guid>();
    var path = new Stack<Guid>();
    return DfsDetectCycle(graph, childOrgId, parentOrgId, visited, path);
}
```

---

## 6. Indexes & Constraints

```sql
-- Organization lookups by tenant
CREATE INDEX IX_Organizations_TenantID ON Organizations (TenantID) INCLUDE (LegalName, Status);

-- Edge traversal (downstream)
CREATE INDEX IX_Edges_ParentOrg ON SupplyChainEdges (ParentOrgID, IsActive) INCLUDE (ChildOrgID, TenantID);

-- Edge traversal (upstream / reverse)
CREATE INDEX IX_Edges_ChildOrg ON SupplyChainEdges (ChildOrgID, IsActive) INCLUDE (ParentOrgID, TenantID);

-- Document lookups by org
CREATE INDEX IX_Documents_OrgID ON Documents (OrgID, Status) INCLUDE (ExpiresAt, DocumentType);

-- Certification expiry monitoring (scheduled job)
CREATE INDEX IX_Documents_ExpiresAt ON Documents (ExpiresAt, Status) WHERE Status = 'Active';

-- BOM explosion by product + version
CREATE INDEX IX_BomEntries_Product ON BomEntries (ProductID, BomVersion) INCLUDE (ParentBomEntryID, MaterialID, Quantity);

-- Audit log retrieval by entity
CREATE INDEX IX_AuditLog_Entity ON AuditLogEntries (EntityType, EntityID, OccurredAt);

-- Unique: no duplicate active edges between the same pair of orgs
CREATE UNIQUE INDEX UX_Edges_ActivePair
    ON SupplyChainEdges (ParentOrgID, ChildOrgID, MaterialID)
    WHERE IsActive = 1;
```

---

## 7. Multi-Tenancy Strategy

Echo-Trace uses a **Shared Database, Shared Schema** multi-tenancy model with Row-Level Isolation.

### 7.1 Rationale

| Strategy | Isolation | Cost | Complexity |
|----------|-----------|------|------------|
| Separate DB per tenant | Highest | Highest | Highest |
| Shared DB, separate schema | Medium | Medium | Medium |
| **Shared DB, shared schema + TenantID** | Good (soft) | Lowest | Low |

For the MVP targeting up to 10,000 tenants, shared schema is appropriate. Strong isolation is enforced via:

1. `TenantID` column on every tenant-scoped table
2. EF Core Global Query Filters (applied in `DbContext.OnModelCreating`) automatically append `WHERE TenantID = @currentTenantId` to all LINQ queries
3. `ICurrentTenantService` injected into `DbContext` extracts `TenantID` from the JWT claims
4. Integration tests verify that a user with `TenantID = A` cannot access data belonging to `TenantID = B`

### 7.2 EF Core Global Query Filter Example

```csharp
// In EchoTraceDbContext.OnModelCreating:
builder.Entity<Organization>()
    .HasQueryFilter(o => o.TenantID == _currentTenantService.TenantId);

builder.Entity<SupplyChainEdge>()
    .HasQueryFilter(e => e.TenantID == _currentTenantService.TenantId);

// AuditLogEntries intentionally excluded from query filter
// so Platform Admins can query across tenants
```
