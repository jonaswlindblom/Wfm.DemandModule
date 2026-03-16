export const DEFAULT_API_BASE =
    (import.meta as any).env?.VITE_API_BASE ?? "/api/v1";

export function getApiBase(): string {
  return normalizeApiBase(localStorage.getItem("dm_api_base") ?? DEFAULT_API_BASE);
}

export function setApiBase(base: string) {
  localStorage.setItem("dm_api_base", normalizeApiBase(base));
}

export function buildApiUrl(base: string, path: string): string {
  return joinApiPath(normalizeApiBase(base), path);
}

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = localStorage.getItem("dm_token");
  const headers: HeadersInit = {
    "Content-Type": "application/json",
    ...(options.headers ?? {}),
    ...(token ? { Authorization: `Bearer ${token}` } : {})
  };

  const res = await fetch(joinApiPath(getApiBase(), path), { ...options, headers });
  if (!res.ok) {
    const text = await res.text();
    throw new Error(text || `HTTP ${res.status}`);
  }
  const contentType = res.headers.get("content-type") || "";
  if (!contentType.includes("application/json")) {
    return (await res.text()) as any as T;
  }
  return (await res.json()) as T;
}

function normalizeApiBase(base: string): string {
  const trimmed = base.trim();
  if (!trimmed) {
    return "/api/v1";
  }

  if (/^https?:\/\//i.test(trimmed)) {
    const url = new URL(trimmed);
    url.pathname = url.pathname.replace(/\/{2,}/g, "/").replace(/\/$/, "");
    return url.toString().replace(/\/$/, "");
  }

  return trimmed.replace(/\/{2,}/g, "/").replace(/\/$/, "");
}

function joinApiPath(base: string, path: string): string {
  return `${base.replace(/\/$/, "")}/${path.replace(/^\//, "")}`;
}
