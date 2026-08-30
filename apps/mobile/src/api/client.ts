import { config } from '@/config/env';

/**
 * Thin API client boundary. All backend calls go through here so auth headers,
 * base URL, and error shaping live in one place. No business logic.
 */
export class ApiError extends Error {
  constructor(
    readonly status: number,
    message: string,
    readonly body?: unknown,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export interface CaseManifestEntry {
  id: string;
  version: number;
  title: string;
  chiefComplaint: string;
  specialty: string;
  estimatedMinutes: number;
  clinicalReviewStatus: string;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${config.apiBaseUrl}${path}`, {
    ...init,
    headers: { 'content-type': 'application/json', ...(init?.headers ?? {}) },
  });
  const text = await res.text();
  const body: unknown = text ? JSON.parse(text) : undefined;
  if (!res.ok) {
    throw new ApiError(res.status, `${init?.method ?? 'GET'} ${path} -> ${res.status}`, body);
  }
  return body as T;
}

export const apiClient = {
  health: () => request<{ status: string }>('/health'),
  listCases: () => request<{ cases: CaseManifestEntry[] }>('/cases'),
  getCase: (id: string, version?: number) =>
    request<Record<string, unknown>>(`/cases/${id}${version ? `?version=${version}` : ''}`),
  startAttempt: (caseId: string, caseVersion: number, difficulty: 'standard' | 'hard') =>
    request<{ attemptId: string; seed: number }>('/attempts', {
      method: 'POST',
      body: JSON.stringify({ caseId, caseVersion, difficulty }),
    }),
};
