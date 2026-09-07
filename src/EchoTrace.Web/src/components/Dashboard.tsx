import { useCallback, useEffect, useState } from 'react';
import { ApiError, addEdge, createOrganization, getAuditLogs, getOrganizations, getSupplyChainTree } from '../api';
import type { AuditLogEntry, Organization, Session, SupplyChainTree } from '../types';
import { Documents } from './Documents';
import { SupplyChainGraph } from './SupplyChainGraph';

const DEMO_SUPPLIERS = [
  { legalName: 'Global Textiles Ltd', country: 'CN', relationshipType: 'SOURCES' },
  { legalName: 'Raw Cotton Farms Co', country: 'IN', relationshipType: 'SOURCES' },
  { legalName: 'Precision Manufacturing Inc', country: 'DE', relationshipType: 'MANUFACTURES' },
  { legalName: 'Global Logistics Partners', country: 'NL', relationshipType: 'DISTRIBUTES' },
];

export function Dashboard({
  session,
  onLogout,
}: {
  session: Session;
  onLogout: (message?: string) => void;
}) {
  const [orgs, setOrgs] = useState<Organization[]>([]);
  const [tree, setTree] = useState<SupplyChainTree | null>(null);
  const [auditLog, setAuditLog] = useState<AuditLogEntry[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [cycleTest, setCycleTest] = useState<{ ok: boolean; message: string } | null>(null);

  const [supplierName, setSupplierName] = useState('');
  const [supplierCountry, setSupplierCountry] = useState('CN');
  const [supplierEmail, setSupplierEmail] = useState('');
  const [relationshipType, setRelationshipType] = useState('SOURCES');

  const refresh = useCallback(async () => {
    const [orgList, chainTree, logs] = await Promise.all([
      getOrganizations(session.accessToken),
      getSupplyChainTree(session.accessToken, session.orgId),
      getAuditLogs(session.accessToken),
    ]);
    setOrgs(orgList);
    setTree(chainTree);
    setAuditLog(logs);
  }, [session]);

  // Access tokens last 15 minutes and there's no refresh flow yet, so any action can hit a 401
  // mid-session. Centralize that: log out with an explanatory message instead of a bare
  // "Request failed (401)" (JwtBearer's 401 challenge has no JSON body to show, either).
  const handleSessionExpiry = useCallback(
    (err: unknown): boolean => {
      if (err instanceof ApiError && err.status === 401) {
        onLogout('Your session expired (access tokens last 15 minutes). Please log in again.');
        return true;
      }
      return false;
    },
    [onLogout],
  );

  useEffect(() => {
    refresh().catch((err) => {
      if (handleSessionExpiry(err)) return;
      setError(err instanceof ApiError ? err.message : 'Failed to load data');
    });
  }, [refresh, handleSessionExpiry]);

  async function handleAddSupplier(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      const supplier = await createOrganization(session.accessToken, {
        legalName: supplierName,
        country: supplierCountry,
        contactEmail: supplierEmail || `contact@${supplierName.toLowerCase().replace(/\s+/g, '')}.example`,
      });
      await addEdge(session.accessToken, { childOrgId: supplier.orgId, relationshipType });
      setSupplierName('');
      setSupplierEmail('');
      await refresh();
    } catch (err) {
      if (handleSessionExpiry(err)) return;
      setError(err instanceof ApiError ? err.message : 'Failed to add supplier');
    } finally {
      setBusy(false);
    }
  }

  async function handleCycleTest() {
    setCycleTest(null);
    setBusy(true);
    try {
      await addEdge(session.accessToken, { childOrgId: session.orgId, relationshipType: 'SOURCES' });
      setCycleTest({ ok: false, message: 'Unexpected: the edge was created. Cycle detection did not fire.' });
      await refresh();
    } catch (err) {
      if (handleSessionExpiry(err)) return;
      if (err instanceof ApiError && err.status === 422) {
        setCycleTest({ ok: true, message: `Correctly rejected: ${err.message}` });
      } else {
        setCycleTest({ ok: false, message: err instanceof ApiError ? err.message : 'Request failed.' });
      }
    } finally {
      setBusy(false);
    }
  }

  async function handleSeedDemo() {
    const existingNames = new Set(orgs.map((o) => o.legalName));
    const toAdd = DEMO_SUPPLIERS.filter((s) => !existingNames.has(s.legalName));
    if (toAdd.length === 0) return;

    setError(null);
    setBusy(true);
    try {
      for (const s of toAdd) {
        const supplier = await createOrganization(session.accessToken, {
          legalName: s.legalName,
          country: s.country,
          contactEmail: `contact@${s.legalName.toLowerCase().replace(/\s+/g, '')}.example`,
        });
        await addEdge(session.accessToken, { childOrgId: supplier.orgId, relationshipType: s.relationshipType });
      }
      await refresh();
    } catch (err) {
      if (handleSessionExpiry(err)) return;
      setError(err instanceof ApiError ? err.message : 'Failed to seed demo suppliers');
    } finally {
      setBusy(false);
    }
  }

  const myOrg = orgs.find((o) => o.orgId === session.orgId);
  const seedAvailable = DEMO_SUPPLIERS.some((s) => !orgs.some((o) => o.legalName === s.legalName));

  const tierByOrgId = new Map((tree?.nodes ?? []).map((n) => [n.orgId, n.depth]));
  const statusCounts = orgs.reduce<Record<string, number>>((acc, o) => {
    acc[o.status] = (acc[o.status] ?? 0) + 1;
    return acc;
  }, {});
  const deepestTier = tree && tree.nodes.length > 0 ? Math.max(...tree.nodes.map((n) => n.depth)) : 0;
  const tiers = [...new Set((tree?.nodes ?? []).map((n) => n.depth))].sort((a, b) => a - b);

  return (
    <div className="dashboard">
      <header className="topbar">
        <div>
          <strong>Echo-Trace</strong>
          <span className="org-name">{myOrg?.legalName ?? '…'}</span>
        </div>
        <div className="session-info">
          <span>
            {session.email} · <code>{session.role}</code>
          </span>
          <button onClick={() => onLogout()} type="button">
            Log out
          </button>
        </div>
      </header>

      <div className="tagline">
        Tiered Supply Chain Transparency &amp; Audit Platform — trace materials and
        certifications across every supplier tier, not just direct (Tier&nbsp;1) vendors.
      </div>

      {error && <div className="error banner">{error}</div>}

      <div className="stat-strip">
        <div className="stat-tile">
          <span className="stat-value">{orgs.length}</span>
          <span className="stat-label">Organizations onboarded</span>
        </div>
        <div className="stat-tile">
          <span className="stat-value">
            {statusCounts.Active ?? 0}
            <span className="stat-muted"> active</span>
            {statusCounts.Pending ? <span className="stat-muted"> · {statusCounts.Pending} pending</span> : null}
          </span>
          <span className="stat-label">Verification status</span>
        </div>
        <div className="stat-tile">
          <span className="stat-value">Tier {deepestTier}</span>
          <span className="stat-label">Deepest tier traced</span>
        </div>
      </div>

      <div className="grid">
        <section className="panel">
          <div className="panel-header">
            <h2>Organizations ({orgs.length})</h2>
            {session.role === 'ORG_ADMIN' && seedAvailable && (
              <button type="button" className="ghost" onClick={handleSeedDemo} disabled={busy}>
                Seed demo suppliers
              </button>
            )}
          </div>
          <table>
            <thead>
              <tr>
                <th>Legal name</th>
                <th>Tier</th>
                <th>Country</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {orgs.map((o) => (
                <tr key={o.orgId} className={o.orgId === session.orgId ? 'self' : ''}>
                  <td>{o.legalName}</td>
                  <td>{tierByOrgId.has(o.orgId) ? `Tier ${tierByOrgId.get(o.orgId)}` : '—'}</td>
                  <td>{o.country}</td>
                  <td>
                    <span className={`badge ${o.status.toLowerCase()}`}>{o.status}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          {session.role === 'ORG_ADMIN' && (
            <form onSubmit={handleAddSupplier} className="add-supplier-form">
              <h3>Onboard a supplier</h3>
              <input required placeholder="Legal name" value={supplierName} onChange={(e) => setSupplierName(e.target.value)} />
              <div className="row">
                <input
                  required
                  maxLength={2}
                  placeholder="Country (ISO-2)"
                  value={supplierCountry}
                  onChange={(e) => setSupplierCountry(e.target.value.toUpperCase())}
                />
                <select value={relationshipType} onChange={(e) => setRelationshipType(e.target.value)}>
                  <option value="SOURCES">SOURCES</option>
                  <option value="MANUFACTURES">MANUFACTURES</option>
                  <option value="DISTRIBUTES">DISTRIBUTES</option>
                </select>
              </div>
              <input
                type="email"
                placeholder="Contact email (optional)"
                value={supplierEmail}
                onChange={(e) => setSupplierEmail(e.target.value)}
              />
              <button type="submit" className="primary" disabled={busy}>
                {busy ? 'Adding…' : 'Add & link supplier'}
              </button>
            </form>
          )}

          {session.role === 'ORG_ADMIN' && (
            <div className="cycle-test">
              <h3>Cycle detection</h3>
              <p className="hint-text">
                Tries to link your own org as its own supplier. The backend walks the existing
                graph before writing anything, so this should be rejected — not silently allowed.
              </p>
              <button type="button" className="ghost" onClick={handleCycleTest} disabled={busy}>
                Test cycle detection
              </button>
              {cycleTest && (
                <div className={`cycle-result ${cycleTest.ok ? 'ok' : 'fail'}`}>{cycleTest.message}</div>
              )}
            </div>
          )}
        </section>

        <section className="panel">
          <h2>Supply chain graph</h2>
          {tree ? <SupplyChainGraph tree={tree} rootOrgId={session.orgId} /> : <p>Loading…</p>}
          <p className="caption">
            Rendered from a live query — depth/path come from a genuine SQL Server{' '}
            <strong>recursive CTE</strong> walking the edge table, not an in-memory graph walk.
          </p>
        </section>

        <section className="panel span-2">
          <h2>Audit trail (hash-chained)</h2>
          <p className="caption">
            Every mutating command logs an entry automatically. Each <code>chainHash</code>{' '}
            incorporates the previous entry's hash for that entity — tamper with any stored row
            and every hash after it breaks.
          </p>
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Entity</th>
                <th>Action</th>
                <th>When</th>
                <th>Chain hash</th>
              </tr>
            </thead>
            <tbody>
              {auditLog.map((entry) => (
                <tr key={entry.auditId}>
                  <td>{entry.auditId}</td>
                  <td>{entry.entityType}</td>
                  <td>{entry.action}</td>
                  <td>{new Date(entry.occurredAt).toLocaleString()}</td>
                  <td className="hash">{entry.chainHash.slice(0, 16)}…</td>
                </tr>
              ))}
              {auditLog.length === 0 && (
                <tr>
                  <td colSpan={5}>No audit entries yet.</td>
                </tr>
              )}
            </tbody>
          </table>
        </section>

        <Documents session={session} onSessionExpired={handleSessionExpiry} />

        <section className="panel span-2 dpp-card">
          <div className="panel-header">
            <h2>Digital Product Passport</h2>
            <span className="badge-mockup">PREVIEW — not backed by a live endpoint</span>
          </div>
          <p className="caption">
            Illustrative only. Phase 2 will generate this as a signed (HMAC-SHA256), downloadable
            report per product, built from real certification data attached to each supplier.
            The chain below is real — it's read from the graph on the left.
          </p>

          <div className="dpp-body">
            <div className="dpp-row">
              <span className="dpp-label">Sample product</span>
              <span className="dpp-value">Organic Cotton T-Shirt (illustrative — Products aren't modeled yet)</span>
            </div>
            <div className="dpp-row">
              <span className="dpp-label">Traced chain</span>
              <span className="dpp-value">
                {tiers.length === 0
                  ? 'No suppliers onboarded yet.'
                  : tiers.map((depth) => {
                      const names = (tree?.nodes ?? [])
                        .filter((n) => n.depth === depth)
                        .map((n) => n.legalName)
                        .join(', ');
                      return (
                        <div key={depth}>
                          <span className="tier-chip">Tier {depth}</span> {names}
                        </div>
                      );
                    })}
              </span>
            </div>
            <div className="dpp-row">
              <span className="dpp-label">Compliance score</span>
              <span className="dpp-value dpp-placeholder">— requires certification data (Phase 2)</span>
            </div>
          </div>

          <div className="dpp-actions">
            <button type="button" className="ghost" disabled title="Not implemented — Phase 2 roadmap item">
              Export PDF
            </button>
            <button type="button" className="ghost" disabled title="Not implemented — Phase 2 roadmap item">
              Verify signature
            </button>
          </div>
        </section>
      </div>
    </div>
  );
}
