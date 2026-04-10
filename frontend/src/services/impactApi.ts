import type {
  CreateSocialPostMetricRequest,
  ImpactSummary,
  ReportsAnalyticsResponse,
  SocialPostMetricListItem,
} from '../types/impact'
import { buildAuthHeaders } from './authHeaders'
import { HttpError, NOT_AUTHORIZED_MESSAGE } from './httpErrors'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''
const IMPACT_ENDPOINT = import.meta.env.VITE_IMPACT_AGGREGATE_PATH ?? '/api/impact/aggregate'
const REPORTS_ENDPOINT = import.meta.env.VITE_REPORTS_ANALYTICS_PATH ?? '/api/admin/reports-analytics'
const SOCIAL_METRICS_ENDPOINT =
  import.meta.env.VITE_SOCIAL_POST_METRICS_PATH ?? '/api/admin/social-post-metrics'
const ENABLE_DEV_FALLBACK = (import.meta.env.VITE_ENABLE_IMPACT_DEV_FALLBACK ?? 'false') === 'true'
const STATIC_WEB_APP_FALLBACK_API_HOSTS = [
  'https://safeharborbackend-ggdyhzdggag9d3df.canadacentral-01.azurewebsites.net',
  'https://safeharbor-api-staging.azurewebsites.net',
]

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

function resolveReportsApiBaseCandidates(): string[] {
  if (API_BASE) {
    return [API_BASE]
  }

  if (import.meta.env.DEV) {
    // NOTE: Keep the Vite same-origin path first, then explicit local API host fallbacks.
    // This avoids regressions for teams already using a local reverse proxy.
    return ['', 'https://localhost:7217', 'http://localhost:5264', 'http://localhost:5000']
  }

  if (typeof window !== 'undefined' && window.location.hostname.endsWith('.azurestaticapps.net')) {
    // NOTE: Prefer explicit backend hosts first in SWA deployments where /api/* may not be proxied.
    return [...STATIC_WEB_APP_FALLBACK_API_HOSTS, '']
  }

  return ['']
}

function resolveImpactApiBaseCandidates(): string[] {
  if (API_BASE) {
    return [API_BASE]
  }

  if (import.meta.env.DEV) {
    // NOTE: Keep same-origin first for local reverse-proxy setups, then explicit local API fallbacks.
    return ['', 'https://localhost:7217', 'http://localhost:5264', 'http://localhost:5000']
  }

  if (typeof window !== 'undefined' && window.location.hostname.endsWith('.azurestaticapps.net')) {
    // NOTE: SWA deployments may not proxy /api/* by default; prefer explicit backend hosts first.
    return [...STATIC_WEB_APP_FALLBACK_API_HOSTS, '']
  }

  return ['']
}

export async function fetchImpactSummary(): Promise<ImpactSummary> {
  try {
    const baseCandidates = resolveImpactApiBaseCandidates()
    let hadNetworkFailure = false

    for (const baseUrl of baseCandidates) {
      let response: Response

      try {
        // NOTE: The dashboard remains read-only and aggregate-only.
        response = await fetch(`${baseUrl}${IMPACT_ENDPOINT}`, {
          method: 'GET',
          headers: buildAuthHeaders({
            Accept: 'application/json',
          }),
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
          `Impact endpoint ${IMPACT_ENDPOINT} is not reachable on this frontend origin (HTTP ${response.status}). ` +
            'Set VITE_API_BASE_URL to the backend URL or configure frontend hosting to proxy /api/* to the API.',
          {
            method: 'GET',
            endpoint: IMPACT_ENDPOINT,
          },
        )
      }

      if (!response.ok) {
        throw new HttpError(response.status, `Impact endpoint returned ${response.status}`, {
          method: 'GET',
          endpoint: IMPACT_ENDPOINT,
        })
      }

      return (await response.json()) as ImpactSummary
    }

    if (hadNetworkFailure) {
      throw new HttpError(0, 'Unable to load impact summary from backend API.', {
        method: 'GET',
        endpoint: IMPACT_ENDPOINT,
      })
    }

    throw new HttpError(0, 'Unable to load impact summary from backend API.', {
      method: 'GET',
      endpoint: IMPACT_ENDPOINT,
    })
  } catch (error) {
    if (error instanceof HttpError && error.status === 403) {
      throw error
    }

    // NOTE: Mock fallback is intentionally opt-in for local development only.
    // Production/default behavior must surface backend outages to the UI.
    if (ENABLE_DEV_FALLBACK) {
      return fallbackImpactData
    }

    if (error instanceof HttpError) {
      throw error
    }

    throw new HttpError(0, 'Unable to load impact summary from backend API.', {
      method: 'GET',
      endpoint: IMPACT_ENDPOINT,
    })
  }
}

export async function fetchReportsAnalytics(): Promise<ReportsAnalyticsResponse> {
  try {
    const baseCandidates = resolveReportsApiBaseCandidates()
    let hadNetworkFailure = false

    for (const baseUrl of baseCandidates) {
      let response: Response

      try {
        response = await fetch(`${baseUrl}${REPORTS_ENDPOINT}`, {
          method: 'GET',
          headers: buildAuthHeaders({
            Accept: 'application/json',
          }),
        })
      } catch {
        hadNetworkFailure = true
        continue
      }

      if (response.status === 404 && baseUrl === '' && import.meta.env.DEV && baseCandidates.length > 1) {
        // In local Vite sessions, this usually means "/api" hit the frontend server.
        // Continue to explicit backend host candidates before failing the request.
        continue
      }

      if ((response.status === 404 || response.status === 405) && baseUrl === '' && !import.meta.env.DEV) {
        if (baseCandidates.length > 1) {
          continue
        }

        throw new HttpError(
          response.status,
          `Reports analytics endpoint ${REPORTS_ENDPOINT} is not reachable on this frontend origin (HTTP ${response.status}). ` +
            'Set VITE_API_BASE_URL to the backend URL or configure frontend hosting to proxy /api/* to the API.',
          {
            method: 'GET',
            endpoint: REPORTS_ENDPOINT,
          },
        )
      }

      if (!response.ok) {
        if (response.status === 403) {
          throw new HttpError(403, NOT_AUTHORIZED_MESSAGE, { method: 'GET', endpoint: REPORTS_ENDPOINT })
        }

        throw new HttpError(response.status, `Reports endpoint returned ${response.status}`, {
          method: 'GET',
          endpoint: REPORTS_ENDPOINT,
        })
      }

      return (await response.json()) as ReportsAnalyticsResponse
    }

    if (hadNetworkFailure) {
      throw new HttpError(0, 'Unable to load reports analytics from backend API.', {
        method: 'GET',
        endpoint: REPORTS_ENDPOINT,
      })
    }

    throw new HttpError(0, 'Unable to load reports analytics from backend API.', {
      method: 'GET',
      endpoint: REPORTS_ENDPOINT,
    })
  } catch (error) {
    if (error instanceof HttpError && error.status === 403) {
      // Preserve explicit 403 semantics so staff/donor route mismatches are visible in UI.
      throw error
    }

    // NOTE: Fallback is explicitly gated so missing backend integrations fail loudly by default.
    if (ENABLE_DEV_FALLBACK) {
      return fallbackReportsData
    }

    if (error instanceof HttpError) {
      throw error
    }

    throw new HttpError(0, 'Unable to load reports analytics from backend API.', {
      method: 'GET',
      endpoint: REPORTS_ENDPOINT,
    })
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
    throw new HttpError(response.status, `Social post metrics endpoint returned ${response.status}`, {
      method: 'GET',
      endpoint: SOCIAL_METRICS_ENDPOINT,
    })
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
    throw new HttpError(response.status, `Create social post metric endpoint returned ${response.status}`, {
      method: 'POST',
      endpoint: SOCIAL_METRICS_ENDPOINT,
    })
  }

  return (await response.json()) as SocialPostMetricListItem
}
