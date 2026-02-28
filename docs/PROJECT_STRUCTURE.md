# Project Structure
## Echo-Trace: Tiered Supply Chain Transparency & Audit Platform

**Version:** 1.0.0
**Date:** 2026-02-26

---

This document defines the canonical folder and file structure for the Echo-Trace monorepo. All contributors must follow this layout.

---

## Root Layout

```
Echo_Trace/
├── .github/
│   └── workflows/
│       ├── ci.yml                  # PR: lint, test, build, security scan
│       └── cd.yml                  # main: build images → push → deploy
├── docs/
│   ├── SRS.md
│   ├── ARCHITECTURE.md
│   ├── DATA_MODEL.md
│   ├── API_DESIGN.md
│   ├── ROADMAP.md
│   ├── PROJECT_STRUCTURE.md        # (this file)
│   └── ADR/
│       ├── 001-clean-architecture.md
│       ├── 002-graph-relational-schema.md
│       ├── 003-file-microservice.md
│       └── 004-immutable-audit-trail.md
├── src/
│   ├── EchoTrace.API/              # C# .NET 8 — Core API
│   ├── EchoTrace.FileService/      # Node.js — File microservice
│   └── EchoTrace.Web/              # React 18 — Frontend SPA
├── k8s/                            # Kubernetes manifests (Phase 3)
├── scripts/
│   ├── init-db.sql                 # Database seed / initial setup
│   └── dev-setup.sh                # One-command local dev setup
├── docker-compose.yml
├── docker-compose.override.yml     # Local dev overrides (volumes, hot reload)
├── .editorconfig
├── .gitattributes
├── LICENSE
└── README.md
```

---

## C# Core API: `src/EchoTrace.API/`

```
EchoTrace.API/
├── EchoTrace.Domain/
│   ├── Entities/
│   │   ├── Organization.cs
│   │   ├── SupplyChainEdge.cs
│   │   ├── Material.cs
│   │   ├── Product.cs
│   │   ├── BomEntry.cs
│   │   ├── Document.cs
│   │   ├── AuditLogEntry.cs
│   │   ├── Tenant.cs
│   │   └── User.cs
│   ├── ValueObjects/
│   │   ├── ContentHash.cs          # SHA-256 wrapper
│   │   ├── ComplianceScore.cs
│   │   ├── OrgTier.cs
│   │   └── DocumentType.cs
│   ├── Events/
│   │   ├── OrganizationOnboardedEvent.cs
│   │   ├── SupplierInvitedEvent.cs
│   │   ├── DocumentUploadedEvent.cs
│   │   ├── DocumentExpiredEvent.cs
│   │   └── BomVersionPublishedEvent.cs
│   ├── Exceptions/
│   │   ├── CycleDetectedException.cs
│   │   ├── DocumentIntegrityViolationException.cs
│   │   ├── AuditIntegrityViolationException.cs
│   │   └── TenantAccessViolationException.cs
│   ├── Interfaces/
│   │   ├── Repositories/
│   │   │   ├── IOrganizationRepository.cs
│   │   │   ├── ISupplyChainRepository.cs
│   │   │   ├── IBomRepository.cs
│   │   │   ├── IDocumentRepository.cs
│   │   │   └── IAuditLogRepository.cs
│   │   └── Services/
│   │       └── ICycleDetectionService.cs
│   └── EchoTrace.Domain.csproj
│
├── EchoTrace.Application/
│   ├── Organizations/
│   │   ├── Commands/
│   │   │   ├── CreateOrganization/
│   │   │   │   ├── CreateOrganizationCommand.cs
│   │   │   │   ├── CreateOrganizationCommandHandler.cs
│   │   │   │   └── CreateOrganizationCommandValidator.cs
│   │   │   ├── UpdateOrganization/
│   │   │   │   ├── UpdateOrganizationCommand.cs
│   │   │   │   ├── UpdateOrganizationCommandHandler.cs
│   │   │   │   └── UpdateOrganizationCommandValidator.cs
│   │   │   └── InviteSupplier/
│   │   │       ├── InviteSupplierCommand.cs
│   │   │       ├── InviteSupplierCommandHandler.cs
│   │   │       └── InviteSupplierCommandValidator.cs
│   │   └── Queries/
│   │       ├── GetOrganizationById/
│   │       │   ├── GetOrganizationByIdQuery.cs
│   │       │   └── GetOrganizationByIdQueryHandler.cs
│   │       ├── GetOrganizations/
│   │       └── GetOrganizationTree/
│   ├── SupplyChain/
│   │   ├── Commands/
│   │   │   ├── AddEdge/
│   │   │   │   ├── AddEdgeCommand.cs
│   │   │   │   ├── AddEdgeCommandHandler.cs   # Calls ICycleDetectionService
│   │   │   │   └── AddEdgeCommandValidator.cs
│   │   │   └── RemoveEdge/
│   │   └── Queries/
│   │       ├── GetSupplyChainTree/
│   │       ├── TraceBack/
│   │       │   ├── TraceBackQuery.cs
│   │       │   └── TraceBackQueryHandler.cs
│   │       └── GetComplianceScore/
│   ├── BOM/
│   │   ├── Commands/
│   │   │   ├── CreateBomEntry/
│   │   │   └── PublishBomVersion/
│   │   └── Queries/
│   │       └── ExplodeBom/
│   ├── Documents/
│   │   ├── Commands/
│   │   │   ├── InitiateUpload/
│   │   │   ├── ConfirmUpload/
│   │   │   └── RevokeDocument/
│   │   └── Queries/
│   │       ├── GetDocuments/
│   │       ├── GetDocumentById/
│   │       └── VerifyDocumentIntegrity/
│   ├── DPP/
│   │   └── Queries/
│   │       └── GenerateDPP/
│   ├── Audit/
│   │   └── Queries/
│   │       ├── GetAuditLog/
│   │       └── VerifyAuditChain/
│   ├── Auth/
│   │   └── Commands/
│   │       ├── Login/
│   │       ├── Register/
│   │       ├── RefreshToken/
│   │       └── Logout/
│   ├── Common/
│   │   ├── Behaviors/
│   │   │   ├── ValidationBehavior.cs       # FluentValidation pipeline
│   │   │   └── AuditBehavior.cs            # Auto audit-log on commands
│   │   ├── DTOs/                           # Shared response/request shapes
│   │   ├── Mappings/                       # AutoMapper profiles
│   │   └── Pagination/
│   │       └── PagedResult.cs
│   ├── Interfaces/
│   │   ├── IBlobStorageService.cs
│   │   ├── IEmailService.cs
│   │   ├── IFileServiceClient.cs
│   │   ├── IComplianceScoreService.cs
│   │   ├── IDppGeneratorService.cs
│   │   └── ICurrentTenantService.cs
│   └── EchoTrace.Application.csproj
│
├── EchoTrace.Infrastructure/
│   ├── Persistence/
│   │   ├── EchoTraceDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── TenantConfiguration.cs
│   │   │   ├── OrganizationConfiguration.cs
│   │   │   ├── SupplyChainEdgeConfiguration.cs
│   │   │   ├── MaterialConfiguration.cs
│   │   │   ├── ProductConfiguration.cs
│   │   │   ├── BomEntryConfiguration.cs
│   │   │   ├── DocumentConfiguration.cs
│   │   │   ├── AuditLogEntryConfiguration.cs
│   │   │   └── UserConfiguration.cs
│   │   ├── Repositories/
│   │   │   ├── OrganizationRepository.cs
│   │   │   ├── SupplyChainRepository.cs    # Contains CTE queries
│   │   │   ├── BomRepository.cs
│   │   │   ├── DocumentRepository.cs
│   │   │   └── AuditLogRepository.cs
│   │   └── Migrations/
│   │       └── (EF Core migration files)
│   ├── Services/
│   │   ├── CycleDetectionService.cs        # DFS implementation
│   │   ├── ComplianceScoreService.cs
│   │   ├── AuditHashService.cs             # Chained SHA-256
│   │   ├── DppGeneratorService.cs          # JSON + PDF generation
│   │   └── CurrentTenantService.cs         # Extracts TenantID from HttpContext
│   ├── ExternalServices/
│   │   ├── FileServiceClient.cs            # HTTP client to Node.js service
│   │   ├── BlobStorageService.cs           # Azure Blob or MinIO
│   │   └── EmailService.cs                 # SMTP or SendGrid
│   └── EchoTrace.Infrastructure.csproj
│
├── EchoTrace.API/                          # (the web host / entry point)
│   ├── Controllers/
│   │   ├── AuthController.cs
│   │   ├── OrganizationsController.cs
│   │   ├── SupplyChainController.cs
│   │   ├── BomController.cs
│   │   ├── DocumentsController.cs
│   │   ├── DppController.cs
│   │   ├── AuditController.cs
│   │   └── AdminController.cs
│   ├── Middleware/
│   │   ├── GlobalExceptionMiddleware.cs    # RFC 7807 error responses
│   │   ├── CorrelationIdMiddleware.cs
│   │   └── TenantResolutionMiddleware.cs
│   ├── Extensions/
│   │   ├── ServiceCollectionExtensions.cs  # DI wiring
│   │   └── WebApplicationExtensions.cs
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Program.cs
│   └── EchoTrace.API.csproj
│
└── EchoTrace.Tests/
    ├── Unit/
    │   ├── Domain/
    │   ├── Application/
    │   └── Infrastructure/
    ├── Integration/
    │   └── (WebApplicationFactory tests with TestContainers)
    └── EchoTrace.Tests.csproj
```

---

## Node.js File Service: `src/EchoTrace.FileService/`

```
EchoTrace.FileService/
├── src/
│   ├── server.js                   # Express app + port binding
│   ├── app.js                      # Express app factory (testable)
│   ├── routes/
│   │   ├── upload.routes.js        # POST /upload
│   │   └── health.routes.js        # GET /health
│   ├── middleware/
│   │   ├── auth.middleware.js       # X-Internal-Key validation
│   │   ├── validate.middleware.js   # File type + size validation
│   │   └── errorHandler.js          # Centralized error responses
│   ├── services/
│   │   ├── hashService.js           # SHA-256 computation
│   │   └── storageService.js        # Blob storage abstraction
│   └── config/
│       └── index.js                 # Environment config with defaults
├── tests/
│   ├── hashService.test.js
│   ├── upload.routes.test.js
│   └── storageService.test.js
├── Dockerfile
├── package.json
├── jest.config.js
└── .env.example
```

---

## React Frontend: `src/EchoTrace.Web/`

```
EchoTrace.Web/
├── public/
│   └── favicon.ico
├── src/
│   ├── app/
│   │   ├── App.tsx
│   │   ├── router.tsx              # React Router v6 route definitions
│   │   └── providers.tsx           # QueryClient, AuthProvider, ThemeProvider
│   ├── features/
│   │   ├── auth/
│   │   │   ├── components/
│   │   │   │   ├── LoginForm.tsx
│   │   │   │   └── RegisterForm.tsx
│   │   │   ├── pages/
│   │   │   │   ├── LoginPage.tsx
│   │   │   │   └── RegisterPage.tsx
│   │   │   ├── hooks/
│   │   │   │   └── useAuth.ts
│   │   │   └── authStore.ts        # Zustand slice
│   │   ├── organizations/
│   │   │   ├── components/
│   │   │   │   ├── OrgCard.tsx
│   │   │   │   ├── OrgTable.tsx
│   │   │   │   └── InviteSupplierModal.tsx
│   │   │   ├── pages/
│   │   │   │   ├── OrgListPage.tsx
│   │   │   │   └── OrgDetailPage.tsx
│   │   │   └── hooks/
│   │   │       └── useOrganizations.ts
│   │   ├── supply-chain/
│   │   │   ├── components/
│   │   │   │   ├── SupplyChainGraph.tsx  # Cytoscape.js wrapper
│   │   │   │   ├── TraceBackPanel.tsx
│   │   │   │   └── ComplianceBadge.tsx
│   │   │   └── hooks/
│   │   │       └── useSupplyChainTree.ts
│   │   ├── bom/
│   │   │   ├── components/
│   │   │   │   ├── BomTree.tsx
│   │   │   │   └── BomEntryForm.tsx
│   │   │   └── hooks/
│   │   │       └── useBom.ts
│   │   ├── documents/
│   │   │   ├── components/
│   │   │   │   ├── DocumentList.tsx
│   │   │   │   ├── DocumentUploadForm.tsx
│   │   │   │   └── DocumentStatusBadge.tsx
│   │   │   └── hooks/
│   │   │       └── useDocuments.ts
│   │   ├── dpp/
│   │   │   ├── components/
│   │   │   │   └── DppViewer.tsx
│   │   │   └── pages/
│   │   │       └── DppPage.tsx
│   │   └── audit/
│   │       ├── components/
│   │       │   ├── AuditLogTable.tsx
│   │       │   └── AuditIntegrityStatus.tsx
│   │       └── pages/
│   │           └── AuditLogPage.tsx
│   ├── shared/
│   │   ├── components/
│   │   │   ├── Layout/
│   │   │   │   ├── AppShell.tsx
│   │   │   │   ├── Sidebar.tsx
│   │   │   │   └── TopBar.tsx
│   │   │   ├── DataTable.tsx
│   │   │   ├── ConfirmDialog.tsx
│   │   │   ├── EmptyState.tsx
│   │   │   └── LoadingSpinner.tsx
│   │   ├── api/
│   │   │   ├── client.ts           # Axios instance with interceptors
│   │   │   └── endpoints.ts        # Typed API endpoint constants
│   │   ├── hooks/
│   │   │   ├── useDebounce.ts
│   │   │   └── useTenantId.ts
│   │   └── utils/
│   │       ├── formatDate.ts
│   │       └── complianceColor.ts
│   └── types/
│       └── api.types.ts            # TypeScript shapes mirroring API responses
├── index.html
├── vite.config.ts
├── tailwind.config.ts
├── tsconfig.json
├── Dockerfile
└── nginx.conf                      # Production NGINX config
```

---

## Naming Conventions

| Context | Convention | Example |
|---------|-----------|---------|
| C# files | PascalCase | `OrganizationRepository.cs` |
| C# namespaces | `EchoTrace.<Layer>.<Feature>` | `EchoTrace.Application.Organizations.Commands` |
| SQL tables | PascalCase plural | `Organizations`, `SupplyChainEdges` |
| SQL columns | PascalCase | `ParentOrgID`, `CreatedAt` |
| REST routes | kebab-case, plural nouns | `/supply-chain/edges`, `/organizations` |
| React components | PascalCase | `OrgDetailPage.tsx` |
| React hooks | camelCase, `use` prefix | `useOrganizations.ts` |
| Environment variables | UPPER_SNAKE_CASE | `SQLSERVER_CONNECTION_STRING` |
| Docker services | kebab-case | `echo-api`, `echo-fileservice` |

---

## Key Files at a Glance

| File | Purpose |
|------|---------|
| `src/EchoTrace.API/EchoTrace.API/Program.cs` | App entry point, DI setup, middleware pipeline |
| `src/EchoTrace.Infrastructure/Persistence/EchoTraceDbContext.cs` | EF Core context with global query filters |
| `src/EchoTrace.Infrastructure/Persistence/Repositories/SupplyChainRepository.cs` | Contains all recursive CTE queries |
| `src/EchoTrace.Infrastructure/Services/CycleDetectionService.cs` | DFS cycle detection |
| `src/EchoTrace.Infrastructure/Services/AuditHashService.cs` | Chained SHA-256 audit hashing |
| `src/EchoTrace.FileService/src/services/hashService.js` | SHA-256 computation for uploaded files |
| `src/EchoTrace.Web/src/features/supply-chain/components/SupplyChainGraph.tsx` | Cytoscape.js graph renderer |
| `docker-compose.yml` | Full local dev stack |
