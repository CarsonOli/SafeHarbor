import { useEffect, useMemo, useState, type CSSProperties } from 'react'
import { fetchDonorAnalytics } from '../../services/donorAnalyticsApi'
import { toUserFacingError } from '../../services/httpErrors'
import type { DonorAnalyticsData } from '../../types/impact'

/** Formats a number as a compact USD string, e.g. "$3,400" or "$1.2k". */
function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(value)
}

/** Formats a number as a USD string with cents, e.g. "$200.00". */
function formatCurrencyFull(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)
}

/** Abbreviates an ISO year-month string to a short label, e.g. "2025-11" → "Nov". */
function shortMonth(isoMonth: string): string {
  const [year, month] = isoMonth.split('-')
  const date = new Date(Number(year), Number(month) - 1, 1)
  return date.toLocaleString('en-US', { month: 'short' })
}

// ── SVG Line Chart ────────────────────────────────────────────────────────────
// Renders natively — no charting library required.
// All coordinates computed from the data so the chart is fully responsive to value ranges.

/** Padding around the chart plot area within the SVG viewBox. */
const CHART_PADDING = { top: 24, right: 24, bottom: 44, left: 68 }
const VIEW_WIDTH = 600
const VIEW_HEIGHT = 320

/** Width and height of the drawable plot area. */
const PLOT_W = VIEW_WIDTH  - CHART_PADDING.left - CHART_PADDING.right
const PLOT_H = VIEW_HEIGHT - CHART_PADDING.top  - CHART_PADDING.bottom

// NOTE: Keep chart colors aligned with global design tokens from index.css so
// the analytics view stays visually consistent with public and donor pages.
const CHART_COLORS = {
  grid: 'var(--color-border)',
  axisLabel: 'var(--color-subtle)',
  areaFill: 'var(--color-secondary)',
  line: 'var(--color-primary)',
  pointHalo: 'var(--color-surface)',
  pointMuted: 'var(--color-border)',
}

interface LineChartProps {
  /** 12-month trend data from the API. */
  data: DonorAnalyticsData['monthlyTrend']
}

/**
 * SVG line chart showing monthly donation totals for the last 12 months.
 *
 * COORDINATE SYSTEM:
 *   - Origin (0,0) is top-left of the SVG viewBox.
 *   - The plot area is offset by CHART_PADDING from the viewBox edge.
 *   - X scale: evenly distributes 12 months across PLOT_W.
 *   - Y scale: 0 at the bottom of the plot (PLOT_H offset + top padding), max at top.
 *     Values are inverted: higher dollar amounts map to lower Y coordinates.
 */
function DonationLineChart({ data }: LineChartProps) {
  // Add 10% headroom above the max value so the line never touches the top edge.
  const maxAmount = useMemo(
    () => Math.max(...data.map(p => p.amount), 1) * 1.1,
    [data],
  )

  /**
   * Maps a 0-based month index (0–11) to its SVG X coordinate within the plot area.
   * Distributes 12 points evenly across the plot width.
   */
  const xScale = (i: number) =>
    CHART_PADDING.left + (i / (data.length - 1)) * PLOT_W

  /**
   * Maps a dollar amount to its SVG Y coordinate.
   * Inverted: amount=0 → bottom of plot, amount=max → top of plot.
   */
  const yScale = (amount: number) =>
    CHART_PADDING.top + PLOT_H - (amount / maxAmount) * PLOT_H

  // Build the polyline points string: "x1,y1 x2,y2 ..."
  const polylinePoints = data
    .map((point, i) => `${xScale(i)},${yScale(point.amount)}`)
    .join(' ')

  // Build the filled area path: go along the line, then close back along the bottom.
  const areaPath =
    `M ${xScale(0)},${yScale(data[0].amount)} ` +
    data.slice(1).map((p, i) => `L ${xScale(i + 1)},${yScale(p.amount)}`).join(' ') +
    ` L ${xScale(data.length - 1)},${CHART_PADDING.top + PLOT_H}` +
    ` L ${xScale(0)},${CHART_PADDING.top + PLOT_H} Z`

  // Grid lines at 4 evenly-spaced Y positions (25%, 50%, 75%, 100% of max).
  const gridValues = [0.25, 0.5, 0.75, 1.0].map(pct => maxAmount * pct)

  return (
    <svg
      className="donor-line-chart"
      viewBox={`0 0 ${VIEW_WIDTH} ${VIEW_HEIGHT}`}
      aria-label="Donation growth line chart"
      role="img"
    >
      {/* ── Y-axis grid lines + labels ──────────────────────────────────── */}
      {gridValues.map((val, i) => {
        const y = yScale(val)
        return (
          <g key={i}>
            {/* Dashed horizontal grid line across the plot area */}
            <line
              x1={CHART_PADDING.left}
              y1={y}
              x2={CHART_PADDING.left + PLOT_W}
              y2={y}
              stroke={CHART_COLORS.grid}
              strokeWidth="1"
              strokeDasharray="4 4"
            />
            {/* Dollar amount label on the left Y-axis */}
            <text
              x={CHART_PADDING.left - 8}
              y={y + 4}
              textAnchor="end"
              fontSize="11"
              fill={CHART_COLORS.axisLabel}
            >
              {formatCurrency(val)}
            </text>
          </g>
        )
      })}

      {/* ── Filled area under the line (light blue tint) ──────────────── */}
      <path d={areaPath} fill={CHART_COLORS.areaFill} opacity="0.5" />

      {/* ── The line itself ────────────────────────────────────────────── */}
      <polyline
        points={polylinePoints}
        fill="none"
        stroke={CHART_COLORS.line}
        strokeWidth="2.5"
        strokeLinejoin="round"
        strokeLinecap="round"
      />

      {/* ── Data point circles + tooltips ─────────────────────────────── */}
      {data.map((point, i) => (
        <g key={point.month}>
          {/* Outer white halo to make the dot pop off the line */}
          <circle cx={xScale(i)} cy={yScale(point.amount)} r="6" fill={CHART_COLORS.pointHalo} />
          {/* Colored dot */}
          <circle
            cx={xScale(i)}
            cy={yScale(point.amount)}
            r="4"
            fill={point.amount > 0 ? CHART_COLORS.line : CHART_COLORS.pointMuted}
          >
            {/* SVG title acts as a native tooltip on hover */}
            <title>{shortMonth(point.month)}: {formatCurrencyFull(point.amount)}</title>
          </circle>
        </g>
      ))}

      {/* ── X-axis month labels ────────────────────────────────────────── */}
      {data.map((point, i) => (
        <text
          key={point.month}
          x={xScale(i)}
          y={CHART_PADDING.top + PLOT_H + 20}
          textAnchor="middle"
          fontSize="11"
          fill={CHART_COLORS.axisLabel}
        >
          {shortMonth(point.month)}
        </text>
      ))}

      {/* ── Axis baseline ─────────────────────────────────────────────── */}
      <line
        x1={CHART_PADDING.left}
        y1={CHART_PADDING.top + PLOT_H}
        x2={CHART_PADDING.left + PLOT_W}
        y2={CHART_PADDING.top + PLOT_H}
        stroke={CHART_COLORS.grid}
        strokeWidth="1"
      />
    </svg>
  )
}

// ── Main Page Component ───────────────────────────────────────────────────────

/**
 * Admin Donor Analytics Page — /app/donor-analytics
 *
 * Two-column layout:
 *   Left sidebar: KPI metric cards, campaign OKRs, top donors leaderboard
 *   Right:        SVG line chart of monthly donation growth (last 12 months)
 *
 * All data comes from GET /api/admin/donor-analytics with fallback to static
 * seed-matched data so the page renders even without a running backend.
 */
export function AdminDonorAnalyticsPage() {
  const [data, setData] = useState<DonorAnalyticsData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      setError(null)
      try {
        const result = await fetchDonorAnalytics()
        setData(result)
      } catch (error) {
        setError(toUserFacingError(error, 'Unable to load donor analytics right now.'))
      } finally {
        setLoading(false)
      }
    }
    void load()
  }, [])

  // The max lifetime donated among top donors — used to scale the leaderboard bars.
  const maxLifetime = useMemo(
    () => Math.max(...(data?.topDonors.map(d => d.lifetimeDonated) ?? [1]), 1),
    [data],
  )

  return (
    <section aria-labelledby="analytics-title">
      <h1 id="analytics-title">Donor Analytics</h1>
      <p className="lead">
        Key fundraising metrics, campaign OKRs, and donation growth trends for admin review.
      </p>

      {loading && <p role="status">Loading analytics…</p>}
      {!loading && error && <p role="alert">{error}</p>}

      {!loading && data && (
        <div className="donor-analytics-layout">

          {/* ── LEFT SIDEBAR ───────────────────────────────────────────── */}
          <div className="donor-analytics-sidebar">

            {/* KPI metric cards — 2×2 grid */}
            <section className="metric-grid" aria-label="Key performance indicators">
              <article className="metric-card">
                <p className="eyebrow">Total Donations</p>
                <p className="metric-value">{formatCurrency(data.totalDonationsReceived)}</p>
                <p className="caption">{data.totalContributionCount} contributions</p>
              </article>

              <article className="metric-card">
                <p className="eyebrow">Donors</p>
                {/* Show active donors vs total so admins can spot disengagement */}
                <p className="metric-value">{data.activeDonorCount}</p>
                <p className="caption">active of {data.totalDonorCount} total</p>
              </article>

              <article className="metric-card">
                <p className="eyebrow">Retention Rate</p>
                {/* Retention = % of donors who gave more than once */}
                <p className="metric-value">{data.retentionRate}%</p>
                <p className="caption">repeat donors</p>
              </article>

              <article className="metric-card">
                <p className="eyebrow">Avg Gift Size</p>
                <p className="metric-value">{formatCurrencyFull(data.averageGiftSize)}</p>
                <p className="caption">per contribution</p>
              </article>
            </section>

            {/* Campaign OKRs — progress bar per campaign */}
            <article className="chart-card" aria-label="Campaign OKRs">
              <h2>Campaign OKRs</h2>
              {data.campaigns.length === 0 ? (
                <p className="caption">No campaigns found.</p>
              ) : (
                <ul className="stack-list">
                  {data.campaigns.map(campaign => (
                    <li key={campaign.campaignId}>
                      <div className="stack-label-row">
                        <span>{campaign.campaignName}</span>
                        {/* Progress percentage on the right */}
                        <span>{campaign.progressPercent}%</span>
                      </div>

                      {/* Progress bar: --stack-width drives the CSS gradient width */}
                      <div
                        className="stack"
                        style={{ '--stack-width': `${campaign.progressPercent}%` } as CSSProperties}
                        role="progressbar"
                        aria-valuenow={campaign.progressPercent}
                        aria-valuemin={0}
                        aria-valuemax={100}
                        aria-label={`${campaign.campaignName}: ${campaign.progressPercent}% of goal`}
                      />

                      {/* Sub-line: raised vs goal, with donor/contribution counts */}
                      <p className="caption">
                        {formatCurrency(campaign.totalRaised)} raised of{' '}
                        {formatCurrency(campaign.goalAmount)} goal
                        {' · '}{campaign.donorCount} donor{campaign.donorCount !== 1 ? 's' : ''}
                        {' · '}{campaign.contributionCount} gift{campaign.contributionCount !== 1 ? 's' : ''}
                      </p>
                    </li>
                  ))}
                </ul>
              )}
            </article>

            {/* Top Donors leaderboard */}
            <article className="chart-card" aria-label="Top donors leaderboard">
              <h2>Top Donors</h2>
              {data.topDonors.length === 0 ? (
                <p className="caption">No donor data yet.</p>
              ) : (
                <ul className="stack-list">
                  {data.topDonors.map(donor => {
                    // Scale bar width relative to the highest-earning donor (= 100%).
                    const barWidth = (donor.lifetimeDonated / maxLifetime) * 100
                    return (
                      <li key={donor.displayName}>
                        <div className="stack-label-row">
                          <span>{donor.displayName}</span>
                          <span>{formatCurrency(donor.lifetimeDonated)}</span>
                        </div>
                        <div
                          className="stack"
                          style={{ '--stack-width': `${barWidth}%` } as CSSProperties}
                          aria-hidden="true"
                        />
                        <p className="caption">
                          {donor.contributionCount} contribution{donor.contributionCount !== 1 ? 's' : ''}
                        </p>
                      </li>
                    )
                  })}
                </ul>
              )}
            </article>
          </div>

          {/* ── RIGHT COLUMN — SVG Line Chart ──────────────────────────── */}
          <article className="chart-card donor-line-chart-card" aria-label="Donation growth chart">
            <h2>Donation Growth</h2>
            <p className="caption">Monthly totals — last 12 months</p>
            <DonationLineChart data={data.monthlyTrend} />
          </article>

        </div>
      )}
    </section>
  )
}
