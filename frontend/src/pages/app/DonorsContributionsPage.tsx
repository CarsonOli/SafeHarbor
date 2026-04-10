import { useEffect, useMemo, useState } from 'react'
import { ApiErrorNotice } from '../../components/ApiErrorNotice'
import { fetchAllDonations } from '../../services/donationsApi'
import { toUserFacingError } from '../../services/httpErrors'
import type { DonationFilters, DonationListItem } from '../../types/donations'

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount)
}

function toDateInput(value: string | null): string {
  if (!value) return ''
  return value.slice(0, 10)
}

export function DonorsContributionsPage() {
  const [items, setItems] = useState<DonationListItem[]>([])
  const [page, setPage] = useState(1)
  const [pageSize] = useState(20)
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const [filters, setFilters] = useState<DonationFilters>({
    fromDate: '',
    toDate: '',
    donationType: '',
    campaign: '',
    channelSource: '',
    supporterType: '',
    frequency: '',
  })

  const hasFilters = useMemo(
    () => Object.values(filters).some((value) => typeof value === 'string' && value.trim().length > 0),
    [filters],
  )

  useEffect(() => {
    let cancelled = false

    async function load() {
      try {
        setLoading(true)
        setError(null)
        const payload = await fetchAllDonations({ ...filters, page, pageSize })
        if (cancelled) return
        setItems(payload.items)
        setTotalCount(payload.totalCount)
      } catch (err) {
        if (!cancelled) {
          setError(toUserFacingError(err, 'Failed to load donations'))
        }
      } finally {
        if (!cancelled) {
          setLoading(false)
        }
      }
    }

    void load()
    return () => {
      cancelled = true
    }
  }, [filters, page, pageSize])

  return (
    <section>
      <h1>Donations</h1>
      <p className="lead">View all donation transactions with supporter identity from the CRM profile.</p>

      <article className="feature-card">
        <h2>Filters</h2>
        <div style={{ display: 'grid', gap: '0.5rem', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))' }}>
          <label>
            From date
            <input
              type="date"
              value={filters.fromDate}
              onChange={(e) => {
                setPage(1)
                setFilters((current) => ({ ...current, fromDate: e.target.value }))
              }}
            />
          </label>
          <label>
            To date
            <input
              type="date"
              value={filters.toDate}
              onChange={(e) => {
                setPage(1)
                setFilters((current) => ({ ...current, toDate: e.target.value }))
              }}
            />
          </label>
          <label>
            Donation type
            <input
              value={filters.donationType}
              onChange={(e) => {
                setPage(1)
                setFilters((current) => ({ ...current, donationType: e.target.value }))
              }}
              placeholder="Cash, In-kind..."
            />
          </label>
          <label>
            Campaign
            <input
              value={filters.campaign}
              onChange={(e) => {
                setPage(1)
                setFilters((current) => ({ ...current, campaign: e.target.value }))
              }}
              placeholder="Campaign name"
            />
          </label>
          <label>
            Channel/source
            <input
              value={filters.channelSource}
              onChange={(e) => {
                setPage(1)
                setFilters((current) => ({ ...current, channelSource: e.target.value }))
              }}
              placeholder="Online, Event..."
            />
          </label>
          <label>
            Supporter type
            <input
              value={filters.supporterType}
              onChange={(e) => {
                setPage(1)
                setFilters((current) => ({ ...current, supporterType: e.target.value }))
              }}
              placeholder="Individual, Organization..."
            />
          </label>
          <label>
            Frequency
            <input
              value={filters.frequency}
              onChange={(e) => {
                setPage(1)
                setFilters((current) => ({ ...current, frequency: e.target.value }))
              }}
              placeholder="One-time, Monthly..."
            />
          </label>
        </div>
        {hasFilters && (
          <button
            type="button"
            className="button button-secondary"
            onClick={() => {
              setPage(1)
              setFilters({
                fromDate: '',
                toDate: '',
                donationType: '',
                campaign: '',
                channelSource: '',
                supporterType: '',
                frequency: '',
              })
            }}
          >
            Clear filters
          </button>
        )}
      </article>

      <article className="feature-card" style={{ marginTop: '1rem' }}>
        <h2>Donation transactions</h2>
        {loading && <p role="status">Loading donations...</p>}
        {error && <ApiErrorNotice error={error} />}
        {!loading && !error && items.length === 0 && (
          <p>No donations matched your current filters.</p>
        )}
        {!loading && !error && items.length > 0 && (
          <>
            <table>
              <thead>
                <tr>
                  <th>Date</th>
                  <th>Donor</th>
                  <th>Type</th>
                  <th>Campaign</th>
                  <th>Channel</th>
                  <th>Amount</th>
                  <th>In-kind Est.</th>
                </tr>
              </thead>
              <tbody>
                {items.map((donation) => (
                  <tr key={donation.donationId}>
                    <td>{toDateInput(donation.donationDate)}</td>
                    <td>
                      {donation.donorDisplayName}
                      {donation.supporterEmail ? <div>{donation.supporterEmail}</div> : null}
                    </td>
                    <td>{donation.donationType}</td>
                    <td>{donation.campaignName ?? '-'}</td>
                    <td>{donation.channelSource ?? '-'}</td>
                    <td>{formatCurrency(donation.amount)}</td>
                    <td>{formatCurrency(donation.estimatedValue)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
            <div style={{ marginTop: '0.75rem' }}>
              <button disabled={page <= 1} onClick={() => setPage((current) => current - 1)}>Previous</button>
              <span> Page {page} of {Math.max(1, Math.ceil(totalCount / pageSize))} </span>
              <button disabled={page * pageSize >= totalCount} onClick={() => setPage((current) => current + 1)}>Next</button>
            </div>
          </>
        )}
      </article>
    </section>
  )
}
