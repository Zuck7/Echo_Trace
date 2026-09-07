import crypto from 'node:crypto';
import { Router } from 'express';
import multer from 'multer';
import { minioClient, BUCKET } from '../lib/minioClient.js';

export const MAX_SIZE_BYTES = 50 * 1024 * 1024; // 50MB, per docs/ADR/003-file-microservice.md
const ALLOWED_MIME_TYPES = new Set(['application/pdf', 'image/png', 'image/jpeg']);

const upload = multer({
  storage: multer.memoryStorage(),
  limits: { fileSize: MAX_SIZE_BYTES },
});

export const router = Router();

router.post('/upload', upload.single('file'), async (req, res, next) => {
  try {
    if (!req.file) return res.status(400).json({ error: 'file is required' });
    if (!ALLOWED_MIME_TYPES.has(req.file.mimetype)) {
      return res.status(415).json({ error: `Unsupported MIME type: ${req.file.mimetype}` });
    }

    const contentHash = crypto.createHash('sha256').update(req.file.buffer).digest('hex');
    const uploadRequestId = req.header('X-Upload-Request-Id') || crypto.randomUUID();
    const extensionMatch = req.file.originalname.match(/\.[^./\\]+$/);
    const blobPath = `docs/${uploadRequestId}${extensionMatch ? extensionMatch[0] : ''}`;

    await minioClient.putObject(BUCKET, blobPath, req.file.buffer, req.file.size, {
      'Content-Type': req.file.mimetype,
    });

    res.json({
      blobPath,
      contentHash,
      fileSizeBytes: req.file.size,
      mimeType: req.file.mimetype,
    });
  } catch (err) {
    next(err);
  }
});
