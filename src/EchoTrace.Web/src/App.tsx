import { useEffect, useState } from 'react';
import './App.css';
import { decodeJwt } from './api';
import { Dashboard } from './components/Dashboard';
import { LoginPage } from './components/LoginPage';
import type { Session } from './types';

const STORAGE_KEY = 'echotrace.session';

function loadSession(): Session | null {
  const raw = localStorage.getItem(STORAGE_KEY);
  if (!raw) return null;
  try {
    const session = JSON.parse(raw) as Session;
    decodeJwt(session.accessToken); // throws if malformed
    return session;
  } catch {
    return null;
  }
}

export default function App() {
  const [session, setSession] = useState<Session | null>(() => loadSession());
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    if (session) localStorage.setItem(STORAGE_KEY, JSON.stringify(session));
    else localStorage.removeItem(STORAGE_KEY);
  }, [session]);

  function handleLogin(newSession: Session) {
    setNotice(null);
    setSession(newSession);
  }

  function handleLogout(message?: string) {
    setSession(null);
    setNotice(message ?? null);
  }

  if (!session) return <LoginPage onLogin={handleLogin} notice={notice} />;
  return <Dashboard session={session} onLogout={handleLogout} />;
}
