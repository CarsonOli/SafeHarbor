import { buildAuthHeaders } from './authHeaders'
import type {
  PagedResult,
  PagingQuery,
  DonorListItem,
  ResidentCaseListItem,
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

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const err = (await response.json().catch(() => null)) as ApiErrorEnvelope | null
    throw new Error(err?.message ?? `Request failed with status ${response.status}`)
  }

  return (await response.json()) as T
}

export async function fetchDonors(query: PagingQuery): Promise<PagedResult<DonorListItem>> {
  const response = await fetch(`${API_BASE}/api/admin/donors-contributions/donors?${toQueryString(query)}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<PagedResult<DonorListItem>>(response)
}

export async function createDonor(name: string, email: string): Promise<void> {
  const response = await fetch(`${API_BASE}/api/admin/donors-contributions/donors`, {
    method: 'POST',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json', Accept: 'application/json' }),
    body: JSON.stringify({ name, email }),
  })

  await readJson<unknown>(response)
}

export async function fetchResidentCases(query: PagingQuery): Promise<PagedResult<ResidentCaseListItem>> {
  const response = await fetch(`${API_BASE}/api/admin/caseload/residents?${toQueryString(query)}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<PagedResult<ResidentCaseListItem>>(response)
}

export async function fetchProcessRecordings(query: PagingQuery): Promise<PagedResult<ProcessRecordItem>> {
  const response = await fetch(`${API_BASE}/api/admin/process-recordings?${toQueryString(query)}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<PagedResult<ProcessRecordItem>>(response)
}

export async function createProcessRecording(residentCaseId: string, summary: string): Promise<void> {
  const response = await fetch(`${API_BASE}/api/admin/process-recordings`, {
    method: 'POST',
    headers: buildAuthHeaders({ 'Content-Type': 'application/json', Accept: 'application/json' }),
    body: JSON.stringify({ residentCaseId, summary }),
  })
  await readJson<unknown>(response)
}

export async function fetchVisitLogs(query: PagingQuery): Promise<PagedResult<HomeVisitItem>> {
  const response = await fetch(`${API_BASE}/api/admin/visitation-conferences/visits?${toQueryString(query)}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<PagedResult<HomeVisitItem>>(response)
}

export async function fetchUpcomingConferences(query: PagingQuery): Promise<PagedResult<CaseConferenceItem>> {
  const response = await fetch(`${API_BASE}/api/admin/visitation-conferences/conferences/upcoming?${toQueryString(query)}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<PagedResult<CaseConferenceItem>>(response)
}

export async function fetchPreviousConferences(query: PagingQuery): Promise<PagedResult<CaseConferenceItem>> {
  const response = await fetch(`${API_BASE}/api/admin/visitation-conferences/conferences/previous?${toQueryString(query)}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<PagedResult<CaseConferenceItem>>(response)
}
