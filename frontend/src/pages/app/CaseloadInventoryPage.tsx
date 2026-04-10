import { useEffect, useState } from 'react'
import { fetchResidentCases } from '../../services/adminOperationsApi'
import { toUserFacingError } from '../../services/httpErrors'
import type { ResidentCaseListItem } from '../../types/adminOperations'

export function CaseloadInventoryPage() {
  const [items, setItems] = useState<ResidentCaseListItem[]>([])
  const [statusStateId, setStatusStateId] = useState('')
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [pageSize] = useState(10)
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    async function load() {
      try {
        setLoading(true)
        const result = await fetchResidentCases({
          page,
          pageSize,
          search: search || undefined,
          statusStateId: statusStateId ? Number(statusStateId) : undefined,
        })
        if (!cancelled) {
          setItems(result.items)
          setTotalCount(result.totalCount)
          setError(null)
        }
      } catch (err) {
        if (!cancelled) setError(toUserFacingError(err, 'Failed to load caseload'))
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [page, pageSize, search, statusStateId])

  return (
    <section>
      <h1>Caseload Inventory</h1>
      <p className="lead">Resident profile CRUD with filtering by status, safehouse, category, and social worker.</p>
      <article className="feature-card">
        <h2>Case search</h2>
        <input placeholder="Search" value={search} onChange={(e) => { setSearch(e.target.value); setPage(1) }} />
        <input placeholder="Status ID" value={statusStateId} onChange={(e) => { setStatusStateId(e.target.value); setPage(1) }} />

        {loading && <p role="status">Loading cases…</p>}
        {error && <p role="alert">{error}</p>}
        {!loading && !error && (
          <>
            <table>
              <thead><tr><th>Safehouse</th><th>Category</th><th>Status</th><th>Opened</th></tr></thead>
              <tbody>{items.map((x) => <tr key={x.id}><td>{x.safehouse}</td><td>{x.category}</td><td>{x.status}</td><td>{new Date(x.openedAt).toLocaleDateString()}</td></tr>)}</tbody>
            </table>
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
