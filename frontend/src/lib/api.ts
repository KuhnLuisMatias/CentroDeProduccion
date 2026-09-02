import type { PagedResult, RefreshResponse } from "./types";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api";

export class ApiError extends Error {
  status: number;
  code?: string;

  constructor(status: number, message: string, code?: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
  }
}

export function getToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem("cdp_token");
}

export function getRefreshToken(): string | null {
  if (typeof window === "undefined") return null;
  return window.localStorage.getItem("cdp_refreshToken");
}

export function setTokens(token: string, refreshToken: string): void {
  if (typeof window === "undefined") return;
  window.localStorage.setItem("cdp_token", token);
  window.localStorage.setItem("cdp_refreshToken", refreshToken);
}

export function clearTokens(): void {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem("cdp_token");
  window.localStorage.removeItem("cdp_refreshToken");
}

function parseErrorPayload(payload: unknown, fallback: string): { message: string; code?: string } {
  if (payload && typeof payload === "object") {
    const obj = payload as Record<string, unknown>;
    if (typeof obj.detail === "string" && obj.detail.length > 0) {
      return { message: obj.detail, code: typeof obj.errorCode === "string" ? obj.errorCode : undefined };
    }
    if (typeof obj.title === "string" && obj.title.length > 0) {
      return { message: obj.title, code: typeof obj.errorCode === "string" ? obj.errorCode : undefined };
    }
    if (typeof obj.message === "string" && obj.message.length > 0) {
      return { message: obj.message, code: typeof obj.code === "string" ? obj.code : undefined };
    }
    if (obj.error && typeof obj.error === "object") {
      const err = obj.error as Record<string, unknown>;
      if (typeof err.message === "string") {
        return { message: err.message, code: typeof err.code === "string" ? err.code : undefined };
      }
    }
  }
  return { message: fallback };
}

async function parseResponse<T>(response: Response): Promise<T> {
  const text = await response.text();
  let payload: unknown = null;
  if (text) {
    try {
      payload = JSON.parse(text);
    } catch {
      payload = text;
    }
  }

  if (!response.ok) {
    const { message, code } = parseErrorPayload(payload, `Error ${response.status}`);
    throw new ApiError(response.status, message, code);
  }

  return payload as T;
}

async function rawRequest<T>(
  path: string,
  options: { method?: string; body?: unknown; token?: string | null; skipAuth?: boolean } = {},
): Promise<T> {
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (!options.skipAuth) {
    const token = options.token !== undefined ? options.token : getToken();
    if (token) {
      headers["Authorization"] = `Bearer ${token}`;
    }
  }

  const response = await fetch(`${API_URL}${path}`, {
    method: options.method ?? "GET",
    headers,
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
    cache: "no-store",
  });

  return parseResponse<T>(response);
}

let refreshing: Promise<RefreshResponse> | null = null;

async function performRefresh(): Promise<RefreshResponse> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) {
    throw new ApiError(401, "Sesión expirada");
  }

  if (!refreshing) {
    refreshing = rawRequest<RefreshResponse>("/auth/refresh", {
      method: "POST",
      body: { refreshToken },
      skipAuth: true,
    })
      .then((result) => {
        setTokens(result.token, result.refreshToken);
        return result;
      })
      .finally(() => {
        refreshing = null;
      });
  }

  return refreshing;
}

export async function apiClient<T>(
  path: string,
  options: { method?: string; body?: unknown } = {},
): Promise<T> {
  try {
    return await rawRequest<T>(path, options);
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      try {
        await performRefresh();
        return await rawRequest<T>(path, options);
      } catch (refreshError) {
        clearTokens();
        if (typeof window !== "undefined") {
          window.dispatchEvent(new CustomEvent("cdp:unauthorized"));
        }
        throw refreshError;
      }
    }
    throw error;
  }
}

// The backend clamps pageSize to this cap regardless of what the client asks
// for (e.g. pageSize=500 returns at most 100 items per page).
const PAGE_SIZE_CAP = 100;

// Fetches every page of a paged endpoint and merges the items into a single
// array. Loops with page=1..N (pageSize capped at 100) until the collected
// count reaches the reported totalCount.
export async function fetchAllPages<T>(
  path: string,
  options: { pageSize?: number } = {},
): Promise<T[]> {
  const effectiveSize = Math.min(Math.max(1, options.pageSize ?? PAGE_SIZE_CAP), PAGE_SIZE_CAP);
  const [base, existingQuery] = path.split("?");
  const params = new URLSearchParams(existingQuery ?? "");
  const all: T[] = [];
  let page = 1;

  // Safety bound: never loop forever even if totalCount is inconsistent.
  const maxPages = 10_000;

  while (page <= maxPages) {
    params.set("page", String(page));
    params.set("pageSize", String(effectiveSize));
    const result = await apiClient<PagedResult<T>>(`${base}?${params.toString()}`);
    all.push(...result.items);
    if (result.items.length === 0 || all.length >= result.totalCount) break;
    page += 1;
  }

  return all;
}
