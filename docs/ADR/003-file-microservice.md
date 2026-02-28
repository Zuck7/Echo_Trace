# ADR-003: Decoupled Node.js File Microservice

**Date:** 2026-02-26
**Status:** Accepted
**Deciders:** Architecture Team

---

## Context

Echo-Trace requires secure document ingestion for ISO/ESG certifications. Document uploads have distinct technical characteristics from the main business logic:

1. Large binary payloads (up to 50 MB) that should not flow through the main C# API process
2. CPU-bound SHA-256 hashing during upload
3. Streaming to blob storage (Azure Blob / MinIO) — I/O-bound, suits Node.js's event loop model
4. Independent scaling requirements: upload load is bursty (suppliers upload docs quarterly)
5. The upload service has zero business logic — it only moves and hashes files

## Decision

Build a **separate Node.js / Express microservice** (`EchoTrace.FileService`) responsible exclusively for:
- Receiving multipart file uploads
- Computing SHA-256 hash
- Streaming to blob storage
- Returning `{ blobPath, contentHash, fileSizeBytes }` to the caller

The Core C# API delegates all file uploads to this service via an internal HTTP call. The Core API retains ownership of the document metadata (DocumentType, ExpiresAt, OrgID) and stores it in SQL Server.

## Considered Alternatives

### Option A: Handle uploads directly in the C# API
- **Pros:** Single service to deploy; no inter-service HTTP call; simpler architecture
- **Cons:** Large binary payloads consume memory in the C# process; scaling the upload function requires scaling the entire API; mixing file I/O concerns with business logic violates single-responsibility principle; SHA-256 hashing and blob streaming are more idiomatic in Node.js

### Option B: Decoupled Node.js File Service (chosen)
- **Pros:** Independent scaling of upload capacity; event-loop-friendly streaming in Node.js; clear separation of concerns; file service can be maintained/deployed by a separate team without risking Core API stability
- **Cons:** Additional service to deploy and monitor; introduces inter-service network call (adds ~5ms latency for upload initiation)

### Option C: Upload directly from browser to blob storage via pre-signed URL
- **Pros:** Eliminates server-side processing; fastest for the user
- **Cons:** Pre-signed URLs expose blob storage directly to clients; SHA-256 hash cannot be computed server-side before upload (the hash must be verified after the fact); cannot enforce MIME type or file size limits server-side before write

## Consequences

### Positive
- File service scales independently (HPA in Kubernetes based on CPU/memory)
- The C# Core API is never blocked on large file I/O
- Node.js is extremely well-suited for streaming multipart data with `multer`
- Clear security boundary: file service validates MIME type and size limits before storing anything

### Negative
- Two-step upload flow: client → Core API → File Service → Blob Storage → Core API (register metadata)
- Requires shared internal API key secret for service-to-service auth
- Additional Dockerfile, CI pipeline, monitoring

### Inter-Service Communication

The file service is accessible only on the internal Docker/Kubernetes network. It is not exposed on the public internet. The Core API authenticates using a shared `X-Internal-Key` header (rotated via secrets management).

The upload flow:
```
Client → Core API [1] generate upload request
       → File Service [2] upload + hash
       → Core API [3] confirm + store metadata
```

This three-step flow ensures the Core API always controls document metadata and the audit trail, regardless of how files are stored.
