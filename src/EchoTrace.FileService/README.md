# EchoTrace File Service

Internal Node.js/Express microservice responsible for receiving certification document uploads,
computing their SHA-256 hash, and streaming them to MinIO. See
[docs/ADR/003-file-microservice.md](../../docs/ADR/003-file-microservice.md) for why this is a
separate service from the Core API.

**Not exposed to the public internet.** Every route except `/health` requires an
`X-Internal-Key` header matching the `INTERNAL_KEY` environment variable; the Core API is the
only caller (see `FileServiceClient` in `EchoTrace.Infrastructure/ExternalServices`).

## Endpoints

| Method | Path | Purpose |
|---|---|---|
| `POST` | `/upload` | Multipart upload (`file` field). Returns `{ blobPath, contentHash, fileSizeBytes, mimeType }`. |
| `GET` | `/file/*` | Streams a stored file back by its blob path. |
| `GET` | `/health` | No auth required — used by container orchestration. |

Allowed MIME types: `application/pdf`, `image/png`, `image/jpeg`. Max upload size: 50 MB.

## Local development

```bash
npm install
INTERNAL_KEY=dev-secret MINIO_ENDPOINT=localhost npm run dev
```

Requires a running MinIO instance — `docker compose up minio` from the repo root, or run the full
stack with `docker compose up --build`.
