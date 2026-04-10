import { useEffect, useState } from 'react'
import { createDonor, fetchDonors } from '../../services/adminOperationsApi'
import { fetchDonorRiskFlags } from '../../services/mlInsightsApi'
import { toUserFacingError } from '../../services/httpErrors'
import { ApiErrorNotice } from '../../components/ApiErrorNotice'
import { RiskBadge } from '../../components/RiskBadge'
import type { DonorListItem } from '../../types/adminOperations'
import type { DonorRiskFlag } from '../../services/mlInsightsApi'

export function DonorsContributionsPage() {
  const [items, setItems] = useState<DonorListItem[]>([])
  const [page, setPage] = useState(1)
  const [pageSize] = useState(10)
  const [search, setSearch] = useState('')
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [name, setName] = useState('')
  const [email, setEmail] = useState('')
  const [success, setSuccess] = useState<string | null>(null)

  // Secondary risk flag fetch — silent fallback on error
  const [riskFlags, setRiskFlags] = useState<Map<string, DonorRiskFlag>>(new Map())

  useEffect(() => {
    let cancelled = false
    async function load() {
      try {
        setLoading(true)
        setError(null)
        const data = await fetchDonors({ page, pageSize, search: search || undefined, desc: true })
        if (!cancelled) {
          setItems(data.items)
          setTotalCount(data.totalCount)
        }
      } catch (err) {
        if (!cancelled) setError(toUserFacingError(err, 'Failed to load donors'))
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [page, pageSize, search])

  // Load risk flags once on mount — independent of pagination
  useEffect(() => {
    let cancelled = false
    async function loadFlags() {
      try {
        const flags = await fetchDonorRiskFlags()
        if (!cancelled) {
          const map = new Map<string, DonorRiskFlag>()
          for (const f of flags) map.set(f.donorId, f)
          setRiskFlags(map)
        }
      } catch {
        // Silent fallback — risk column simply won't render
      }
    }
    void loadFlags()
    return () => { cancelled = true }
  }, [])

  async function handleCreateDonor(event: React.FormEvent) {
    event.preventDefault()
    setSuccess(null)
    setError(null)
    try {
      await createDonor(name, email)
      setSuccess('Donor saved successfully.')
      setName('')
      setEmail('')
      setPage(1)
      const data = await fetchDonors({ page: 1, pageSize, desc: true })
      setItems(data.items)
      setTotalCount(data.totalCount)
    } catch (err) {
      setError(toUserFacingError(err, 'Failed to save donor'))
    }
  }

  return (
    <section>
      <h1>Donors & Contributions</h1>
      <p className="lead">Manage donor profiles, contribution logs, and allocation tracking.</p>
      <form className="feature-card" onSubmit={handleCreateDonor}>
        <h2>Create donor</h2>
        <input placeholder="Name" value={name} onChange={(e) => setName(e.target.value)} required minLength={2} />
        <input placeholder="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
        <button className="button" type="submit">Save donor</button>
      </form>

      <article className="feature-card" style={{ marginTop: '1rem' }}>
        <h2>Donor profiles</h2>
        <input
          value={search}
          onChange={(e) => {
            setSearch(e.target.value)
            setPage(1)
          }}
          placeholder="Filter by name or email"
        />
        {loading && <p role="status">Loading donors…</p>}
        {error && <ApiErrorNotice error={error} />}
        {success && <p role="status">{success}</p>}
        {!loading && !error && (
          <>
            <table>
              <thead>
                <tr>
                  <th>Name</th>
                  <th>Email</th>
                  <th>Last activity</th>
                  <th>Lifetime</th>
                  {riskFlags.size > 0 && <th>Lapse Risk</th>}
                </tr>
              </thead>
              <tbody>
                {items.map((x) => {
                  const flag = riskFlags.get(x.id.toString())
                  return (
                    <tr key={x.id}>
                      <td>{x.name}</td>
                      <td>{x.email}</td>
                      <td>{new Date(x.lastActivityAt).toLocaleDateString()}</td>
                      <td>${x.lifetimeContributions.toFixed(2)}</td>
                      {riskFlags.size > 0 && (
                        <td>
                          {flag && (
                            <span title={flag.signals.join(' · ')}>
                              <RiskBadge level={flag.level} />
                            </span>
                          )}
                        </td>
                      )}
                    </tr>
                  )
                })}
              </tbody>
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
