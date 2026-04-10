import { buildAuthHeaders } from './authHeaders'
import { HttpError, NOT_AUTHORIZED_MESSAGE } from './httpErrors'
import type { ApiErrorEnvelope, DashboardSummaryResponse, NotImplementedEnvelope } from '../types/adminOperations'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''
const ADMIN_DASHBOARD_ENDPOINT = '/api/admin/dashboard'

export type AdminDashboardResult =
  | { kind: 'ready'; summary: DashboardSummaryResponse }
  | { kind: 'notImplemented'; payload: NotImplementedEnvelope }

export async function fetchAdminDashboardSummary(): Promise<AdminDashboardResult> {
  const response = await fetch(`${API_BASE}${ADMIN_DASHBOARD_ENDPOINT}`, {
    method: 'GET',
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })

  if (response.status === 501) {
    const payload = (await response.json().catch(() => null)) as NotImplementedEnvelope | null
    return {
      kind: 'notImplemented',
      payload: payload ?? {
        errorCode: 'NotImplemented.v1',
        message: 'Admin dashboard endpoint is not implemented yet.',
        traceId: 'unknown',
        apiVersion: 'v1',
      },
    }
  }

  if (!response.ok) {
    if (response.status === 403) {
      throw new HttpError(403, NOT_AUTHORIZED_MESSAGE, { method: 'GET', endpoint: ADMIN_DASHBOARD_ENDPOINT })
    }

    const err = (await response.json().catch(() => null)) as ApiErrorEnvelope | null
    throw new HttpError(response.status, err?.message ?? `Request failed with status ${response.status}`, {
      method: 'GET',
      endpoint: ADMIN_DASHBOARD_ENDPOINT,
    })
  }

  return {
    kind: 'ready',
    summary: (await response.json()) as DashboardSummaryResponse,
  }
}
