// Every route in this service is internal-only (docs/ADR/003-file-microservice.md): the Core API
// is the sole caller, authenticated with a shared secret rather than a user identity. Only
// /health skips this check, since it's used by container orchestration.
export function internalAuth(req, res, next) {
  const key = req.header('X-Internal-Key');
  if (!key || key !== process.env.INTERNAL_KEY) {
    return res.status(401).json({ error: 'Missing or invalid X-Internal-Key' });
  }
  next();
}
