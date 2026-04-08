import type { DonorDashboardData } from '../types/impact'
import { buildAuthHeaders } from './authHeaders'

// Base URL for API calls. Defaults to relative (same origin) in production.
// Override with VITE_API_BASE_URL in .env.local for local development pointing to a different port.
const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''

const DONOR_DASHBOARD_ENDPOINT = '/api/donor/dashboard'
const DONOR_CONTRIBUTION_ENDPOINT = '/api/donor/contribution'

// ── Fallback data ─────────────────────────────────────────────────────────────
// Used when the backend is unavailable (e.g. running frontend in isolation).
// Values match the seed data in DonorDashboardSeeder.cs so visual testing is consistent.
// Alice's 14 seeded contributions total $2,550; $2,550 / $47 = 54 girls helped.
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
    { month: '2025-10', amount: 150 }, // two donations in Oct aggregated
    { month: '2025-11', amount: 300 },
    { month: '2025-12', amount: 500 },
    { month: '2026-01', amount: 375 }, // two donations in Jan aggregated
    { month: '2026-02', amount: 150 },
    { month: '2026-03', amount: 250 },
  ],
  activeCampaign: {
    campaignId: '00000000-0003-0000-0000-000000000001',
    campaignName: 'Spring 2026 Safe Homes Drive',
    goalAmount: 50000,
    // Alice $2,550 + Bob $850 = $3,400 total raised
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
 * Fetches the full donor dashboard for the given email address.
 *
 * The email comes from the frontend auth session (localStorage) and is sent as a query
 * parameter. When real Entra ID auth is wired, the backend will read the email from the
 * JWT claim instead, but this call signature stays the same.
 *
 * Falls back to static mock data if the API call fails, so the page still renders
 * during local frontend development without a running backend.
 */
export async function fetchDonorDashboard(email: string): Promise<DonorDashboardData> {
  try {
    const url = `${API_BASE}${DONOR_DASHBOARD_ENDPOINT}?email=${encodeURIComponent(email)}`
    const response = await fetch(url, {
      method: 'GET',
      headers: buildAuthHeaders({ Accept: 'application/json' }),
    })

    if (!response.ok) {
      console.warn(`[donorApi] Dashboard fetch returned ${response.status} — using fallback data`)
      return FALLBACK_DASHBOARD
    }

    return (await response.json()) as DonorDashboardData
  } catch (err) {
    console.warn('[donorApi] Dashboard fetch failed — using fallback data', err)
    return FALLBACK_DASHBOARD
  }
}

/**
 * Submits a new donation for the donor identified by email.
 *
 * After a successful response, the caller should re-fetch the dashboard via
 * fetchDonorDashboard() so all metrics (lifetime total, impact count, campaign progress)
 * update to reflect the new contribution.
 *
 * @param email - The donor's email from the auth session.
 * @param amount - Donation amount in USD. Must be > 0.
 * @param campaignId - Optional campaign GUID. If omitted, the backend auto-assigns to the active campaign.
 * @returns A success message string, or throws an Error if the request failed.
 */
export async function submitDonation(
  email: string,
  amount: number,
  campaignId?: string,
): Promise<string> {
  const body: { email: string; amount: number; campaignId?: string } = { email, amount }
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
    // Surface the error so the page can display it to the donor.
    const errorData = (await response.json().catch(() => ({}))) as { error?: string }
    throw new Error(errorData.error ?? `Donation failed with status ${response.status}`)
  }

  const result = (await response.json()) as { message: string }
  return result.message
}
