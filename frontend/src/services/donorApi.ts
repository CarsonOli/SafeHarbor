import type { DonorDashboardData } from '../types/impact'
import { buildAuthHeaders } from './authHeaders'
import { HttpError } from './httpErrors'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''
const DONOR_DASHBOARD_ENDPOINT = '/api/donor/dashboard'
const DONOR_CONTRIBUTION_ENDPOINT = '/api/donor/contribution'
const ENABLE_DONOR_DASHBOARD_DEV_FALLBACK =
  (import.meta.env.VITE_ENABLE_DONOR_DASHBOARD_DEV_FALLBACK ?? 'false') === 'true'
const STATIC_WEB_APP_FALLBACK_API_HOSTS = [
  'https://safeharborbackend-ggdyhzdggag9d3df.canadacentral-01.azurewebsites.net',
  'https://safeharbor-api-staging.azurewebsites.net',
]

const FALLBACK_DASHBOARD: DonorDashboardData = {
  donorName: 'Alice Nguyen',
  lifetimeDonated: 2550,
  monthlyHistory: [
    { month: '2025-04', amount: 100 },
    { month: '2025-05', amount: 50 },
    { month: '2025-06', amount: 200 },
    { month: '2025-07', amount: 75 },
    { month: '2025-08', amount: 150 },
    { month: '2025-09', amount: 250 },
    { month: '2025-10', amount: 150 },
    { month: '2025-11', amount: 300 },
    { month: '2025-12', amount: 500 },
    { month: '2026-01', amount: 375 },
    { month: '2026-02', amount: 150 },
    { month: '2026-03', amount: 250 },
  ],
  activeCampaign: {
    campaignId: '00000000-0003-0000-0000-000000000001',
    campaignName: 'Spring 2026 Safe Homes Drive',
    goalAmount: 50000,
    totalRaisedAllDonors: 3400,
    thisDonorContributed: 2550,
    progressPercent: 6.8,
  },
  impact: {
    girlsHelped: 54,
    impactLabel: 'girls supported toward safe housing',
    modelVersion: 'rule-based-v1',
  },
}

function resolveDonorApiBaseCandidates(): string[] {
  if (API_BASE) {
    return [API_BASE]
  }

  if (import.meta.env.DEV) {
    return ['', 'https://localhost:7217', 'http://localhost:5264', 'http://localhost:5000']
  }

  if (typeof window !== 'undefined' && window.location.hostname.endsWith('.azurestaticapps.net')) {
    return [...STATIC_WEB_APP_FALLBACK_API_HOSTS, '']
  }

  return ['']
}

export async function fetchDonorDashboard(email: string): Promise<DonorDashboardData> {
  try {
    const baseCandidates = resolveDonorApiBaseCandidates()
    let hadNetworkFailure = false

    for (const baseUrl of baseCandidates) {
      let response: Response

      try {
        response = await fetch(`${baseUrl}${DONOR_DASHBOARD_ENDPOINT}?email=${encodeURIComponent(email)}`, {
          method: 'GET',
          headers: buildAuthHeaders({ Accept: 'application/json' }),
        })
      } catch {
        hadNetworkFailure = true
        continue
      }

      if (response.status === 404 && baseUrl === '' && import.meta.env.DEV && baseCandidates.length > 1) {
        continue
      }

      if ((response.status === 404 || response.status === 405) && baseUrl === '' && !import.meta.env.DEV) {
        if (baseCandidates.length > 1) {
          continue
        }

        throw new HttpError(
          response.status,
          `Donor dashboard endpoint ${DONOR_DASHBOARD_ENDPOINT} is not reachable on this frontend origin (HTTP ${response.status}). ` +
            'Set VITE_API_BASE_URL to the backend URL or configure frontend hosting to proxy /api/* to the API.',
          {
            method: 'GET',
            endpoint: DONOR_DASHBOARD_ENDPOINT,
          },
        )
      }

      if (!response.ok) {
        if (ENABLE_DONOR_DASHBOARD_DEV_FALLBACK) {
          console.warn(`[donorApi] Dashboard fetch returned ${response.status} - using fallback data`)
          return FALLBACK_DASHBOARD
        }

        throw new HttpError(response.status, `Donor dashboard request failed with status ${response.status}`, {
          method: 'GET',
          endpoint: DONOR_DASHBOARD_ENDPOINT,
        })
      }

      return (await response.json()) as DonorDashboardData
    }

    if (hadNetworkFailure) {
      throw new HttpError(0, 'Donor dashboard request failed before receiving an HTTP response.', {
        method: 'GET',
        endpoint: DONOR_DASHBOARD_ENDPOINT,
      })
    }

    throw new HttpError(0, 'Donor dashboard request failed before receiving an HTTP response.', {
      method: 'GET',
      endpoint: DONOR_DASHBOARD_ENDPOINT,
    })
  } catch (err) {
    if (ENABLE_DONOR_DASHBOARD_DEV_FALLBACK) {
      console.warn('[donorApi] Dashboard fetch failed - using fallback data', err)
      return FALLBACK_DASHBOARD
    }

    if (err instanceof HttpError) {
      throw err
    }

    throw new HttpError(0, 'Donor dashboard request failed before receiving an HTTP response.', {
      method: 'GET',
      endpoint: DONOR_DASHBOARD_ENDPOINT,
    })
  }
}

export async function submitDonation(
  email: string,
  amount: number,
  frequency: string,
  campaignId?: string,
): Promise<string> {
  const body: { email: string; amount: number; frequency: string; campaignId?: string } = {
    email,
    amount,
    frequency,
  }

  if (campaignId) {
    body.campaignId = campaignId
  }

  const baseCandidates = resolveDonorApiBaseCandidates()
  let hadNetworkFailure = false

  for (const baseUrl of baseCandidates) {
    let response: Response

    try {
      response = await fetch(`${baseUrl}${DONOR_CONTRIBUTION_ENDPOINT}`, {
        method: 'POST',
        headers: buildAuthHeaders({
          'Content-Type': 'application/json',
          Accept: 'application/json',
        }),
        body: JSON.stringify(body),
      })
    } catch {
      hadNetworkFailure = true
      continue
    }

    if (response.status === 404 && baseUrl === '' && import.meta.env.DEV && baseCandidates.length > 1) {
      continue
    }

    if ((response.status === 404 || response.status === 405) && baseUrl === '' && !import.meta.env.DEV) {
      if (baseCandidates.length > 1) {
        continue
      }

      throw new Error(
        `Donor contribution endpoint ${DONOR_CONTRIBUTION_ENDPOINT} is not reachable on this frontend origin (HTTP ${response.status}). ` +
          'Set VITE_API_BASE_URL to the backend URL or configure frontend hosting to proxy /api/* to the API.',
      )
    }

    if (!response.ok) {
      const errorData = (await response.json().catch(() => ({}))) as { error?: string }
      throw new Error(errorData.error ?? `Donation failed with status ${response.status}`)
    }

    const result = (await response.json()) as { message: string }
    return result.message
  }

  if (hadNetworkFailure) {
    throw new Error('Donation request failed before receiving an HTTP response.')
  }

  throw new Error('Donation request failed before receiving an HTTP response.')
}
