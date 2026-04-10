import type { DonorDashboardData } from '../types/impact'
import { buildAuthHeaders } from './authHeaders'
import { HttpError } from './httpErrors'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''
const DONOR_DASHBOARD_ENDPOINT = '/api/donor/dashboard'
const DONOR_CONTRIBUTION_ENDPOINT = '/api/donor/contribution'
const ENABLE_DONOR_DASHBOARD_DEV_FALLBACK =
  (import.meta.env.VITE_ENABLE_DONOR_DASHBOARD_DEV_FALLBACK ?? 'false') === 'true'

// ── Fallback data ─────────────────────────────────────────────────────────────
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

/**
 * RESTORED: Fetches the full donor dashboard for the given email address.
 */
export async function fetchDonorDashboard(email: string): Promise<DonorDashboardData> {
  try {
    const url = `${API_BASE}${DONOR_DASHBOARD_ENDPOINT}?email=${encodeURIComponent(email)}`
    const response = await fetch(url, {
      method: 'GET',
      headers: buildAuthHeaders({ Accept: 'application/json' }),
    })

    if (!response.ok) {
      if (ENABLE_DONOR_DASHBOARD_DEV_FALLBACK) {
        console.warn(`[donorApi] Dashboard fetch returned ${response.status} — using fallback data`)
        return FALLBACK_DASHBOARD
      }

      throw new HttpError(response.status, `Donor dashboard request failed with status ${response.status}`, {
        method: 'GET',
        endpoint: DONOR_DASHBOARD_ENDPOINT,
      })
    }

    return (await response.json()) as DonorDashboardData
  } catch (err) {
    if (ENABLE_DONOR_DASHBOARD_DEV_FALLBACK) {
      console.warn('[donorApi] Dashboard fetch failed — using fallback data', err)
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

/**
 * Submits a new donation for the donor identified by email.
 */
export async function submitDonation(
  email: string,
  amount: number,
  frequency: string,
  campaignId?: string,
): Promise<string> {
  const body: { email: string; amount: number; frequency: string; campaignId?: string } = { 
    email, 
    amount, 
    frequency 
  }
  
  if (campaignId) body.campaignId = campaignId

  const response = await fetch(`${API_BASE}${DONOR_CONTRIBUTION_ENDPOINT}`, {
    method: 'POST',
    headers: buildAuthHeaders({
      'Content-Type': 'application/json',
      Accept: 'application/json',
    }),
    body: JSON.stringify(body),
  })

  if (!response.ok) {
    const errorData = (await response.json().catch(() => ({}))) as { error?: string }
    throw new Error(errorData.error ?? `Donation failed with status ${response.status}`)
  }

  const result = (await response.json()) as { message: string }
  return result.message
}
