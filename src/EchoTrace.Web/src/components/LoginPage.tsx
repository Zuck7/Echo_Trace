import { useState } from 'react';
import { ApiError, decodeJwt, login, register } from '../api';
import type { Session } from '../types';

export function LoginPage({ onLogin }: { onLogin: (session: Session) => void }) {
  const [mode, setMode] = useState<'login' | 'register'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [orgLegalName, setOrgLegalName] = useState('');
  const [orgCountry, setOrgCountry] = useState('US');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function doLogin(loginEmail: string, loginPassword: string) {
    const result = await login(loginEmail, loginPassword);
    const claims = decodeJwt(result.accessToken);
    onLogin({ accessToken: result.accessToken, email: claims.email, role: claims.role, orgId: claims.orgId });
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      if (mode === 'register') {
        await register({ email, password, orgLegalName, orgCountry });
      }
      await doLogin(email, password);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Something went wrong. Is the API running?');
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="auth-shell">
      <div className="pitch-panel">
        <h1>Echo-Trace</h1>
        <p className="subtitle">Tiered Supply Chain Transparency &amp; Audit Platform</p>
        <p className="pitch-lede">
          Buyers can usually verify their direct (Tier 1) suppliers — but Tier 2, 3, and deeper
          suppliers stay invisible, which is exactly where forced labor, unsafe conditions, and
          environmental violations hide.
        </p>
        <ul className="pitch-list">
          <li>
            <strong>Trace unlimited tiers.</strong> The supply chain is a real graph — organizations
            are nodes, supplier relationships are edges — walked with a SQL recursive CTE.
          </li>
          <li>
            <strong>Tamper-evident by construction.</strong> Every change writes a SHA-256
            hash-chained audit entry. Edit history quietly and every hash after it breaks.
          </li>
          <li>
            <strong>Multi-tenant by design.</strong> Each buyer's data is isolated at the database
            query layer, not just in application code.
          </li>
        </ul>
      </div>

      <div className="auth-card">
        <div className="tabs">
          <button className={mode === 'login' ? 'active' : ''} onClick={() => setMode('login')} type="button">
            Log in
          </button>
          <button className={mode === 'register' ? 'active' : ''} onClick={() => setMode('register')} type="button">
            Register org
          </button>
        </div>

        <form onSubmit={handleSubmit}>
          <label>
            Email
            <input type="email" required value={email} onChange={(e) => setEmail(e.target.value)} placeholder="admin@acme-buyer.com" />
          </label>
          <label>
            Password
            <input type="password" required minLength={8} value={password} onChange={(e) => setPassword(e.target.value)} />
          </label>

          {mode === 'register' && (
            <>
              <label>
                Organization legal name
                <input required value={orgLegalName} onChange={(e) => setOrgLegalName(e.target.value)} placeholder="Acme Buyer Corp" />
              </label>
              <label>
                Country (ISO-2)
                <input required maxLength={2} value={orgCountry} onChange={(e) => setOrgCountry(e.target.value.toUpperCase())} />
              </label>
            </>
          )}

          {error && <div className="error">{error}</div>}

          <button type="submit" className="primary" disabled={busy}>
            {busy ? 'Working…' : mode === 'login' ? 'Log in' : 'Create organization & log in'}
          </button>
        </form>

        <p className="hint">
          Seeded platform admin: <code>admin@echotrace.dev</code> / <code>ChangeMe123!</code>
        </p>
      </div>
    </div>
  );
}
