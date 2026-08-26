// Centralized API client for PetroVisor Lite.
//
// Base URL comes from VITE_API_BASE_URL (see .env.example). All endpoint
// paths below are Luke's conventional assumptions -- Han's backend is being
// built concurrently, so exact paths may still shift. Keeping them here in
// one place makes it a single-point update once Han's contracts land.

const API_BASE_URL: string =
  (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5000';

const TOKEN_STORAGE_KEY = 'petrovisor.jwt';

export function getStoredToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  } catch {
    return null;
  }
}

export function setStoredToken(token: string | null): void {
  try {
    if (token) {
      localStorage.setItem(TOKEN_STORAGE_KEY, token);
    } else {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
    }
  } catch {
    // localStorage unavailable (e.g. private browsing) -- fail silently,
    // auth will just not persist across reloads.
  }
}

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.status = status;
    this.name = 'ApiError';
  }
}

interface RequestOptions extends RequestInit {
  auth?: boolean; // attach Bearer token, default true
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { auth = true, headers, ...rest } = options;
  const finalHeaders: Record<string, string> = {
    ...(headers as Record<string, string> | undefined),
  };

  if (!(rest.body instanceof FormData) && rest.body !== undefined) {
    finalHeaders['Content-Type'] = finalHeaders['Content-Type'] ?? 'application/json';
  }

  if (auth) {
    const token = getStoredToken();
    if (token) {
      finalHeaders['Authorization'] = `Bearer ${token}`;
    }
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...rest,
    headers: finalHeaders,
  });

  if (!response.ok) {
    let message = `Request failed with status ${response.status}`;
    try {
      const body = await response.json();
      message = body?.message ?? message;
    } catch {
      // ignore parse failure, use default message
    }
    throw new ApiError(response.status, message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

// --- REST conventions assumed for Han's backend ---
// Auth:        POST /api/auth/login          { username, password } -> { token }
// Wells:       GET  /api/wells                                       -> Well[]
//              GET  /api/wells/:id                                   -> Well
// Production:  GET  /api/production?wellId=:id                       -> ProductionRecord[]
// CSV import:  POST /api/import/csv           (multipart/form-data)  -> CsvImportResult
// KPI:         GET  /api/kpi/wells/:id                                -> WellKpiSummary
//              GET  /api/kpi/dashboard                                -> DashboardSummary

export const api = {
  get: <T>(path: string, options?: RequestOptions) => request<T>(path, { ...options, method: 'GET' }),
  post: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, {
      ...options,
      method: 'POST',
      body: body instanceof FormData ? body : body !== undefined ? JSON.stringify(body) : undefined,
    }),
};

export { API_BASE_URL };
