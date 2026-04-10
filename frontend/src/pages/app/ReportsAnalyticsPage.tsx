import { useEffect, useMemo, useState } from 'react'
import { fetchReportsAnalytics } from '../../services/impactApi'
import { toUserFacingError } from '../../services/httpErrors'
import { ApiErrorNotice } from '../../components/ApiErrorNotice'
import type { ReportsAnalyticsResponse, SocialDonationCorrelationPoint } from '../../types/impact'

function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 }).format(value)
}

function CorrelationList({
  title,
  data,
}: {
  title: string
  data: SocialDonationCorrelationPoint[]
}) {
  return (
    <article className="feature-card">
      <h2>{title}</h2>
      {data.length === 0 ? (
        <p className="caption">No post-level metrics logged yet.</p>
      ) : (
        <ul className="stack-list">
          {data.slice(0, 5).map((item) => (
            <li key={`${title}-${item.group}`}>
              <div className="stack-label-row">
                <strong>{item.group}</strong>
                <span>{formatCurrency(item.totalAttributedDonationAmount)}</span>
              </div>
              <div
                className="stack"
                style={{ ['--stack-width' as string]: `${Math.min(100, item.engagementRatePercent * 6)}%` }}
                aria-hidden="true"
              />
              <p className="caption">
                {item.posts} posts · {item.totalReach.toLocaleString()} reach · {item.engagementRatePercent.toFixed(1)}% engagement
              </p>
            </li>
          ))}
        </ul>
      )}
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

  const totalAttributedDonations = useMemo(() => {
    if (!report) {
      return 0
    }

    return report.topAttributedPosts.reduce((total, item) => total + (item.attributedDonationAmount ?? 0), 0)
  }, [report])

  if (isLoading) {
    return (
      <section>
        <h1>Reports & Analytics</h1>
        <p className="lead">Loading donation correlation report...</p>
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

  if (!report) {
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
      <p className="lead">Analyze what platform, post time, and content type most closely correlate with donation outcomes.</p>

      <div className="metric-grid">
        <article className="metric-card">
          <p className="eyebrow">Top posts</p>
          <p className="metric-value">{report.topAttributedPosts.length}</p>
          <p>Posts with the strongest known donation attribution in this dataset.</p>
        </article>
        <article className="metric-card">
          <p className="eyebrow">Attributed donations</p>
          <p className="metric-value">{formatCurrency(totalAttributedDonations)}</p>
          <p>Known donation value from currently highlighted post-level metrics.</p>
        </article>
      </div>

      <div className="feature-grid">
        <CorrelationList title="Correlation by platform" data={report.donationCorrelationByPlatform} />
        <CorrelationList title="Correlation by content type" data={report.donationCorrelationByContentType} />
        <CorrelationList title="Correlation by posting time" data={report.donationCorrelationByPostingHour} />
      </div>

      <h2 style={{ marginTop: '2rem' }}>Recommendation cards (rule-based)</h2>
      <div className="feature-grid">
        {report.recommendations.map((recommendation) => (
          <article key={recommendation.title} className="feature-card">
            <h3>{recommendation.title}</h3>
            <p>{recommendation.rationale}</p>
            <p>
              <strong>Suggested action:</strong> {recommendation.action}
            </p>
          </article>
        ))}
      </div>
    </section>
  )
}
