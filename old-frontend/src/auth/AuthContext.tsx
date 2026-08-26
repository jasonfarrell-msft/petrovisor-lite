import { createContext, useCallback, useContext, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { login as loginRequest } from '../api/endpoints';
import { getStoredToken, setStoredToken } from '../api/client';
import { decodeJwt } from './jwt';
import type { AuthUser, UserRole } from '../types';

interface AuthContextValue {
  token: string | null;
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

function userFromToken(token: string | null): AuthUser | null {
  if (!token) return null;
  const payload = decodeJwt(token);
  if (!payload) return null;
  const username = (payload.username as string) ?? (payload.sub as string) ?? 'unknown';
  const role = ((payload.role as string) ?? 'Viewer') as UserRole;
  return { username, role };
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(() => getStoredToken());

  const user = useMemo(() => userFromToken(token), [token]);

  const login = useCallback(async (username: string, password: string) => {
    const { token: newToken } = await loginRequest(username, password);
    setStoredToken(newToken);
    setToken(newToken);
  }, []);

  const logout = useCallback(() => {
    setStoredToken(null);
    setToken(null);
  }, []);

  const value: AuthContextValue = {
    token,
    user,
    isAuthenticated: token !== null,
    login,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return ctx;
}
