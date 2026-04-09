import { useEffect, useState } from 'react'
import { createProcessRecording, fetchProcessRecordings } from '../../services/adminOperationsApi'
import type { ProcessRecordItem } from '../../types/adminOperations'

export function ProcessRecordingPage() {
  const [items, setItems] = useState<ProcessRecordItem[]>([])
  const [residentCaseId, setResidentCaseId] = useState('')
  const [summary, setSummary] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize] = useState(10)
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)

  async function load() {
    setLoading(true)
    try {
      const data = await fetchProcessRecordings({ page, pageSize, residentCaseId: residentCaseId || undefined, desc: true })
      setItems(data.items)
      setTotalCount(data.totalCount)
      setError(null)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load recordings')
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { void load() }, [page, pageSize, residentCaseId])

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    setSuccess(null)
    try {
      await createProcessRecording(residentCaseId, summary)
      setSummary('')
      setSuccess('Process note created.')
      await load()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save recording')
    }
  }

  return (
    <section>
      <h1>Process Recording</h1>
      <p className="lead">Chronological session notes per resident case, with role-based write controls.</p>
      <form className="feature-card" onSubmit={handleCreate}>
        <h2>New note</h2>
        <input placeholder="Resident case ID" value={residentCaseId} onChange={(e) => setResidentCaseId(e.target.value)} required />
        <textarea placeholder="Summary" value={summary} onChange={(e) => setSummary(e.target.value)} required minLength={3} />
        <button className="button" type="submit">Create note</button>
      </form>

      <article className="feature-card" style={{ marginTop: '1rem' }}>
        <h2>Session timeline</h2>
        {loading && <p role="status">Loading recordings…</p>}
        {error && <p role="alert">{error}</p>}
        {success && <p role="status">{success}</p>}
        {!loading && !error && (
          <>
            <ul>{items.map((item) => <li key={item.id}><strong>{new Date(item.recordedAt).toLocaleString()}</strong> — {item.summary}</li>)}</ul>
            <div>
              <button disabled={page <= 1} onClick={() => setPage((v) => v - 1)}>Previous</button>
              <span> Page {page} of {Math.max(1, Math.ceil(totalCount / pageSize))} </span>
              <button disabled={page * pageSize >= totalCount} onClick={() => setPage((v) => v + 1)}>Next</button>
            </div>
          </>
        )}
      </article>
    </section>
  )
}
