import { useEffect, useState } from 'react'
import { fetchAdminDashboardSummary, type AdminDashboardResult } from '../../services/adminDashboardApi'
import { toUserFacingError } from '../../services/httpErrors'
import { ApiErrorNotice } from '../../components/ApiErrorNotice'

export function AdminDashboardPage() {
  const [result, setResult] = useState<AdminDashboardResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false

    async function loadDashboard() {
      setLoading(true)
      setError(null)

      try {
        const next = await fetchAdminDashboardSummary()
        if (!cancelled) {
          setResult(next)
        }
      } catch (err) {
        if (!cancelled) {
          setError(toUserFacingError(err, 'Failed to load admin dashboard.'))
          setResult(null)
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void loadDashboard()

    return () => {
      cancelled = true
    }
  }, [])

  return (
    <section>
      <h1>Admin Dashboard</h1>
      <p className="lead">Snapshot of active residents, contributions, conferences, and outcomes.</p>

      {loading && <p role="status">Loading dashboard…</p>}
      {!loading && error && <ApiErrorNotice error={error} />}

      {!loading && !error && result?.kind === 'notImplemented' && (
        <article className="feature-card" role="status">
          <h2>Dashboard API not yet implemented</h2>
          {/* NOTE: We surface the versioned server envelope directly so release readiness is visible in UI testing. */}
          <p>
            {result.payload.message} ({result.payload.errorCode}, API {result.payload.apiVersion ?? 'v1'})
          </p>
          <p className="caption">Trace ID: {result.payload.traceId}</p>
        </article>
      )}

      {!loading && !error && result?.kind === 'ready' && (
        <div className="feature-grid">
          <article className="feature-card">
            <h2>Active residents</h2>
            <p>{result.summary.activeResidents}</p>
            <p className="caption">Currently active resident cases across safehouses.</p>
          </article>

          <article className="feature-card">
            <h2>Recent contributions</h2>
            <p>{result.summary.recentContributions.length}</p>
            <p className="caption">Most recent donor contribution records.</p>
          </article>

          <article className="feature-card">
            <h2>Upcoming conferences</h2>
            <p>{result.summary.upcomingConferences.length}</p>
            <p className="caption">Scheduled case conferences awaiting completion.</p>
          </article>

          <article className="feature-card">
            <h2>Summary outcomes</h2>
            <p>{result.summary.summaryOutcomes.length}</p>
            <p className="caption">Latest operational snapshots.</p>
          </article>
        </div>
      )}
    </section>
  )
}
