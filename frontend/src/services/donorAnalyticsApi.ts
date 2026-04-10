import type { DonorAnalyticsData } from '../types/impact'
import { buildAuthHeaders } from './authHeaders'
import { HttpError, NOT_AUTHORIZED_MESSAGE } from './httpErrors'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''
const ANALYTICS_ENDPOINT = '/api/admin/donor-analytics'
const ENABLE_DONOR_ANALYTICS_DEV_FALLBACK =
  (import.meta.env.VITE_ENABLE_DONOR_ANALYTICS_DEV_FALLBACK ?? 'false') === 'true'

// ── Fallback data ─────────────────────────────────────────────────────────────
// Used when the backend is unavailable (e.g. running frontend without a backend).
// Values match the seed data from DonorDashboardSeeder.cs for consistency:
//   Alice: 14 contributions totaling $2,550
//   Bob:   3 contributions totaling $850
//   Total: $3,400 across 17 contributions, 2 donors, 1 campaign
const FALLBACK_ANALYTICS: DonorAnalyticsData = {
  totalDonationsReceived: 3400,
  totalDonorCount: 2,
  activeDonorCount: 2,
  retentionRate: 100,   // both donors have multiple contributions
  averageGiftSize: 200, // $3,400 / 17 ≈ $200
  totalContributionCount: 17,
  monthlyTrend: [
    { month: '2025-04', amount: 100,  newDonors: 2 }, // Alice + Bob both seeded from Apr
    { month: '2025-05', amount: 50,   newDonors: 0 },
    { month: '2025-06', amount: 200,  newDonors: 0 },
    { month: '2025-07', amount: 75,   newDonors: 0 },
    { month: '2025-08', amount: 150,  newDonors: 0 },
    { month: '2025-09', amount: 250,  newDonors: 0 },
    { month: '2025-10', amount: 150,  newDonors: 0 },
    { month: '2025-11', amount: 300,  newDonors: 0 },
    { month: '2025-12', amount: 500,  newDonors: 0 },
    { month: '2026-01', amount: 725,  newDonors: 0 }, // Alice $375 + Bob $350
    { month: '2026-02', amount: 400,  newDonors: 0 }, // Alice $150 + Bob $250
    { month: '2026-03', amount: 350,  newDonors: 0 }, // Alice $250 + Bob $100
  ],
  campaigns: [
    {
      campaignId: '00000000-0003-0000-0000-000000000001',
      campaignName: 'Spring 2026 Safe Homes Drive',
      goalAmount: 50000,
      totalRaised: 3400,
      progressPercent: 6.8,
      donorCount: 2,
      contributionCount: 17,
    },
  ],
  topDonors: [
    { displayName: 'Alice Nguyen', lifetimeDonated: 2550, contributionCount: 14 },
    { displayName: 'Bob Chen',     lifetimeDonated: 850,  contributionCount: 3  },
  ],
}

/**
 * Fetches aggregated donor analytics for the admin dashboard.
 *
 * Falls back to static mock data if the API is unavailable, so the page
 * renders correctly during local frontend-only development.
 *
 * Called once on page mount — no polling or caching needed for this read-only view.
 */
export async function fetchDonorAnalytics(): Promise<DonorAnalyticsData> {
  try {
    const response = await fetch(`${API_BASE}${ANALYTICS_ENDPOINT}`, {
      method: 'GET',
      headers: buildAuthHeaders({ Accept: 'application/json' }),
    })

    if (!response.ok) {
      if (response.status === 403) {
        // Do not mask policy denials with fallback content; page should show explicit auth state.
        throw new HttpError(403, NOT_AUTHORIZED_MESSAGE)
      }

      if (ENABLE_DONOR_ANALYTICS_DEV_FALLBACK) {
        console.warn(`[donorAnalyticsApi] Analytics fetch returned ${response.status} — using fallback`)
        return FALLBACK_ANALYTICS
      }

      throw new HttpError(response.status, `Donor analytics request failed with status ${response.status}`, {
        method: 'GET',
        endpoint: ANALYTICS_ENDPOINT,
      })
    }

    return (await response.json()) as DonorAnalyticsData
  } catch (err) {
    if (err instanceof HttpError && err.status === 403) {
      throw err
    }

    if (ENABLE_DONOR_ANALYTICS_DEV_FALLBACK) {
      console.warn('[donorAnalyticsApi] Analytics fetch failed — using fallback', err)
      return FALLBACK_ANALYTICS
    }

    if (err instanceof HttpError) {
      throw err
    }

    throw new HttpError(0, 'Donor analytics request failed before receiving an HTTP response.', {
      method: 'GET',
      endpoint: ANALYTICS_ENDPOINT,
    })
  }
}
