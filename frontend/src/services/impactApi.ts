import type {
  CreateSocialPostMetricRequest,
  ImpactSummary,
  ReportsAnalyticsResponse,
  SocialPostMetricListItem,
} from '../types/impact'
import { buildAuthHeaders } from './authHeaders'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''
const IMPACT_ENDPOINT = import.meta.env.VITE_IMPACT_AGGREGATE_PATH ?? '/api/impact/aggregate'
const REPORTS_ENDPOINT = import.meta.env.VITE_REPORTS_ANALYTICS_PATH ?? '/api/admin/reports-analytics'
const SOCIAL_METRICS_ENDPOINT =
  import.meta.env.VITE_SOCIAL_POST_METRICS_PATH ?? '/api/admin/social-post-metrics'

const fallbackImpactData: ImpactSummary = {
  generatedAt: '2026-04-01T00:00:00.000Z',
  metrics: [
    { label: 'Households Supported', value: 482, changePercent: 8.4 },
    { label: 'Referrals Completed', value: 317, changePercent: 5.2 },
    { label: 'Partner Programs Active', value: 28, changePercent: 12.6 },
  ],
  monthlyTrend: [
    { month: 'Nov', assistedHouseholds: 57 },
    { month: 'Dec', assistedHouseholds: 61 },
    { month: 'Jan', assistedHouseholds: 70 },
    { month: 'Feb', assistedHouseholds: 74 },
    { month: 'Mar', assistedHouseholds: 83 },
  ],
  outcomes: [
    { category: 'Safe Housing', count: 198 },
    { category: 'Medical Stabilization', count: 121 },
    { category: 'Legal Aid', count: 74 },
    { category: 'Employment Transition', count: 89 },
  ],
}

const fallbackReportsData: ReportsAnalyticsResponse = {
  donationTrends: [
    { month: '2026-01', amount: 22000 },
    { month: '2026-02', amount: 24800 },
    { month: '2026-03', amount: 26300 },
  ],
  outcomeTrends: [
    { month: '2026-01', residentsServed: 134, homeVisits: 212 },
    { month: '2026-02', residentsServed: 145, homeVisits: 226 },
    { month: '2026-03', residentsServed: 152, homeVisits: 241 },
  ],
  safehouseComparisons: [],
  reintegrationRates: [],
  donationCorrelationByPlatform: [
    {
      group: 'Instagram',
      posts: 14,
      totalReach: 31900,
      totalEngagements: 2810,
      totalAttributedDonationAmount: 7400,
      totalAttributedDonationCount: 71,
      donationsPer1kReach: 231.97,
      engagementRatePercent: 8.81,
    },
    {
      group: 'Facebook',
      posts: 10,
      totalReach: 25500,
      totalEngagements: 1710,
      totalAttributedDonationAmount: 3800,
      totalAttributedDonationCount: 42,
      donationsPer1kReach: 149.02,
      engagementRatePercent: 6.71,
    },
  ],
  donationCorrelationByContentType: [
    {
      group: 'Story video',
      posts: 11,
      totalReach: 27800,
      totalEngagements: 2480,
      totalAttributedDonationAmount: 6900,
      totalAttributedDonationCount: 66,
      donationsPer1kReach: 248.2,
      engagementRatePercent: 8.92,
    },
  ],
  donationCorrelationByPostingHour: [
    {
      group: '18:00',
      posts: 9,
      totalReach: 19100,
      totalEngagements: 1690,
      totalAttributedDonationAmount: 4300,
      totalAttributedDonationCount: 39,
      donationsPer1kReach: 225.13,
      engagementRatePercent: 8.85,
    },
  ],
  topAttributedPosts: [],
  recommendations: [
    {
      title: 'Lean into top donation platform',
      rationale: 'Instagram currently returns the strongest donation attribution per reach.',
      action: 'Increase Instagram publishing cadence by 2 posts/week for the next reporting cycle.',
    },
  ],
}

export async function fetchImpactSummary(): Promise<ImpactSummary> {
  try {
    // NOTE: The dashboard must remain read-only and aggregate-only.
    // This client call intentionally requests only rolled-up analytics data.
    const response = await fetch(`${API_BASE}${IMPACT_ENDPOINT}`, {
      method: 'GET',
      headers: buildAuthHeaders({
        Accept: 'application/json',
      }),
    })

    if (!response.ok) {
      throw new Error(`Impact endpoint returned ${response.status}`)
    }

    const data = (await response.json()) as ImpactSummary
    return data
  } catch {
    // NOTE: Local fallback keeps the UI testable before backend rollout.
    return fallbackImpactData
  }
}

export async function fetchReportsAnalytics(): Promise<ReportsAnalyticsResponse> {
  try {
    const response = await fetch(`${API_BASE}${REPORTS_ENDPOINT}`, {
      method: 'GET',
      headers: buildAuthHeaders({
        Accept: 'application/json',
      }),
    })

    if (!response.ok) {
      throw new Error(`Reports endpoint returned ${response.status}`)
    }

    return (await response.json()) as ReportsAnalyticsResponse
  } catch {
    // NOTE: Fallback keeps report UI deterministic during local, unauthenticated development.
    return fallbackReportsData
  }
}

export async function fetchSocialPostMetrics(): Promise<SocialPostMetricListItem[]> {
  const response = await fetch(`${API_BASE}${SOCIAL_METRICS_ENDPOINT}`, {
    method: 'GET',
    headers: buildAuthHeaders({
      Accept: 'application/json',
    }),
  })

  if (!response.ok) {
    throw new Error(`Social post metrics endpoint returned ${response.status}`)
  }

  return (await response.json()) as SocialPostMetricListItem[]
}

export async function createSocialPostMetric(
  request: CreateSocialPostMetricRequest,
): Promise<SocialPostMetricListItem> {
  const response = await fetch(`${API_BASE}${SOCIAL_METRICS_ENDPOINT}`, {
    method: 'POST',
    headers: buildAuthHeaders({
      'Content-Type': 'application/json',
      Accept: 'application/json',
    }),
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(`Create social post metric endpoint returned ${response.status}`)
  }

  return (await response.json()) as SocialPostMetricListItem
}
