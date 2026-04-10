import { useEffect, useMemo, useState } from 'react'
import { fetchReportsAnalytics } from '../../services/impactApi'
import { toUserFacingError } from '../../services/httpErrors'
import { ApiErrorNotice } from '../../components/ApiErrorNotice'
import type { ReportsAnalyticsResponse } from '../../types/impact'

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(value)
}

function formatMonth(monthKey: string): string {
  const parsed = new Date(`${monthKey}-01T00:00:00.000Z`)
  return Number.isNaN(parsed.getTime())
    ? monthKey
    : parsed.toLocaleDateString('en-US', { month: 'short', year: 'numeric', timeZone: 'UTC' })
}

function MetricCard({ label, value, detail }: { label: string; value: string; detail: string }) {
  return (
    <article className="metric-card">
      <p className="eyebrow">{label}</p>
      <p className="metric-value">{value}</p>
      <p>{detail}</p>
    </article>
  )
}

export function ReportsAnalyticsPage() {
  const [report, setReport] = useState<ReportsAnalyticsResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function loadReport() {
      setIsLoading(true)
      try {
        const data = await fetchReportsAnalytics()

        if (!cancelled) {
          setReport(data)
          setError(null)
        }
      } catch (err) {
        if (!cancelled) {
          setError(toUserFacingError(err, 'Failed to load reports analytics'))
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false)
        }
      }
    }

    void loadReport()

    return () => {
      cancelled = true
    }
  }, [])

  const viewModel = useMemo(() => {
    if (!report) {
      return null
    }

    const totalDonations = report.donationTrends.reduce((sum, item) => sum + item.amount, 0)
    const latestDonation = report.donationTrends[report.donationTrends.length - 1] ?? null
    const latestOutcome = report.outcomeTrends[report.outcomeTrends.length - 1] ?? null
    const latestReintegration = report.reintegrationRates[report.reintegrationRates.length - 1] ?? null
    const totalActiveResidents = report.safehouseComparisons.reduce((sum, item) => sum + item.activeResidents, 0)
    const totalAllocatedFunding = report.safehouseComparisons.reduce((sum, item) => sum + item.allocatedFunding, 0)
    const topPlatform = report.donationCorrelationByPlatform[0] ?? null

    return {
      totalDonations,
      latestDonation,
      latestOutcome,
      latestReintegration,
      totalActiveResidents,
      safehouseCount: report.safehouseComparisons.length,
      totalAllocatedFunding,
      topPlatform,
    }
  }, [report])

  if (isLoading) {
    return (
      <section>
        <h1>Reports & Analytics</h1>
        <p className="lead">Loading reports and analytics...</p>
      </section>
    )
  }

  if (error) {
    return (
      <section>
        <h1>Reports & Analytics</h1>
        <ApiErrorNotice error={error} />
      </section>
    )
  }

  if (!report || !viewModel) {
    return (
      <section>
        <h1>Reports & Analytics</h1>
        <p className="lead">No report data is available.</p>
      </section>
    )
  }

  return (
    <section>
      <h1>Reports & Analytics</h1>
      <p className="lead">Executive reporting for donations, resident outcomes, safehouse performance, and reintegration.</p>

      <div className="metric-grid">
        <MetricCard
          label="Donations (period total)"
          value={formatCurrency(viewModel.totalDonations)}
          detail="Total donation value across available reporting months."
        />
        <MetricCard
          label="Latest month donations"
          value={viewModel.latestDonation ? formatCurrency(viewModel.latestDonation.amount) : formatCurrency(0)}
          detail={viewModel.latestDonation ? formatMonth(viewModel.latestDonation.month) : 'No donation trend data yet.'}
        />
        <MetricCard
          label="Active residents"
          value={viewModel.totalActiveResidents.toLocaleString()}
          detail="Current active resident count across tracked safehouses."
        />
        <MetricCard
          label="Home visits (latest month)"
          value={(viewModel.latestOutcome?.homeVisits ?? 0).toLocaleString()}
          detail={viewModel.latestOutcome ? formatMonth(viewModel.latestOutcome.month) : 'No home visit trend data yet.'}
        />
        <MetricCard
          label="Reintegration success"
          value={`${(viewModel.latestReintegration?.ratePercent ?? 0).toFixed(1)}%`}
          detail={viewModel.latestReintegration ? formatMonth(viewModel.latestReintegration.month) : 'No reintegration data yet.'}
        />
        <MetricCard
          label="Safehouses tracked"
          value={viewModel.safehouseCount.toLocaleString()}
          detail={`Allocated funding across tracked safehouses: ${formatCurrency(viewModel.totalAllocatedFunding)}.`}
        />
      </div>

      <h2 style={{ marginTop: '2rem' }}>Donations & Resource Flow</h2>
      <div className="feature-grid">
        <article className="feature-card">
          <h3>Monthly Donation Trend</h3>
          {report.donationTrends.length === 0 ? (
            <p className="caption">No donation trend data is available yet.</p>
          ) : (
            <ul className="stack-list">
              {report.donationTrends.map((point) => (
                <li key={point.month}>
                  <div className="stack-label-row">
                    <strong>{formatMonth(point.month)}</strong>
                    <span>{formatCurrency(point.amount)}</span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </article>
      </div>

      <h2 style={{ marginTop: '2rem' }}>Resident Outcomes</h2>
      <div className="feature-grid">
        <article className="feature-card">
          <h3>Monthly Service Outcomes</h3>
          {report.outcomeTrends.length === 0 ? (
            <p className="caption">No resident outcomes trend is available yet.</p>
          ) : (
            <ul className="stack-list">
              {report.outcomeTrends.map((point) => (
                <li key={point.month}>
                  <div className="stack-label-row">
                    <strong>{formatMonth(point.month)}</strong>
                    <span>{point.residentsServed.toLocaleString()} residents</span>
                  </div>
                  <p className="caption">{point.homeVisits.toLocaleString()} home visits</p>
                </li>
              ))}
            </ul>
          )}
        </article>
        <article className="feature-card">
          <h3>Reintegration Success Trend</h3>
          {report.reintegrationRates.length === 0 ? (
            <p className="caption">No reintegration trend is available yet.</p>
          ) : (
            <ul className="stack-list">
              {report.reintegrationRates.map((point) => (
                <li key={point.month}>
                  <div className="stack-label-row">
                    <strong>{formatMonth(point.month)}</strong>
                    <span>{point.ratePercent.toFixed(1)}%</span>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </article>
      </div>

      <h2 style={{ marginTop: '2rem' }}>Safehouse Performance</h2>
      <div className="feature-grid">
        <article className="feature-card">
          <h3>Safehouse Comparison</h3>
          {report.safehouseComparisons.length === 0 ? (
            <p className="caption">No safehouse comparison data is available yet.</p>
          ) : (
            <ul className="stack-list">
              {report.safehouseComparisons.map((item) => (
                <li key={item.safehouse}>
                  <div className="stack-label-row">
                    <strong>{item.safehouse}</strong>
                    <span>{item.activeResidents.toLocaleString()} active residents</span>
                  </div>
                  <p className="caption">Allocated funding: {formatCurrency(item.allocatedFunding)}</p>
                </li>
              ))}
            </ul>
          )}
        </article>
      </div>

      <h2 style={{ marginTop: '2rem' }}>Top Social Signal</h2>
      <div className="feature-grid">
        <article className="feature-card">
          <h3>Highest Donation Platform</h3>
          {!viewModel.topPlatform ? (
            <p className="caption">No social attribution data is available yet.</p>
          ) : (
            <>
              <p>
                <strong>{viewModel.topPlatform.group}</strong> has the strongest donation attribution in the current dataset.
              </p>
              <p className="caption">
                {formatCurrency(viewModel.topPlatform.totalAttributedDonationAmount)} attributed from {viewModel.topPlatform.posts} posts, with{' '}
                {viewModel.topPlatform.totalReach.toLocaleString()} reach.
              </p>
            </>
          )}
        </article>
      </div>

      <h2 style={{ marginTop: '2rem' }}>Recommendations</h2>
      <div className="feature-grid">
        {report.recommendations.length === 0 ? (
          <article className="feature-card">
            <p className="caption">No recommendations are available yet.</p>
          </article>
        ) : (
          report.recommendations.map((recommendation) => (
            <article key={recommendation.title} className="feature-card">
              <h3>{recommendation.title}</h3>
              <p>{recommendation.rationale}</p>
              <p>
                <strong>Suggested action:</strong> {recommendation.action}
              </p>
            </article>
          ))
        )}
      </div>
    </section>
  )
}
