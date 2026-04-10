import { buildAuthHeaders } from './authHeaders'
import { HttpError, NOT_AUTHORIZED_MESSAGE } from './httpErrors'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

export interface DonorRiskFlag {
  donorId:     string
  displayName: string
  level:       'High' | 'Medium' | 'Low'
  score:       number
  signals:     string[]
}

export interface ResidentReadinessFlag {
  residentId: string
  level:      'High' | 'Medium' | 'Low'
  score:      number
  action:     string
  signals:    string[]
}

export async function fetchDonorRiskFlags(): Promise<DonorRiskFlag[]> {
  const endpoint = '/api/admin/ml/donor-risk-flags'
  const response = await fetch(`${API_BASE}${endpoint}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  if (!response.ok) {
    if (response.status === 403) throw new HttpError(403, NOT_AUTHORIZED_MESSAGE, { endpoint })
    throw new HttpError(response.status, `Request failed with status ${response.status}`, { endpoint })
  }
  return response.json() as Promise<DonorRiskFlag[]>
}

export async function fetchResidentReadinessFlags(): Promise<ResidentReadinessFlag[]> {
  const endpoint = '/api/admin/ml/resident-readiness-flags'
  const response = await fetch(`${API_BASE}${endpoint}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  if (!response.ok) {
    if (response.status === 403) throw new HttpError(403, NOT_AUTHORIZED_MESSAGE, { endpoint })
    throw new HttpError(response.status, `Request failed with status ${response.status}`, { endpoint })
  }
  return response.json() as Promise<ResidentReadinessFlag[]>
}
