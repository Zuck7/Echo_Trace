# Deployment Guide

This guide covers the minimum path to deploy Echo-Trace API and share it on a resume.

## 1. Pre-deployment Checklist

- Solution builds successfully.
- Tests pass in CI.
- Docker image builds locally.
- Environment variables are externalized (no secrets in git).
- A public health endpoint is available.

## 2. Local Verification

From repository root:

```bash
cp .env.example .env
docker compose up --build
curl http://localhost:5150/health
```

Expected output:

```json
{"status":"healthy","timestamp":"..."}
```

## 3. Container Registry

Push the API image to a registry before cloud deployment.

```bash
cd src/EchoTrace.API
docker build -f EchoTrace.API/Dockerfile -t <registry>/<namespace>/echotrace-api:latest .
docker push <registry>/<namespace>/echotrace-api:latest
```

## 4. Cloud Deployment (Starter Path)

Use one of these managed platforms for fastest portfolio deployment:

- Azure Container Apps
- Render
- Railway
- Fly.io

Set these environment variables in your platform:

- ConnectionStrings__Default
- Jwt__Key
- Jwt__Issuer
- Jwt__Audience
- ASPNETCORE_ENVIRONMENT

## 5. Post-deployment Validation

- Verify GET /health returns HTTP 200.
- Verify Swagger endpoint is reachable.
- Run one authenticated endpoint with a valid JWT.
- Capture screenshots of:
  - CI passing checks
  - Swagger endpoint
  - Health endpoint response

## 6. Resume and Recruiter Readiness

Before adding to resume, make sure all of these are available:

- Live API URL
- Repository URL
- README with setup + architecture notes
- 2-3 minute demo video
- List of implemented features vs planned roadmap
