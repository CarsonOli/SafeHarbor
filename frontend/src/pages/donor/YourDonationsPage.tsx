import { useEffect, useMemo, useState, type CSSProperties } from 'react'
import { useAuth } from '../../auth/AuthContext'
import { fetchDonorDashboard, submitDonation } from '../../services/donorApi'
import type { DonorDashboardData } from '../../types/impact'

// Preset donation amounts shown as quick-select buttons on the donation form.
// Update these values if the organization adjusts its suggested giving tiers.
const PRESET_AMOUNTS = [25, 50, 100, 250]

/** Formats a USD number as a locale-aware currency string, e.g. "$1,250.00". */
function formatCurrency(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value)
}

/**
 * Donor Dashboard — "Your Donations"
 *
 * This page is accessible only to users with the "Donor" role (route: /donor/dashboard).
 * It shows:
 *   1. A hero section with the donor's name and lifetime giving total.
 *   2. A 12-month donation history bar chart.
 *   3. A campaign goal progress bar (if an active campaign exists).
 *   4. An impact metric card showing estimated girls helped.
 *   5. A donation form so donors can give again without leaving the page.
 *
 * Data is fetched from GET /api/donor/dashboard?email={session.email}.
 * After a new donation is submitted (POST /api/donor/contribution), the dashboard
 * is re-fetched so all metrics update immediately.
 *
 * STYLING:
 *   All donor-specific styles are scoped under .donor-dashboard-page in index.css
 *   so they don't bleed into staff pages.
 */
export function YourDonationsPage() {
  const { session } = useAuth()

  const [data, setData] = useState<DonorDashboardData | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // Donation form state.
  const [selectedPreset, setSelectedPreset] = useState<number | null>(null)
  const [customAmount, setCustomAmount] = useState('')
  const [donationStatus, setDonationStatus] = useState<'idle' | 'submitting' | 'success' | 'error'>('idle')
  const [donationMessage, setDonationMessage] = useState<string | null>(null)

  /** Loads (or reloads) the dashboard data from the API. */
  const loadDashboard = async () => {
    if (!session?.email) return
    setLoading(true)
    setError(null)
    try {
      const result = await fetchDonorDashboard(session.email)
      setData(result)
    } catch {
      setError('Unable to load your donation history right now. Please try again later.')
    } finally {
      setLoading(false)
    }
  }

  // Fetch on mount.
  useEffect(() => {
    void loadDashboard()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [session?.email])

  // Compute the max monthly amount for scaling bar widths (so the largest bar = 100%).
  // useMemo prevents recalculating on every render.
  const maxMonthlyAmount = useMemo(
    () => Math.max(...(data?.monthlyHistory.map((p) => p.amount) ?? [1]), 1),
    [data],
  )

  /** Resolves the donation amount from either the preset selection or the custom input. */
  const resolvedAmount = (): number | null => {
    if (customAmount.trim() !== '') {
      const parsed = parseFloat(customAmount)
      return isNaN(parsed) || parsed <= 0 ? null : parsed
    }
    return selectedPreset
  }

  /** Handles the "Donate Now" button click. */
  const handleDonate = async () => {
    const amount = resolvedAmount()
    if (!amount || !session?.email) return

    setDonationStatus('submitting')
    setDonationMessage(null)

    try {
      const message = await submitDonation(session.email, amount, 'One-time')
      setDonationStatus('success')
      setDonationMessage(message)
      // Reset the form.
      setSelectedPreset(null)
      setCustomAmount('')
      // Re-fetch the dashboard so lifetime total, impact count, and goal progress all update.
      await loadDashboard()
    } catch (err) {
      setDonationStatus('error')
      setDonationMessage(
        err instanceof Error ? err.message : 'Something went wrong. Please try again.',
      )
    }
  }

  return (
    // .donor-dashboard-page scopes the teal/orange CSS custom properties defined in index.css.
    // All donor-specific color overrides cascade from this wrapper class.
    <div className="donor-dashboard-page">
      {loading && <p role="status">Loading your dashboard…</p>}
      {!loading && error && <p role="alert">{error}</p>}

      {!loading && data && (
        <>
          {/* ── 1. Hero Section ─────────────────────────────────────────────── */}
          {/* Dark teal background with the donor's name and lifetime giving total. */}
          <section className="donor-hero-section" aria-label="Your giving summary">
            <p className="eyebrow">Total Giving</p>
            <h1>Hello, {data.donorName}</h1>
            <p className="metric-value">{formatCurrency(data.lifetimeDonated)}</p>
            <p className="caption">Lifetime contributions — thank you for your support.</p>
          </section>

          {/* ── 2. Donation History Chart ────────────────────────────────────── */}
          {/* 12-month bar chart using the same .bar-list / .bar pattern as ImpactDashboardPage. */}
          {/* --bar-width scales each bar relative to the highest-value month (= 100%). */}
          <article className="chart-card" aria-label="Donation history">
            <h2>Donation history — last 12 months</h2>
            <ul className="bar-list" aria-label="Monthly donation chart">
              {data.monthlyHistory.map((point) => {
                // Scale bar width: highest month = 100%, others proportional.
                const barWidthPercent =
                  point.amount === 0 ? 0 : (point.amount / maxMonthlyAmount) * 100
                return (
                  <li key={point.month}>
                    <span>{point.month}</span>
                    <div
                      className="bar"
                      style={{ '--bar-width': `${barWidthPercent}%` } as CSSProperties}
                      aria-hidden="true"
                    />
                    {/* Show the dollar amount, or an em-dash for zero months. */}
                    <span>{point.amount > 0 ? formatCurrency(point.amount) : '—'}</span>
                  </li>
                )
              })}
            </ul>
          </article>

          {/* ── 3. Campaign Goal Widget ──────────────────────────────────────── */}
          {/* Shows the active campaign's fundraising goal and overall progress. */}
          {/* If no campaign is currently active, a friendly placeholder is shown. */}
          <article className="metric-card" aria-label="Campaign goal">
            {data.activeCampaign ? (
              <>
                <h2>{data.activeCampaign.campaignName}</h2>
                <p className="eyebrow">Campaign Goal</p>

                {/* Progress bar: --bar-width uses progressPercent from the API. */}
                <div
                  className="bar"
                  style={
                    { '--bar-width': `${data.activeCampaign.progressPercent}%` } as CSSProperties
                  }
                  role="progressbar"
                  aria-valuenow={data.activeCampaign.progressPercent}
                  aria-valuemin={0}
                  aria-valuemax={100}
                  aria-label={`Campaign progress: ${data.activeCampaign.progressPercent}%`}
                />

                <p>
                  Campaign raised:{' '}
                  <strong>{formatCurrency(data.activeCampaign.totalRaisedAllDonors)}</strong> of{' '}
                  <strong>{formatCurrency(data.activeCampaign.goalAmount)}</strong>
                </p>
                <p className="caption">
                  Your contribution:{' '}
                  <strong>{formatCurrency(data.activeCampaign.thisDonorContributed)}</strong>
                </p>
              </>
            ) : (
              <>
                <h2>Campaign Goal</h2>
                <p className="lead">No active campaign right now.</p>
                <p className="caption">Check back soon for our next fundraising drive.</p>
              </>
            )}
          </article>

          {/* ── 4. Impact Metric Card ────────────────────────────────────────── */}
          {/* Shows how many girls have been helped through this donor's contributions. */}
          {/* modelVersion badge tells donors and staff which calculation method was used. */}
          <article className="metric-card donor-impact-card" aria-label="Your impact">
            <p className="eyebrow">Your Impact</p>
            <p className="metric-value">{data.impact.girlsHelped.toLocaleString()}</p>
            <p className="lead">{data.impact.impactLabel}</p>
            {/* Model version badge: helps team track when ML replaces the rule-based formula. */}
            <p className="caption">Calculated by: {data.impact.modelVersion}</p>
          </article>

          {/* ── 5. Donation Form ─────────────────────────────────────────────── */}
          {/* Allows donors to make another contribution without leaving the page. */}
          {/* After a successful submission the dashboard re-fetches to update all metrics. */}
          <article className="metric-card" aria-label="Make another donation">
            <h2>Make Another Donation</h2>

            {/* Preset amount buttons — highlight in orange when selected. */}
            <div className="donor-amount-presets" role="group" aria-label="Preset donation amounts">
              {PRESET_AMOUNTS.map((amount) => (
                <button
                  key={amount}
                  type="button"
                  className={`donor-amount-btn${selectedPreset === amount && customAmount === '' ? ' selected' : ''}`}
                  onClick={() => {
                    setSelectedPreset(amount)
                    setCustomAmount('') // clear custom input when preset is chosen
                    setDonationStatus('idle')
                    setDonationMessage(null)
                  }}
                  aria-pressed={selectedPreset === amount && customAmount === ''}
                >
                  {formatCurrency(amount)}
                </button>
              ))}
            </div>

            {/* Custom amount input — takes precedence over preset selection when filled. */}
            <label htmlFor="custom-amount">Or enter amount</label>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.25rem', marginBottom: '1rem' }}>
              <span aria-hidden="true">$</span>
              <input
                id="custom-amount"
                type="number"
                min="1"
                step="any"
                placeholder="0.00"
                value={customAmount}
                onChange={(e) => {
                  setCustomAmount(e.target.value)
                  setDonationStatus('idle')
                  setDonationMessage(null)
                }}
                aria-label="Custom donation amount in US dollars"
              />
            </div>

            {/* Submit button — disabled while submitting or when no amount is selected. */}
            <button
              type="button"
              className="button donor-cta-button"
              onClick={() => void handleDonate()}
              disabled={donationStatus === 'submitting' || resolvedAmount() === null}
              aria-busy={donationStatus === 'submitting'}
            >
              {donationStatus === 'submitting' ? 'Processing…' : 'Donate Now →'}
            </button>

            {/* Feedback messages after submission attempt. */}
            {donationStatus === 'success' && donationMessage && (
              <p role="status" style={{ marginTop: '0.75rem', color: 'var(--color-hero-bg)' }}>
                {donationMessage}
              </p>
            )}
            {donationStatus === 'error' && donationMessage && (
              <p role="alert" style={{ marginTop: '0.75rem', color: 'var(--color-danger)' }}>
                {donationMessage}
              </p>
            )}
          </article>
        </>
      )}
    </div>
  )
}
