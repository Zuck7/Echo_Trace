import type {
  AuditLogEntry,
  DocumentRecord,
  LoginResult,
  Organization,
  ProblemDetails,
  RegisterResult,
  SupplyChainEdge,
  SupplyChainTree,
} from './types';

const BASE_URL = (import.meta.env.VITE_API_URL as string | undefined) ?? 'http://localhost:5150';
const API = `${BASE_URL}/api/v1`;

export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

async function request<T>(
  path: string,
  options: { method?: string; body?: unknown; token?: string } = {},
): Promise<T> {
  const res = await fetch(`${API}${path}`, {
    method: options.method ?? 'GET',
    headers: {
      'Content-Type': 'application/json',
      ...(options.token ? { Authorization: `Bearer ${options.token}` } : {}),
    },
    body: options.body ? JSON.stringify(options.body) : undefined,
  });

  if (!res.ok) {
    const problem = (await res.json().catch(() => null)) as ProblemDetails | null;
    throw new ApiError(res.status, problem?.detail ?? problem?.title ?? `Request failed (${res.status})`);
  }

  if (res.status === 204) return undefined as T;
  return res.json() as Promise<T>;
}

export function healthCheck(): Promise<{ status: string }> {
  return fetch(`${BASE_URL}/health`).then((r) => r.json());
}

export function register(input: {
  email: string;
  password: string;
  orgLegalName: string;
  orgCountry: string;
}): Promise<RegisterResult> {
  return request('/auth/register', { method: 'POST', body: input });
}

export function login(email: string, password: string): Promise<LoginResult> {
  return request('/auth/login', { method: 'POST', body: { email, password } });
}

export function getOrganizations(token: string): Promise<Organization[]> {
  return request('/organizations', { token });
}

export function createOrganization(
  token: string,
  input: { legalName: string; country: string; contactEmail: string },
): Promise<{ orgId: string; tenantId: string; legalName: string; status: string }> {
  return request('/organizations', { method: 'POST', body: input, token });
}

export function addEdge(
  token: string,
  input: { childOrgId: string; relationshipType: string },
): Promise<SupplyChainEdge> {
  return request('/supply-chain/edges', { method: 'POST', body: input, token });
}

export function getSupplyChainTree(token: string, orgId: string): Promise<SupplyChainTree> {
  return request(`/supply-chain/tree/${orgId}`, { token });
}

export function getAuditLogs(token: string): Promise<AuditLogEntry[]> {
  return request('/audit/logs', { token });
}

export function getDocuments(token: string, orgId: string): Promise<DocumentRecord[]> {
  return request(`/documents?orgId=${orgId}`, { token });
}

export function initiateUpload(
  token: string,
  input: { documentType: string; originalFileName: string; issuedAt: string | null; expiresAt: string | null },
): Promise<{ uploadRequestId: string; expiresAt: string }> {
  return request('/documents/upload-request', { method: 'POST', body: input, token });
}

export interface ConfirmUploadResult {
  documentId: string;
  contentHash: string;
  status: string;
  uploadedAt: string;
}

export async function confirmUpload(
  token: string,
  uploadRequestId: string,
  file: File,
): Promise<ConfirmUploadResult> {
  const form = new FormData();
  form.append('uploadRequestId', uploadRequestId);
  form.append('file', file);

  const res = await fetch(`${API}/documents/confirm-upload`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${token}` },
    body: form,
  });

  if (!res.ok) {
    const problem = (await res.json().catch(() => null)) as ProblemDetails | null;
    throw new ApiError(res.status, problem?.detail ?? problem?.title ?? `Upload failed (${res.status})`);
  }

  return res.json() as Promise<ConfirmUploadResult>;
}

export function uploadDocument(
  token: string,
  input: { documentType: string; issuedAt: string | null; expiresAt: string | null },
  file: File,
): Promise<ConfirmUploadResult> {
  return initiateUpload(token, { ...input, originalFileName: file.name }).then((req) =>
    confirmUpload(token, req.uploadRequestId, file),
  );
}

export function getDownloadLink(
  token: string,
  documentId: string,
): Promise<{ downloadUrl: string; expiresAt: string; contentHash: string; integrityVerified: boolean }> {
  return request(`/documents/${documentId}/download`, { token });
}

export async function downloadDocumentFile(token: string, documentId: string, fileName: string): Promise<void> {
  const link = await getDownloadLink(token, documentId);
  if (!link.integrityVerified) {
    throw new ApiError(422, 'Document integrity check failed — the stored hash no longer matches the file.');
  }

  const res = await fetch(`${BASE_URL}${link.downloadUrl}`, { headers: { Authorization: `Bearer ${token}` } });
  if (!res.ok) throw new ApiError(res.status, `Download failed (${res.status})`);

  const blob = await res.blob();
  const objectUrl = URL.createObjectURL(blob);
  const anchor = window.document.createElement('a');
  anchor.href = objectUrl;
  anchor.download = fileName;
  anchor.click();
  URL.revokeObjectURL(objectUrl);
}

export function revokeDocument(token: string, documentId: string): Promise<void> {
  return request(`/documents/${documentId}`, { method: 'DELETE', token });
}

export interface JwtClaims {
  email: string;
  role: string;
  orgId: string;
  tenantId: string;
}

export function decodeJwt(token: string): JwtClaims {
  const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')));
  return {
    email: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
    role: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
    orgId: payload.orgId,
    tenantId: payload.tenantId,
  };
}
