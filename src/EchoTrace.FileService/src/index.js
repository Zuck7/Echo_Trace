import express from 'express';
import cors from 'cors';
import multer from 'multer';
import { router as uploadRouter, MAX_SIZE_BYTES } from './routes/upload.js';
import { router as fileRouter } from './routes/file.js';
import { router as healthRouter } from './routes/health.js';
import { internalAuth } from './middleware/internalAuth.js';
import { ensureBucket } from './lib/minioClient.js';

const app = express();
app.use(cors());

app.use(healthRouter);
app.use(internalAuth, uploadRouter);
app.use(internalAuth, fileRouter);

// eslint-disable-next-line no-unused-vars -- Express identifies error middleware by arity (4 args)
app.use((err, req, res, next) => {
  if (err instanceof multer.MulterError && err.code === 'LIMIT_FILE_SIZE') {
    return res.status(413).json({ error: `File exceeds ${MAX_SIZE_BYTES} bytes` });
  }
  console.error(err);
  res.status(500).json({ error: 'Internal file service error' });
});

const PORT = process.env.PORT || 3000;

if (!process.env.INTERNAL_KEY) {
  throw new Error('INTERNAL_KEY is not configured.');
}

ensureBucket()
  .catch((err) => console.error('Failed to ensure MinIO bucket exists:', err.message))
  .finally(() => {
    app.listen(PORT, () => console.log(`EchoTrace File Service listening on :${PORT}`));
  });
