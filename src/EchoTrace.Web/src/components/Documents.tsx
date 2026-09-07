import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError, downloadDocumentFile, getDocuments, revokeDocument, uploadDocument } from '../api';
import type { DocumentRecord, Session } from '../types';

const DOCUMENT_TYPES = ['ISO_14001', 'ISO_45001', 'ISO_9001', 'ESG_REPORT', 'OTHER'];

export function Documents({
  session,
  onSessionExpired,
}: {
  session: Session;
  onSessionExpired: (err: unknown) => boolean;
}) {
  const [documents, setDocuments] = useState<DocumentRecord[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [actionMessage, setActionMessage] = useState<string | null>(null);

  const [documentType, setDocumentType] = useState(DOCUMENT_TYPES[0]);
  const [expiresAt, setExpiresAt] = useState('');
  const [file, setFile] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const refresh = useCallback(async () => {
    const list = await getDocuments(session.accessToken, session.orgId);
    setDocuments(list);
  }, [session]);

  useEffect(() => {
    refresh().catch((err) => {
      if (onSessionExpired(err)) return;
      setError(err instanceof ApiError ? err.message : 'Failed to load documents');
    });
  }, [refresh, onSessionExpired]);

  async function handleUpload(e: React.FormEvent) {
    e.preventDefault();
    if (!file) return;

    setError(null);
    setBusy(true);
    try {
      await uploadDocument(
        session.accessToken,
        { documentType, issuedAt: null, expiresAt: expiresAt || null },
        file,
      );
      setFile(null);
      if (fileInputRef.current) fileInputRef.current.value = '';
      await refresh();
    } catch (err) {
      if (onSessionExpired(err)) return;
      setError(err instanceof ApiError ? err.message : 'Failed to upload document');
    } finally {
      setBusy(false);
    }
  }

  async function handleDownload(doc: DocumentRecord) {
    setActionMessage(null);
    setError(null);
    try {
      await downloadDocumentFile(session.accessToken, doc.documentId, doc.originalFileName);
      setActionMessage(`Verified & downloaded "${doc.originalFileName}" — SHA-256 hash matched.`);
    } catch (err) {
      if (onSessionExpired(err)) return;
      setError(
        err instanceof ApiError
          ? err.message
          : `Failed to download "${doc.originalFileName}"`,
      );
    }
  }

  async function handleRevoke(doc: DocumentRecord) {
    setError(null);
    setBusy(true);
    try {
      await revokeDocument(session.accessToken, doc.documentId);
      await refresh();
    } catch (err) {
      if (onSessionExpired(err)) return;
      setError(err instanceof ApiError ? err.message : 'Failed to revoke document');
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="panel span-2">
      <div className="panel-header">
        <h2>Certifications ({documents.length})</h2>
      </div>
      <p className="caption">
        Uploaded through the Node.js file service, hashed with SHA-256 on the way in, and
        re-verified against that hash every time it's downloaded — see{' '}
        <code>GET /documents/{'{id}'}/download</code>.
      </p>

      {error && <div className="error banner">{error}</div>}
      {actionMessage && <div className="cycle-result ok">{actionMessage}</div>}

      <table>
        <thead>
          <tr>
            <th>File</th>
            <th>Type</th>
            <th>Hash</th>
            <th>Status</th>
            <th>Uploaded</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {documents.map((d) => (
            <tr key={d.documentId}>
              <td>{d.originalFileName}</td>
              <td>{d.documentType}</td>
              <td className="hash">{d.contentHash.slice(0, 16)}…</td>
              <td>
                <span className={`badge ${d.status.toLowerCase()}`}>{d.status}</span>
              </td>
              <td>{new Date(d.uploadedAt).toLocaleString()}</td>
              <td>
                <button type="button" className="ghost" onClick={() => handleDownload(d)}>
                  Verify &amp; download
                </button>
                {session.role === 'ORG_ADMIN' && d.status === 'Active' && (
                  <button type="button" className="ghost" onClick={() => handleRevoke(d)} disabled={busy}>
                    Revoke
                  </button>
                )}
              </td>
            </tr>
          ))}
          {documents.length === 0 && (
            <tr>
              <td colSpan={6}>No certifications uploaded yet.</td>
            </tr>
          )}
        </tbody>
      </table>

      {(session.role === 'ORG_ADMIN' || session.role === 'COMPLIANCE_OFFICER') && (
        <form onSubmit={handleUpload} className="add-supplier-form">
          <h3>Upload a certification</h3>
          <div className="row">
            <select value={documentType} onChange={(e) => setDocumentType(e.target.value)}>
              {DOCUMENT_TYPES.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
            <input
              type="date"
              value={expiresAt}
              onChange={(e) => setExpiresAt(e.target.value)}
              title="Expiry date (optional)"
            />
          </div>
          <input
            ref={fileInputRef}
            required
            type="file"
            accept="application/pdf,image/png,image/jpeg"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
          <button type="submit" className="primary" disabled={busy || !file}>
            {busy ? 'Uploading…' : 'Upload certification'}
          </button>
        </form>
      )}
    </section>
  );
}
