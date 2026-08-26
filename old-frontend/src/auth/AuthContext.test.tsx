import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, act, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuthProvider, useAuth } from '../auth/AuthContext';
import * as endpoints from '../api/endpoints';

// Base64url-encode a minimal JWT-shaped token: header.payload.signature
function fakeJwt(payload: Record<string, unknown>): string {
  const encode = (obj: unknown) =>
    btoa(JSON.stringify(obj)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
  return `${encode({ alg: 'none' })}.${encode(payload)}.sig`;
}

function TestConsumer() {
  const { isAuthenticated, user, login, logout } = useAuth();
  return (
    <div>
      <span data-testid="auth-state">{isAuthenticated ? 'authenticated' : 'anonymous'}</span>
      <span data-testid="user-role">{user?.role ?? 'none'}</span>
      <button onClick={() => login('luke', 'pw')}>login</button>
      <button onClick={() => logout()}>logout</button>
    </div>
  );
}

describe('AuthContext', () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it('starts unauthenticated with no stored token', () => {
    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>,
    );
    expect(screen.getByTestId('auth-state').textContent).toBe('anonymous');
  });

  it('transitions to authenticated state after login()', async () => {
    const token = fakeJwt({ username: 'luke', role: 'Engineer' });
    vi.spyOn(endpoints, 'login').mockResolvedValue({ token });

    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>,
    );

    const user = userEvent.setup();
    await user.click(screen.getByText('login'));

    await waitFor(() => expect(screen.getByTestId('auth-state').textContent).toBe('authenticated'));
    expect(screen.getByTestId('user-role').textContent).toBe('Engineer');
    expect(localStorage.getItem('petrovisor.jwt')).toBe(token);
  });

  it('transitions back to anonymous after logout()', async () => {
    const token = fakeJwt({ username: 'luke', role: 'Viewer' });
    vi.spyOn(endpoints, 'login').mockResolvedValue({ token });

    render(
      <AuthProvider>
        <TestConsumer />
      </AuthProvider>,
    );

    const user = userEvent.setup();
    await user.click(screen.getByText('login'));
    await waitFor(() => expect(screen.getByTestId('auth-state').textContent).toBe('authenticated'));

    await act(async () => {
      await user.click(screen.getByText('logout'));
    });

    expect(screen.getByTestId('auth-state').textContent).toBe('anonymous');
    expect(localStorage.getItem('petrovisor.jwt')).toBeNull();
  });
});
