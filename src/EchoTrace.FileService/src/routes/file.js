import { Router } from 'express';
import { minioClient, BUCKET } from '../lib/minioClient.js';

export const router = Router();

// blobPath itself contains slashes (e.g. "docs/<id>.pdf"), so it's captured as a wildcard rather
// than a single :param.
router.get('/file/*', async (req, res) => {
  const blobPath = req.params[0];

  try {
    const stat = await minioClient.statObject(BUCKET, blobPath);
    res.setHeader('Content-Type', stat.metaData['content-type'] || 'application/octet-stream');
    const stream = await minioClient.getObject(BUCKET, blobPath);
    stream.on('error', () => res.destroy());
    stream.pipe(res);
  } catch {
    res.status(404).json({ error: 'File not found' });
  }
});
