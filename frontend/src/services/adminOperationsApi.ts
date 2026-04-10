import { buildAuthHeaders } from './authHeaders'
import { HttpError, NOT_AUTHORIZED_MESSAGE } from './httpErrors'
import type {
  PagedResult,
  PagingQuery,
  DonorListItem,
  ResidentCaseListItem,
  CaseloadLookupsResponse,
  ProcessRecordItem,
  HomeVisitItem,
  CaseConferenceItem,
  ApiErrorEnvelope,
} from '../types/adminOperations'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

function toQueryString(query: PagingQuery): string {
  const params = new URLSearchParams()
  params.set('page', String(query.page))
  params.set('pageSize', String(query.pageSize))
  if (query.search) params.set('search', query.search)
  if (query.desc !== undefined) params.set('desc', String(query.desc))
  if (query.safehouseId) params.set('safehouseId', query.safehouseId)
  if (query.statusStateId) params.set('statusStateId', String(query.statusStateId))
  if (query.categoryId) params.set('categoryId', String(query.categoryId))
  if (query.residentCaseId) params.set('residentCaseId', query.residentCaseId)
  return params.toString()
}

async function readJson<T>(response: Response, endpoint: string, method = 'GET'): Promise<T> {
  if (!response.ok) {
    if (response.status === 403) {
      // Keep authorization-denied handling explicit so pages can render a clear state.
      throw new HttpError(403, NOT_AUTHORIZED_MESSAGE, { endpoint, method })
    }

    const err = (await response.json().catch(() => null)) as ApiErrorEnvelope | null
    throw new HttpError(response.status, err?.message ?? `Request failed with status ${response.status}`, { endpoint, method })
  }

  return (await response.json()) as T
}

export async function fetchDonors(query: PagingQuery): Promise<PagedResult<DonorListItem>> {
  const endpoint = '/api/admin/donors-contributions/donors'
  const response = await fetch(`${API_BASE}${endpoint}?${toQueryString(query)}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<PagedResult<DonorListItem>>(response, endpoint)
}

export async function createDonor(name: string, email: string): Promise<void> {
  const endpoint = '/api/admin/donors-contributions/donors'
  const response = await fetch(`${API_BASE}${endpoint}`, {
    method: 'POST',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json', Accept: 'application/json' }),
    body: JSON.stringify({ name, email }),
  })

  await readJson<unknown>(response, endpoint, 'POST')
}

export async function fetchCaseloadLookups(): Promise<CaseloadLookupsResponse> {
  const endpoint = '/api/admin/caseload/lookups'
  const response = await fetch(`${API_BASE}${endpoint}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<CaseloadLookupsResponse>(response, endpoint)
}

export async function fetchResidentCases(query: PagingQuery): Promise<PagedResult<ResidentCaseListItem>> {
  const endpoint = '/api/admin/caseload/residents'
  const response = await fetch(`${API_BASE}${endpoint}?${toQueryString(query)}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<PagedResult<ResidentCaseListItem>>(response, endpoint)
}

export async function fetchProcessRecordings(query: PagingQuery): Promise<PagedResult<ProcessRecordItem>> {
  const endpoint = '/api/admin/process-recordings'
  const response = await fetch(`${API_BASE}${endpoint}?${toQueryString(query)}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<PagedResult<ProcessRecordItem>>(response, endpoint)
}

export async function createProcessRecording(payload: {
  residentCaseId: string
  socialWorker: string
  sessionType: string
  sessionDurationMinutes: number | null
  emotionalStateObserved: string
  emotionalStateEnd: string
  summary: string
  interventionsApplied: string
  followUpActions: string
  progressNoted: boolean
  concernsFlagged: boolean
  referralMade: boolean
  notesRestricted: string
  recordedAt?: string
}): Promise<ProcessRecordItem> {
  const endpoint = '/api/admin/process-recordings'
  const response = await fetch(`${API_BASE}${endpoint}`, {
    method: 'POST',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json', Accept: 'application/json' }),
    body: JSON.stringify(payload),
  })
  return readJson<ProcessRecordItem>(response, endpoint, 'POST')
}

export async function fetchVisitLogs(query: PagingQuery): Promise<PagedResult<HomeVisitItem>> {
  const endpoint = '/api/admin/visitation-conferences/visits'
  const response = await fetch(`${API_BASE}${endpoint}?${toQueryString(query)}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<PagedResult<HomeVisitItem>>(response, endpoint)
}

export async function fetchUpcomingConferences(query: PagingQuery): Promise<PagedResult<CaseConferenceItem>> {
  const endpoint = '/api/admin/visitation-conferences/conferences/upcoming'
  const response = await fetch(`${API_BASE}${endpoint}?${toQueryString(query)}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<PagedResult<CaseConferenceItem>>(response, endpoint)
}

export async function fetchPreviousConferences(query: PagingQuery): Promise<PagedResult<CaseConferenceItem>> {
  const endpoint = '/api/admin/visitation-conferences/conferences/previous'
  const response = await fetch(`${API_BASE}${endpoint}?${toQueryString(query)}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<PagedResult<CaseConferenceItem>>(response, endpoint)
}

// ─── Resident Management (CRUD: Create & Update) ────────────────────────────

export async function createResident(payload: any): Promise<void> {
  const endpoint = '/api/admin/caseload/residents' // Matching your fetchResidentCases route
  const response = await fetch(`${API_BASE}${endpoint}`, {
    method: 'POST',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json', Accept: 'application/json' }),
    body: JSON.stringify(payload),
  })

  await readJson<unknown>(response, endpoint, 'POST')
}

export async function updateResident(id: string, payload: any): Promise<void> {
  const endpoint = `/api/admin/caseload/residents/${id}`
  const response = await fetch(`${API_BASE}${endpoint}`, {
    method: 'PUT',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json', Accept: 'application/json' }),
    body: JSON.stringify(payload),
  })

  await readJson<unknown>(response, endpoint, 'PUT')
}

export async function deleteResident(id: string): Promise<void> {
  const endpoint = `/api/admin/caseload/residents/${id}`
  const response = await fetch(`${API_BASE}${endpoint}`, {
    method: 'DELETE',
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })

  if (response.status !== 204 && response.status !== 200) {
    await readJson<unknown>(response, endpoint, 'DELETE')
  }
}

export async function createDonorProfile(payload: {
  name: string;
  email: string;
  type: 'Monetary' | 'Volunteer' | 'Skills' | 'In-Kind';
  status: 'Active' | 'Inactive';
}): Promise<void> {
  const endpoint = '/api/admin/donors-contributions/donors'
  const response = await fetch(`${API_BASE}${endpoint}`, {
    method: 'POST',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json' }),
    body: JSON.stringify(payload),
  })
  await readJson<unknown>(response, endpoint, 'POST')
}

export async function updateDonorProfile(id: string, payload: any): Promise<void> {
  const endpoint = `/api/admin/donors-contributions/donors/${id}`
  const response = await fetch(`${API_BASE}${endpoint}`, {
    method: 'PUT',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json' }),
    body: JSON.stringify(payload),
  })
  await readJson<unknown>(response, endpoint, 'PUT')
}

export async function deleteDonorProfile(id: string): Promise<void> {
  const endpoint = `/api/admin/donors-contributions/donors/${id}`
  const response = await fetch(`${API_BASE}${endpoint}`, {
    method: 'DELETE',
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  if (response.status !== 204) await readJson<unknown>(response, endpoint, 'DELETE')
}