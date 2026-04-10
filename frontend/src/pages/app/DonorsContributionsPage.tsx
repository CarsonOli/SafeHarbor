import { useEffect, useMemo, useState } from 'react'
import { ApiErrorNotice } from '../../components/ApiErrorNotice'
import { fetchAllDonations } from '../../services/donationsApi'
import { toUserFacingError } from '../../services/httpErrors'
import type { DonationFilters, DonationListItem } from '../../types/donations'
import { 
  createDonorProfile, 
  updateDonorProfile, 
  deleteDonorProfile 
} from '../../services/adminOperationsApi'
import type { DonorProfileUpsertPayload } from '../../services/adminOperationsApi'
// ... rest of your imports

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
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedDonation, setSelectedDonation] = useState<DonationListItem | null>(null);

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

  const handleSaveDonor = async (formData: DonorProfileUpsertPayload) => {
    try {
      if (selectedDonation) {
        // UPDATE
        await updateDonorProfile(String(selectedDonation.donationId), formData);
      } else {
        // CREATE
        await createDonorProfile(formData);
      }
      setIsModalOpen(false);
      setSelectedDonation(null);
      window.location.reload(); 
    } catch (err) {
      console.error(err);
      alert("Donor save failed.");
    }
  };

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
                    <td style={{ textAlign: 'center' }}>
                      <button 
                        onClick={() => { setSelectedDonation(donation); setIsModalOpen(true); }}
                        className="button button-secondary" 
                        style={{ padding: '2px 8px', fontSize: '0.8rem', marginRight: '4px' }}
                      >
                        Edit
                      </button>
                      <button 
                        onClick={async () => {
                          if (window.confirm("Remove this transaction record?")) {
                            // This version is "bulletproof" 🛡️
                            await deleteDonorProfile(donation.donationId?.toString() || '');
                            window.location.reload();
                          }
                        }}
                        className="button button-danger" 
                        style={{ padding: '2px 8px', fontSize: '0.8rem', color: '#be123c' }}
                      >
                        Delete
                      </button>
                    </td>
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
      {isModalOpen && (
        <div className="modal-overlay" style={{ position: 'fixed', top: 0, left: 0, width: '100%', height: '100%', background: 'rgba(0,0,0,0.5)', display: 'flex', justifyContent: 'center', alignItems: 'center', zIndex: 1000 }}>
          <div className="modal-content" style={{ background: 'white', padding: '2rem', borderRadius: '8px', width: '450px' }}>
            <h2>{selectedDonation ? 'Update Contribution' : 'New Contribution'}</h2>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem', marginTop: '1rem' }}>
              <input placeholder="Donor Name" defaultValue={selectedDonation?.donorDisplayName || ''} style={{ padding: '8px' }} />
              
              <label style={{ fontSize: '0.8rem', fontWeight: 600 }}>Classification</label>
              <select defaultValue={selectedDonation?.donationType || ''} style={{ padding: '8px' }}>
                <option>Monetary Donor</option>
                <option>Volunteer (Time)</option>
                <option>Skills Contributor</option>
                <option>In-Kind (Goods)</option>
              </select>

              <label style={{ fontSize: '0.8rem', fontWeight: 600 }}>Status</label>
              <select style={{ padding: '8px' }}>
                <option>Active</option>
                <option>Inactive</option>
              </select>

              <input type="number" placeholder="Amount / Est. Value" defaultValue={selectedDonation?.amount || 0} style={{ padding: '8px' }} />
            </div>

            <div style={{ marginTop: '1.5rem', display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' }}>
              <button onClick={() => { setIsModalOpen(false); setSelectedDonation(null); }} className="button button-secondary">Cancel</button>
              <button 
                type="button"
                onClick={() => {
                  // These selectors find the values in the modal you just filled out
                  const name = (document.querySelector('input[placeholder="Donor Name"]') as HTMLInputElement).value;
                  const type = (document.querySelector('select:nth-of-type(1)') as HTMLSelectElement).value;
                  const status = (document.querySelector('select:nth-of-type(2)') as HTMLSelectElement).value;

                  handleSaveDonor({
                    name,
                    type,
                    status,
                    email: selectedDonation?.supporterEmail || 'new@donor.org'
                  });
                }} 
                className="button button-primary"
              >
                {selectedDonation ? 'Save Changes' : 'Create Entry'}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  )
}
