import { Router } from 'express';
import { minioClient, BUCKET } from '../lib/minioClient.js';

const startedAt = Date.now();
export const router = Router();

router.get('/health', async (_req, res) => {
  const storage = await minioClient.bucketExists(BUCKET).then(
    () => 'connected',
    () => 'disconnected',
  );

  res.json({
    status: storage === 'connected' ? 'healthy' : 'degraded',
    storage,
    uptime: Math.floor((Date.now() - startedAt) / 1000),
  });
});
