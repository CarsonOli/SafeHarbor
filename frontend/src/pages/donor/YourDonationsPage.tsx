import { useEffect, useState } from 'react'
import { ApiErrorNotice } from '../../components/ApiErrorNotice'
import { fetchCurrentUserDonations } from '../../services/donationsApi'
import { toUserFacingError } from '../../services/httpErrors'
import type { YourDonationsResponse } from '../../types/donations'

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount)
}

export function YourDonationsPage() {
  const [payload, setPayload] = useState<YourDonationsResponse | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    let cancelled = false
    async function load() {
      try {
        setLoading(true)
        setError(null)
        const data = await fetchCurrentUserDonations()
        if (!cancelled) {
          setPayload(data)
        }
      } catch (err) {
        if (!cancelled) {
          setError(toUserFacingError(err, 'Failed to load your donations'))
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
  }, [])

  return (
    <section>
      <h1>Your Donations</h1>
      <p className="lead">Track your donation history and in-kind contribution details.</p>

      {loading && <p role="status">Loading your donations...</p>}
      {error && <ApiErrorNotice error={error} />}

      {!loading && !error && payload && !payload.hasLinkedSupporter && (
        <article className="feature-card">
          <h2>Profile not linked yet</h2>
          <p>
            Your account is not linked to a supporter profile yet, so we cannot show donation history.
            Please contact the team and ask them to link your account.
          </p>
        </article>
      )}

      {!loading && !error && payload?.hasLinkedSupporter && payload.donations.length === 0 && (
        <article className="feature-card">
          <h2>No donations yet</h2>
          <p>You do not have any donations recorded yet. Thank you for supporting SafeHarbor.</p>
        </article>
      )}

      {!loading && !error && payload?.hasLinkedSupporter && payload.donations.length > 0 && (
        <article className="feature-card">
          <h2>{payload.supporterDisplayName ?? 'Donor profile'}</h2>
          <table>
            <thead>
              <tr>
                <th>Date</th>
                <th>Type</th>
                <th>Campaign</th>
                <th>Channel</th>
                <th>Amount</th>
                <th>In-kind</th>
              </tr>
            </thead>
            <tbody>
              {payload.donations.map((donation) => (
                <tr key={donation.donationId}>
                  <td>{donation.donationDate ? donation.donationDate.slice(0, 10) : '—'}</td>
                  <td>{donation.donationType}</td>
                  <td>{donation.campaignName ?? '—'}</td>
                  <td>{donation.channelSource ?? '—'}</td>
                  <td>{formatCurrency(donation.amount)}</td>
                  <td>
                    {donation.inKindItems.length === 0 && donation.estimatedValue <= 0
                      ? '—'
                      : (
                        <div>
                          <div>Est. {formatCurrency(donation.estimatedValue)}</div>
                          {donation.inKindItems.slice(0, 2).map((item) => (
                            <div key={item.itemId}>
                              {item.itemName ?? 'Item'} ({item.quantity} {item.unitOfMeasure ?? 'units'})
                            </div>
                          ))}
                          {donation.inKindItems.length > 2 ? <div>+{donation.inKindItems.length - 2} more</div> : null}
                        </div>
                      )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </article>
      )}
    </section>
  )
}
