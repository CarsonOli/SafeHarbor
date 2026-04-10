import { buildAuthHeaders } from './authHeaders'
import { HttpError, NOT_AUTHORIZED_MESSAGE } from './httpErrors'
import type { DonationFilters, DonationListItem, PagedResult, YourDonationsResponse } from '../types/donations'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

function buildQuery(filters: DonationFilters): string {
  const params = new URLSearchParams()
  if (filters.fromDate) params.set('fromDate', filters.fromDate)
  if (filters.toDate) params.set('toDate', filters.toDate)
  if (filters.donationType) params.set('donationType', filters.donationType)
  if (filters.campaign) params.set('campaign', filters.campaign)
  if (filters.channelSource) params.set('channelSource', filters.channelSource)
  if (filters.supporterType) params.set('supporterType', filters.supporterType)
  if (filters.frequency) params.set('frequency', filters.frequency)
  if (filters.page) params.set('page', String(filters.page))
  if (filters.pageSize) params.set('pageSize', String(filters.pageSize))
  return params.toString()
}

async function readJson<T>(response: Response, endpoint: string, method = 'GET'): Promise<T> {
  if (!response.ok) {
    if (response.status === 403) {
      throw new HttpError(403, NOT_AUTHORIZED_MESSAGE, { endpoint, method })
    }

    const errorBody = (await response.json().catch(() => null)) as { error?: string; message?: string } | null
    throw new HttpError(
      response.status,
      errorBody?.message ?? errorBody?.error ?? `Request failed with status ${response.status}`,
      { endpoint, method },
    )
  }

  return (await response.json()) as T
}

export async function fetchAllDonations(filters: DonationFilters): Promise<PagedResult<DonationListItem>> {
  const endpoint = '/api/admin/donations'
  const queryString = buildQuery(filters)
  const url = queryString ? `${API_BASE}${endpoint}?${queryString}` : `${API_BASE}${endpoint}`
  const response = await fetch(url, { headers: buildAuthHeaders({ Accept: 'application/json' }) })
  return readJson<PagedResult<DonationListItem>>(response, endpoint)
}

export async function fetchCurrentUserDonations(): Promise<YourDonationsResponse> {
  const endpoint = '/api/donor/donations'
  const response = await fetch(`${API_BASE}${endpoint}`, {
    headers: buildAuthHeaders({ Accept: 'application/json' }),
  })
  return readJson<YourDonationsResponse>(response, endpoint)
}
