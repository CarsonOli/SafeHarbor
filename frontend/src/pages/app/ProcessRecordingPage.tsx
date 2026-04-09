import { useEffect, useMemo, useState } from 'react'
import { createProcessRecording, fetchProcessRecordings } from '../../services/adminOperationsApi'
import type { ProcessRecordItem } from '../../types/adminOperations'

export function ProcessRecordingPage() {
  const [items, setItems] = useState<ProcessRecordItem[]>([])
  const [formResidentCaseId, setFormResidentCaseId] = useState('')
  const [filterResidentCaseId, setFilterResidentCaseId] = useState('')
  const [summary, setSummary] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize] = useState(10)
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  // Keep query building centralized so list refresh behavior stays deterministic across create/filter/paging flows.
  const query = useMemo(
    () => ({ page, pageSize, residentCaseId: filterResidentCaseId || undefined, desc: true }),
    [page, pageSize, filterResidentCaseId],
  )

  useEffect(() => {
    let cancelled = false

    async function load() {
      setLoading(true)
      try {
        const data = await fetchProcessRecordings(query)
        if (cancelled) return

        setItems(data.items)
        setTotalCount(data.totalCount)
        setError(null)
      } catch (err) {
        if (cancelled) return
        setError(err instanceof Error ? err.message : 'Failed to load recordings')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    void load()

    return () => {
      cancelled = true
    }
  }, [query])

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()

    const trimmedCaseId = formResidentCaseId.trim()
    const trimmedSummary = summary.trim()

    // Validate client-side before API call so users get immediate, clear feedback for common form mistakes.
    if (!trimmedCaseId || trimmedSummary.length < 3) {
      setError('Resident case ID and a summary of at least 3 characters are required.')
      setSuccess(null)
      return
    }

    setSubmitting(true)
    setError(null)
    setSuccess(null)

    try {
      await createProcessRecording(trimmedCaseId, trimmedSummary)
      setSummary('')
      setSuccess('Session note created.')

      // Refresh with the current filter and reset to first page so newest notes are immediately visible.
      setPage(1)
      const data = await fetchProcessRecordings({ page: 1, pageSize, residentCaseId: filterResidentCaseId || undefined, desc: true })
      setItems(data.items)
      setTotalCount(data.totalCount)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save recording')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <section>
      <h1>Counseling Logs</h1>
      <p className="lead">Form for Process Recordings (session notes) with chronological case timelines.</p>

      <form className="feature-card" onSubmit={handleCreate}>
        <h2>New session note</h2>
        <label htmlFor="process-recording-case-id">Resident case ID</label>
        <input
          id="process-recording-case-id"
          placeholder="Resident case ID"
          value={formResidentCaseId}
          onChange={(e) => setFormResidentCaseId(e.target.value)}
          required
        />

        <label htmlFor="process-recording-summary">Session summary</label>
        <textarea
          id="process-recording-summary"
          placeholder="Summary"
          value={summary}
          onChange={(e) => setSummary(e.target.value)}
          required
          minLength={3}
        />
        <button className="button" type="submit" disabled={submitting}>
          {submitting ? 'Saving…' : 'Create note'}
        </button>
      </form>

      <article className="feature-card" style={{ marginTop: '1rem' }}>
        <h2>Session timeline</h2>
        <label htmlFor="recording-filter">Filter by resident case ID</label>
        <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', flexWrap: 'wrap' }}>
          <input
            id="recording-filter"
            placeholder="Resident case ID filter"
            value={filterResidentCaseId}
            onChange={(e) => {
              setFilterResidentCaseId(e.target.value)
              setPage(1)
            }}
          />
          <button
            type="button"
            onClick={() => {
              setFilterResidentCaseId('')
              setPage(1)
            }}
            disabled={!filterResidentCaseId}
          >
            Clear filter
          </button>
        </div>

        {loading && <p role="status">Loading recordings…</p>}
        {error && <p role="alert">{error}</p>}
        {success && <p role="status">{success}</p>}

        {!loading && !error && (
          <>
            {items.length === 0 ? (
              <p>No session notes found for the current filter.</p>
            ) : (
              <ul>
                {items.map((item) => (
                  <li key={item.id}>
                    <strong>{new Date(item.recordedAt).toLocaleString()}</strong> — {item.summary}
                  </li>
                ))}
              </ul>
            )}

            <div>
              <button disabled={page <= 1} onClick={() => setPage((v) => v - 1)}>
                Previous
              </button>
              <span>
                {' '}
                Page {page} of {totalPages}{' '}
              </span>
              <button disabled={page >= totalPages} onClick={() => setPage((v) => v + 1)}>
                Next
              </button>
            </div>
          </>
        )}
      </article>
    </section>
  )
}
