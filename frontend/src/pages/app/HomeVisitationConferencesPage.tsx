import { useEffect, useState } from 'react'
import {
  fetchVisitLogs,
  fetchUpcomingConferences,
  fetchPreviousConferences,
  createHomeVisit,
  createCaseConference,
} from '../../services/adminOperationsApi'
import { toUserFacingError } from '../../services/httpErrors'
import { ApiErrorNotice } from '../../components/ApiErrorNotice'
import type { PagedResult, HomeVisitItem, CaseConferenceItem } from '../../types/adminOperations'

const PAGE_SIZE = 10

const STATUS_OPTIONS = [
  { id: 1, name: 'Active' },
  { id: 2, name: 'Pending' },
  { id: 3, name: 'Closed' },
]

const VISIT_TYPE_OPTIONS = [
  { id: 1, name: 'Initial Assessment' },
  { id: 2, name: 'Routine Follow-up' },
  { id: 3, name: 'Reintegration Assessment' },
  { id: 4, name: 'Post-placement Monitoring' },
  { id: 5, name: 'Emergency' },
]

const FAMILY_COOPERATION_OPTIONS = ['Low', 'Moderate', 'High']

const EMPTY_VISIT_FORM = {
  residentCaseId: '',
  visitTypeId: 1,
  statusStateId: 1,
  visitDate: '',
  homeEnvironmentObservations: '',
  familyCooperationLevel: 'Moderate',
  safetyConcernsIdentified: false,
  followUpActions: '',
  notes: '',
}

const EMPTY_CONFERENCE_FORM = {
  residentCaseId: '',
  statusStateId: 1,
  conferenceDate: '',
  outcomeSummary: '',
}

function fmt(iso: string) {
  return new Date(iso).toLocaleString(undefined, {
    year: 'numeric', month: 'short', day: 'numeric',
    hour: '2-digit', minute: '2-digit',
  })
}

function Pagination({ page, total, pageSize, onPage }: {
  page: number; total: number; pageSize: number; onPage: (p: number) => void
}) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize))
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginTop: '0.75rem' }}>
      <button className="button" disabled={page <= 1} onClick={() => onPage(page - 1)}
        style={{ padding: '4px 12px', fontSize: '0.85rem' }}>← Prev</button>
      <span style={{ fontSize: '0.85rem', color: '#64748b' }}>Page {page} of {totalPages}</span>
      <button className="button" disabled={page >= totalPages} onClick={() => onPage(page + 1)}
        style={{ padding: '4px 12px', fontSize: '0.85rem' }}>Next →</button>
    </div>
  )
}

export function HomeVisitationConferencesPage() {
  // Data
  const [visits, setVisits] = useState<PagedResult<HomeVisitItem> | null>(null)
  const [upcoming, setUpcoming] = useState<PagedResult<CaseConferenceItem> | null>(null)
  const [previous, setPrevious] = useState<PagedResult<CaseConferenceItem> | null>(null)

  // Filters
  const [residentCaseFilter, setResidentCaseFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState(0)

  // Pagination
  const [visitPage, setVisitPage] = useState(1)
  const [upcomingPage, setUpcomingPage] = useState(1)
  const [previousPage, setPreviousPage] = useState(1)

  // Loading / error
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Form toggles
  const [showVisitForm, setShowVisitForm] = useState(false)
  const [showConferenceForm, setShowConferenceForm] = useState(false)

  // Form state
  const [visitForm, setVisitForm] = useState(EMPTY_VISIT_FORM)
  const [conferenceForm, setConferenceForm] = useState(EMPTY_CONFERENCE_FORM)

  // Submit state
  const [submitting, setSubmitting] = useState(false)
  const [submitError, setSubmitError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    async function load() {
      try {
        setLoading(true)
        setError(null)
        const q = {
          page: 1, pageSize: PAGE_SIZE,
          residentCaseId: residentCaseFilter || undefined,
          statusStateId: statusFilter || undefined,
        }
        const [vData, uData, pData] = await Promise.all([
          fetchVisitLogs({ ...q, page: visitPage }),
          fetchUpcomingConferences({ ...q, page: upcomingPage }),
          fetchPreviousConferences({ ...q, page: previousPage }),
        ])
        if (!cancelled) {
          setVisits(vData)
          setUpcoming(uData)
          setPrevious(pData)
        }
      } catch (err) {
        if (!cancelled) setError(toUserFacingError(err, 'Failed to load visitation and conference data'))
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    void load()
    return () => { cancelled = true }
  }, [residentCaseFilter, statusFilter, visitPage, upcomingPage, previousPage])

  // Reset pages when filters change
  function applyFilter(rcId: string, status: number) {
    setResidentCaseFilter(rcId)
    setStatusFilter(status)
    setVisitPage(1)
    setUpcomingPage(1)
    setPreviousPage(1)
  }

  async function handleLogVisit(e: React.FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setSubmitError(null)
    try {
      await createHomeVisit(visitForm)
      setVisitForm(EMPTY_VISIT_FORM)
      setShowVisitForm(false)
      setVisitPage(1)
      // Refresh visits
      const vData = await fetchVisitLogs({
        page: 1, pageSize: PAGE_SIZE,
        residentCaseId: residentCaseFilter || undefined,
        statusStateId: statusFilter || undefined,
      })
      setVisits(vData)
    } catch (err) {
      setSubmitError(toUserFacingError(err, 'Failed to log visit'))
    } finally {
      setSubmitting(false)
    }
  }

  async function handleLogConference(e: React.FormEvent) {
    e.preventDefault()
    setSubmitting(true)
    setSubmitError(null)
    try {
      await createCaseConference(conferenceForm)
      setConferenceForm(EMPTY_CONFERENCE_FORM)
      setShowConferenceForm(false)
      setUpcomingPage(1)
      // Refresh conferences
      const [uData, pData] = await Promise.all([
        fetchUpcomingConferences({
          page: 1, pageSize: PAGE_SIZE,
          residentCaseId: residentCaseFilter || undefined,
          statusStateId: statusFilter || undefined,
        }),
        fetchPreviousConferences({
          page: 1, pageSize: PAGE_SIZE,
          residentCaseId: residentCaseFilter || undefined,
          statusStateId: statusFilter || undefined,
        }),
      ])
      setUpcoming(uData)
      setPrevious(pData)
    } catch (err) {
      setSubmitError(toUserFacingError(err, 'Failed to log conference'))
    } finally {
      setSubmitting(false)
    }
  }

  const inputStyle: React.CSSProperties = {
    padding: '6px 10px', borderRadius: '6px',
    border: '1px solid #cbd5e1', fontSize: '0.9rem', width: '100%',
  }
  const labelStyle: React.CSSProperties = {
    display: 'flex', flexDirection: 'column', gap: '4px',
    fontSize: '0.85rem', fontWeight: 600, color: 'var(--color-text)',
  }
  const thStyle: React.CSSProperties = {
    textAlign: 'left', padding: '8px 12px',
    fontSize: '0.75rem', fontWeight: 700, textTransform: 'uppercase',
    letterSpacing: '0.06em', color: '#94a3b8',
    borderBottom: '1px solid #e2e8f0',
  }
  const tdStyle: React.CSSProperties = {
    padding: '8px 12px', fontSize: '0.875rem',
    borderBottom: '1px solid #f1f5f9', color: 'var(--color-text)',
  }

  return (
    <section>
      <p className="eyebrow">Operations</p>
      <h1>Home Visitation &amp; Case Conferences</h1>
      <p className="lead">Log home and field visits with assessment details, and review conference history and upcoming schedules for each resident case.</p>

      {/* Action buttons */}
      <div style={{ display: 'flex', gap: '0.75rem', marginBottom: '1.25rem', flexWrap: 'wrap' }}>
        <button
          className="button button-primary"
          onClick={() => { setShowVisitForm(v => !v); setShowConferenceForm(false); setSubmitError(null) }}
        >
          {showVisitForm ? 'Cancel' : '+ Log a Visit'}
        </button>
        <button
          className="button button-primary"
          onClick={() => { setShowConferenceForm(v => !v); setShowVisitForm(false); setSubmitError(null) }}
        >
          {showConferenceForm ? 'Cancel' : '+ Log a Conference'}
        </button>
      </div>

      {submitError && <ApiErrorNotice error={submitError} />}

      {/* Log a Visit form */}
      {showVisitForm && (
        <div className="chart-card" style={{ maxWidth: '600px', marginBottom: '1.5rem' }}>
          <p className="eyebrow">New Visit</p>
          <form onSubmit={handleLogVisit}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.85rem 1.25rem' }}>
              <label style={labelStyle}>
                Resident Case ID
                <input style={inputStyle} required value={visitForm.residentCaseId}
                  onChange={e => setVisitForm(f => ({ ...f, residentCaseId: e.target.value }))}
                  placeholder="e.g. 3fa85f64-..." />
              </label>

              <label style={labelStyle}>
                Visit Type
                <select style={inputStyle} value={visitForm.visitTypeId}
                  onChange={e => setVisitForm(f => ({ ...f, visitTypeId: Number(e.target.value) }))}>
                  {VISIT_TYPE_OPTIONS.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
                </select>
              </label>

              <label style={labelStyle}>
                Status
                <select style={inputStyle} value={visitForm.statusStateId}
                  onChange={e => setVisitForm(f => ({ ...f, statusStateId: Number(e.target.value) }))}>
                  {STATUS_OPTIONS.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </label>

              <label style={labelStyle}>
                Visit Date &amp; Time
                <input style={inputStyle} type="datetime-local" required value={visitForm.visitDate}
                  onChange={e => setVisitForm(f => ({ ...f, visitDate: e.target.value }))} />
              </label>

              <label style={{ ...labelStyle, gridColumn: '1 / -1' }}>
                Home Environment Observations
                <textarea style={{ ...inputStyle, minHeight: '72px', resize: 'vertical' }}
                  value={visitForm.homeEnvironmentObservations}
                  onChange={e => setVisitForm(f => ({ ...f, homeEnvironmentObservations: e.target.value }))} />
              </label>

              <label style={labelStyle}>
                Family Cooperation Level
                <select style={inputStyle} value={visitForm.familyCooperationLevel}
                  onChange={e => setVisitForm(f => ({ ...f, familyCooperationLevel: e.target.value }))}>
                  {FAMILY_COOPERATION_OPTIONS.map(option => <option key={option} value={option}>{option}</option>)}
                </select>
              </label>

              <label style={labelStyle}>
                Safety Concerns
                <select style={inputStyle} value={visitForm.safetyConcernsIdentified ? 'yes' : 'no'}
                  onChange={e => setVisitForm(f => ({ ...f, safetyConcernsIdentified: e.target.value === 'yes' }))}>
                  <option value="no">No</option>
                  <option value="yes">Yes</option>
                </select>
              </label>

              <label style={{ ...labelStyle, gridColumn: '1 / -1' }}>
                Follow-up Actions
                <textarea style={{ ...inputStyle, minHeight: '72px', resize: 'vertical' }}
                  value={visitForm.followUpActions}
                  onChange={e => setVisitForm(f => ({ ...f, followUpActions: e.target.value }))} />
              </label>

              <label style={{ ...labelStyle, gridColumn: '1 / -1' }}>
                Notes
                <textarea style={{ ...inputStyle, minHeight: '72px', resize: 'vertical' }}
                  value={visitForm.notes}
                  onChange={e => setVisitForm(f => ({ ...f, notes: e.target.value }))} />
              </label>
            </div>
            <button className="button button-primary" type="submit" disabled={submitting}
              style={{ marginTop: '1rem' }}>
              {submitting ? 'Saving...' : 'Save Visit'}
            </button>
          </form>
        </div>
      )}

      {/* Log a Conference form */}
      {showConferenceForm && (
        <div className="chart-card" style={{ maxWidth: '600px', marginBottom: '1.5rem' }}>
          <p className="eyebrow">New Conference</p>
          <form onSubmit={handleLogConference}>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.85rem 1.25rem' }}>
              <label style={labelStyle}>
                Resident Case ID
                <input style={inputStyle} required value={conferenceForm.residentCaseId}
                  onChange={e => setConferenceForm(f => ({ ...f, residentCaseId: e.target.value }))}
                  placeholder="e.g. 3fa85f64-..." />
              </label>

              <label style={labelStyle}>
                Status
                <select style={inputStyle} value={conferenceForm.statusStateId}
                  onChange={e => setConferenceForm(f => ({ ...f, statusStateId: Number(e.target.value) }))}>
                  {STATUS_OPTIONS.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
                </select>
              </label>

              <label style={labelStyle}>
                Conference Date &amp; Time
                <input style={inputStyle} type="datetime-local" required value={conferenceForm.conferenceDate}
                  onChange={e => setConferenceForm(f => ({ ...f, conferenceDate: e.target.value }))} />
              </label>

              <label style={{ ...labelStyle, gridColumn: '1 / -1' }}>
                Outcome Summary
                <textarea style={{ ...inputStyle, minHeight: '72px', resize: 'vertical' }}
                  value={conferenceForm.outcomeSummary}
                  onChange={e => setConferenceForm(f => ({ ...f, outcomeSummary: e.target.value }))} />
              </label>
            </div>
            <button className="button button-primary" type="submit" disabled={submitting}
              style={{ marginTop: '1rem' }}>
              {submitting ? 'Saving...' : 'Save Conference'}
            </button>
          </form>
        </div>
      )}

      {/* Filter bar */}
      <div className="chart-card" style={{ display: 'flex', gap: '1rem', alignItems: 'flex-end', flexWrap: 'wrap', marginBottom: '1.5rem', maxWidth: '700px' }}>
        <label style={{ ...labelStyle, flex: '2 1 200px' }}>
          Filter by Resident Case ID
          <input style={inputStyle} value={residentCaseFilter}
            onChange={e => applyFilter(e.target.value, statusFilter)}
            placeholder="Paste a case ID to filter…" />
        </label>
        <label style={{ ...labelStyle, flex: '1 1 140px' }}>
          Status
          <select style={inputStyle} value={statusFilter}
            onChange={e => applyFilter(residentCaseFilter, Number(e.target.value))}>
            <option value={0}>All statuses</option>
            {STATUS_OPTIONS.map(s => <option key={s.id} value={s.id}>{s.name}</option>)}
          </select>
        </label>
        {(residentCaseFilter || statusFilter > 0) && (
          <button className="button" onClick={() => applyFilter('', 0)}
            style={{ padding: '6px 14px', fontSize: '0.85rem' }}>Clear</button>
        )}
      </div>

      {loading && <p role="status" style={{ color: '#64748b' }}>Loading…</p>}
      {error && <ApiErrorNotice error={error} />}

      {!loading && !error && (
        <>
          {/* Visit Logs */}
          <div className="metric-card" style={{ marginBottom: '1.5rem' }}>
            <p className="eyebrow">Visit Logs</p>
            {visits && visits.items.length > 0 ? (
              <>
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead>
                    <tr>
                      <th style={thStyle}>Date</th>
                      <th style={thStyle}>Type</th>
                      <th style={thStyle}>Family Cooperation</th>
                      <th style={thStyle}>Safety Concerns</th>
                      <th style={thStyle}>Status</th>
                      <th style={thStyle}>Home Environment</th>
                      <th style={thStyle}>Follow-up Actions</th>
                      <th style={thStyle}>Notes</th>
                    </tr>
                  </thead>
                  <tbody>
                    {visits.items.map(v => (
                      <tr key={v.id}>
                        <td style={tdStyle}>{fmt(v.visitDate)}</td>
                        <td style={tdStyle}>{v.visitType}</td>
                        <td style={tdStyle}>{v.familyCooperationLevel || '-'}</td>
                        <td style={tdStyle}>{v.safetyConcernsIdentified ? 'Yes' : 'No'}</td>
                        <td style={tdStyle}>{v.status}</td>
                        <td style={{ ...tdStyle, color: '#64748b' }}>{v.homeEnvironmentObservations || '-'}</td>
                        <td style={{ ...tdStyle, color: '#64748b' }}>{v.followUpActions || '-'}</td>
                        <td style={{ ...tdStyle, color: '#64748b' }}>{v.notes || '-'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <Pagination page={visitPage} total={visits.totalCount} pageSize={PAGE_SIZE} onPage={setVisitPage} />
              </>
            ) : (
              <p style={{ color: '#94a3b8', fontSize: '0.9rem' }}>No visit records found.</p>
            )}
          </div>

          {/* Upcoming Conferences */}
          <div className="metric-card" style={{ marginBottom: '1.5rem' }}>
            <p className="eyebrow">Upcoming Conferences</p>
            {upcoming && upcoming.items.length > 0 ? (
              <>
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead>
                    <tr>
                      <th style={thStyle}>Date</th>
                      <th style={thStyle}>Status</th>
                      <th style={thStyle}>Outcome Summary</th>
                    </tr>
                  </thead>
                  <tbody>
                    {upcoming.items.map(c => (
                      <tr key={c.id}>
                        <td style={tdStyle}>{fmt(c.conferenceDate)}</td>
                        <td style={tdStyle}>{c.status}</td>
                        <td style={{ ...tdStyle, color: '#64748b' }}>{c.outcomeSummary || '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <Pagination page={upcomingPage} total={upcoming.totalCount} pageSize={PAGE_SIZE} onPage={setUpcomingPage} />
              </>
            ) : (
              <p style={{ color: '#94a3b8', fontSize: '0.9rem' }}>No upcoming conferences.</p>
            )}
          </div>

          {/* Previous Conferences */}
          <div className="metric-card">
            <p className="eyebrow">Previous Conferences</p>
            {previous && previous.items.length > 0 ? (
              <>
                <table style={{ width: '100%', borderCollapse: 'collapse' }}>
                  <thead>
                    <tr>
                      <th style={thStyle}>Date</th>
                      <th style={thStyle}>Status</th>
                      <th style={thStyle}>Outcome Summary</th>
                    </tr>
                  </thead>
                  <tbody>
                    {previous.items.map(c => (
                      <tr key={c.id}>
                        <td style={tdStyle}>{fmt(c.conferenceDate)}</td>
                        <td style={tdStyle}>{c.status}</td>
                        <td style={{ ...tdStyle, color: '#64748b' }}>{c.outcomeSummary || '—'}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
                <Pagination page={previousPage} total={previous.totalCount} pageSize={PAGE_SIZE} onPage={setPreviousPage} />
              </>
            ) : (
              <p style={{ color: '#94a3b8', fontSize: '0.9rem' }}>No previous conferences.</p>
            )}
          </div>
        </>
      )}
    </section>
  )
}
