export interface LoginResult {
  accessToken: string;
  expiresIn: number;
  tokenType: string;
}

export interface RegisterResult {
  userId: string;
  orgId: string;
  tenantId: string;
  email: string;
  role: string;
}

export interface Organization {
  orgId: string;
  tenantId: string;
  legalName: string;
  country: string;
  industrySector: string | null;
  registrationNumber: string | null;
  contactEmail: string;
  address: string | null;
  esgRiskScore: number | null;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface SupplyChainNode {
  orgId: string;
  legalName: string;
  country: string;
  status: string;
  depth: number;
  path: string;
}

export interface SupplyChainEdge {
  edgeId: string;
  tenantId: string;
  parentOrgId: string;
  childOrgId: string;
  materialId: string | null;
  relationshipType: string;
  validFrom: string;
  validTo: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface SupplyChainTree {
  rootOrgId: string;
  nodes: SupplyChainNode[];
  edges: SupplyChainEdge[];
}

export interface AuditLogEntry {
  auditId: number;
  tenantId: string;
  entityType: string;
  entityId: string;
  action: string;
  oldValue: string | null;
  newValue: string | null;
  performedByUserId: string | null;
  performedByIpAddress: string | null;
  occurredAt: string;
  chainHash: string;
}

export interface ProblemDetails {
  title: string;
  status: number;
  detail: string;
}

export interface Session {
  accessToken: string;
  email: string;
  role: string;
  orgId: string;
}
