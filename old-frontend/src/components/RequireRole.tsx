import type { ReactNode } from 'react';
import { useAuth } from '../auth/AuthContext';
import type { UserRole } from '../types';

interface RequireRoleProps {
  role: UserRole;
  children: ReactNode;
  fallback?: ReactNode;
}

/** Gates children based on the current user's role (e.g. Engineer-only actions). */
export function RequireRole({ role, children, fallback = null }: RequireRoleProps) {
  const { user } = useAuth();
  if (user?.role !== role) {
    return <>{fallback}</>;
  }
  return <>{children}</>;
}
