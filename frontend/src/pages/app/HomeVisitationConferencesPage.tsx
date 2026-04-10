import { useEffect, useState } from 'react'
import { fetchPreviousConferences, fetchUpcomingConferences, fetchVisitLogs } from '../../services/adminOperationsApi'
import { toUserFacingError } from '../../services/httpErrors'
import { ApiErrorNotice } from '../../components/ApiErrorNotice'
import type { CaseConferenceItem, HomeVisitItem } from '../../types/adminOperations'

export function HomeVisitationConferencesPage() {
  const [visits, setVisits] = useState<HomeVisitItem[]>([])
  const [upcoming, setUpcoming] = useState<CaseConferenceItem[]>([])
  const [previous, setPrevious] = useState<CaseConferenceItem[]>([])
  const [residentCaseId, setResidentCaseId] = useState('')
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    async function load() {
      try {
        setLoading(true)
        const query = { page: 1, pageSize: 10, residentCaseId: residentCaseId || undefined }
        const [visitData, upcomingData, previousData] = await Promise.all([
          fetchVisitLogs(query),
          fetchUpcomingConferences(query),
          fetchPreviousConferences(query),
        ])

        if (!cancelled) {
          setVisits(visitData.items)
          setUpcoming(upcomingData.items)
          setPrevious(previousData.items)
          setError(null)
        }
      } catch (err) {
        if (!cancelled) setError(toUserFacingError(err, 'Failed to load visitation/conference data'))
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    void load()
    return () => { cancelled = true }
  }, [residentCaseId])

  return (
    <section>
      <h1>Home Visitation & Case Conferences</h1>
      <p className="lead">Maintain visit logs and review upcoming and previous case conferences.</p>
      <input placeholder="Resident case ID filter" value={residentCaseId} onChange={(e) => setResidentCaseId(e.target.value)} />
      {loading && <p role="status">Loading visitation and conference data…</p>}
      {error && <ApiErrorNotice error={error} />}
      {!loading && !error && (
        <div className="feature-grid">
          <article className="feature-card"><h2>Visit logs</h2><ul>{visits.map(v => <li key={v.id}>{new Date(v.visitDate).toLocaleDateString()} · {v.visitType} · {v.status}</li>)}</ul></article>
          <article className="feature-card"><h2>Upcoming conferences</h2><ul>{upcoming.map(c => <li key={c.id}>{new Date(c.conferenceDate).toLocaleDateString()} · {c.status}</li>)}</ul></article>
          <article className="feature-card"><h2>Previous conferences</h2><ul>{previous.map(c => <li key={c.id}>{new Date(c.conferenceDate).toLocaleDateString()} · {c.status}</li>)}</ul></article>
        </div>
      )}
    </section>
  )
}
