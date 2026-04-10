import React, { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { submitDonation } from '../services/donorApi'

// ── Constants ─────────────────────────────────────────────────────────────────

/** Quick-select donation amounts shown as preset buttons. */
const PRESET_AMOUNTS = [25, 50, 100, 250]

/** Confetti pieces: each entry is [left%, color, animation-delay, size]. */
const CONFETTI_PIECES: [string, string, string, string][] = [
  ['8%',  '#e8704a', '0s',    '10px'],
  ['18%', '#2a5c5c', '0.2s',  '8px' ],
  ['30%', '#f5c842', '0.1s',  '12px'],
  ['42%', '#2a5c5c', '0.35s', '9px' ],
  ['55%', '#e8704a', '0.15s', '11px'],
  ['65%', '#2a5c5c', '0.4s',  '8px' ],
  ['75%', '#f5c842', '0.05s', '10px'],
  ['85%', '#2a5c5c', '0.25s', '13px'],
  ['22%', '#e8704a', '0.45s', '8px' ],
  ['48%', '#f5c842', '0.3s',  '9px' ],
  ['70%', '#2a5c5c', '0.5s',  '11px'],
  ['90%', '#e8704a', '0.1s',  '8px' ],
]

// ── Helpers ───────────────────────────────────────────────────────────────────

/** Formats a number as "$X" or "$X.XX" for the donate button label. */
function formatDonateAmount(value: number): string {
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: value % 1 === 0 ? 0 : 2,
  }).format(value)
}

/**
 * Formats a raw digit string as a spaced card number.
 * "1234123412341234" → "1234 1234 1234 1234"
 * Caps at 16 digits (19 chars with spaces).
 */
function formatCardNumber(raw: string): string {
  const digits = raw.replace(/\D/g, '').slice(0, 16)
  return digits.replace(/(.{4})/g, '$1 ').trim()
}

/**
 * Formats a raw digit string as an MM/YY expiry.
 * "1225" → "12/25"
 */
function formatExpiry(raw: string): string {
  const digits = raw.replace(/\D/g, '').slice(0, 4)
  if (digits.length <= 2) return digits
  return `${digits.slice(0, 2)}/${digits.slice(2)}`
}

// ── Validation ────────────────────────────────────────────────────────────────

/** Returns a map of field → error message for any invalid fields. Empty map = all valid. */
function validateForm(fields: {
  amount: number | null
  name: string
  email: string
  address: string
  city: string
  state: string
  zip: string
  cardNumber: string
  expiry: string
  cvv: string
}): Record<string, string> {
  const errors: Record<string, string> = {}

  if (!fields.amount || fields.amount <= 0)
    errors.amount = 'Please select or enter a donation amount.'

  if (!fields.name.trim())
    errors.name = 'Full name is required.'

  if (!fields.email.trim() || !/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(fields.email))
    errors.email = 'A valid email address is required.'

  if (!fields.address.trim())
    errors.address = 'Billing address is required.'

  if (!fields.city.trim())
    errors.city = 'City is required.'

  if (!fields.state.trim())
    errors.state = 'State is required.'

  if (!fields.zip.trim() || !/^\d{5}(-\d{4})?$/.test(fields.zip))
    errors.zip = 'A valid ZIP code is required (e.g. 90210).'

  // Card number: must be exactly 16 digits after stripping spaces.
  const rawCard = fields.cardNumber.replace(/\s/g, '')
  if (rawCard.length !== 16 || !/^\d{16}$/.test(rawCard))
    errors.cardNumber = 'Card number must be 16 digits.'

  // Expiry: MM/YY format, month 01–12, year ≥ current year.
  const expiryMatch = /^(\d{2})\/(\d{2})$/.exec(fields.expiry)
  if (!expiryMatch) {
    errors.expiry = 'Expiry must be in MM/YY format.'
  } else {
    const month = parseInt(expiryMatch[1], 10)
    const year  = parseInt('20' + expiryMatch[2], 10)
    const now   = new Date()
    if (month < 1 || month > 12) errors.expiry = 'Month must be 01–12.'
    else if (year < now.getFullYear() || (year === now.getFullYear() && month < now.getMonth() + 1))
      errors.expiry = 'Card has expired.'
  }

  if (!/^\d{3,4}$/.test(fields.cvv))
    errors.cvv = 'CVV must be 3–4 digits.'

  return errors
}

// ── Celebration Component ─────────────────────────────────────────────────────

/** Renders the post-donation success screen with CSS confetti animation. */
function Celebration({ amount, isDonor, isMonthly }: { amount: number; isDonor: boolean; isMonthly: boolean }) {
  const navigate = useNavigate()

  return (
    <div className="donate-celebration-wrapper" aria-live="polite">
      {/* Confetti pieces — positioned absolutely above the card */}
      <div className="confetti-container" aria-hidden="true">
        {CONFETTI_PIECES.map(([left, color, delay, size], i) => (
          <div
            key={i}
            className="confetti-piece"
            style={{
              left,
              backgroundColor: color,
              animationDelay: delay,
              width: size,
              height: size,
            } as React.CSSProperties}
          />
        ))}
      </div>

      {/* Success card */}
      <div className="donate-celebration">
        {/* Animated SVG checkmark */}
        <svg
          className="checkmark-svg"
          viewBox="0 0 52 52"
          aria-hidden="true"
        >
          {/* Circle that draws itself */}
          <circle className="checkmark-circle" cx="26" cy="26" r="25" fill="none" />
          {/* Tick that draws itself after the circle */}
          <path className="checkmark-tick" fill="none" d="M14.5 27.5 L22 35 L38 17" />
        </svg>

        <h2>Thank You!</h2>
        <p className="lead">
          Your{' '}
          <strong>{formatDonateAmount(amount)}</strong> {isMonthly ? 'monthly ' : ''} donation to SafeHarbor
          International is making a difference.
        </p>
        <p className="caption">
          Every dollar helps bring a girl home to safety.
        </p>

        {/* CTA differs based on login state */}
        {isDonor ? (
          <button
            type="button"
            className="button donate-cta-button"
            onClick={() => navigate('/donor/dashboard')}
          >
            Go to My Dashboard
          </button>
        ) : (
          <Link to="/impact" className="button donate-cta-button">
            See Our Impact
          </Link>
        )}
      </div>
    </div>
  )
}

// ── Visitor Modal ─────────────────────────────────────────────────────────────

/**
 * Modal shown to unauthenticated visitors when they click submit.
 * Prompts them to sign in to track their donation, or continue as a guest.
 */
function VisitorModal({
  onSignIn,
  onContinue,
}: {
  onSignIn: () => void
  onContinue: () => void
}) {
  return (
    // Backdrop
    <div
      className="modal-backdrop"
      role="dialog"
      aria-modal="true"
      aria-labelledby="visitor-modal-title"
    >
      <div className="modal">
        <h2 id="visitor-modal-title">Track your impact</h2>
        <p>
          Create a free account to see your donation history, track your impact,
          and receive updates on how your gift is helping girls find safety.
        </p>

        {/* Sign In takes the visitor to /login; Continue completes the simulated payment */}
        <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', marginTop: '1.5rem' }}>
          <button type="button" className="button button-primary" onClick={onSignIn}>
            Sign In / Create Account
          </button>
          <button type="button" className="button button-secondary" onClick={onContinue}>
            Continue as Guest
          </button>
        </div>
      </div>
    </div>
  )
}

// ── Main Page Component ───────────────────────────────────────────────────────

/**
 * Donation Landing Page — /donate
 *
 * Publicly accessible. The page flow differs by authentication state:
 *
 * VISITOR (not logged in):
 *   Fill form → click submit → visitor modal → "Continue as Guest" → celebration
 *
 * LOGGED-IN DONOR:
 *   Fill form → click submit → POST /api/donor/contribution → celebration
 *   The donation is recorded and will appear on /donor/dashboard.
 *
 * SIMULATED PAYMENT:
 *   Card data is validated client-side but never sent to any server.
 *   A 1.5-second fake "processing" delay mimics a real payment flow.
 *   TODO: Replace the simulated submit with a real Stripe integration when ready.
 */
export function DonatePage() {
  const { session } = useAuth()
  const navigate    = useNavigate()

  const isDonor = session?.role === 'Donor'

  // ── Form state ────────────────────────────────────────────────────────────
  const [isMonthly, setIsMonthly]           = useState(false) // NEW: Recurring toggle
  const [selectedPreset, setSelectedPreset] = useState<number | null>(100) // default $100
  const [customAmount, setCustomAmount]     = useState('')

  const [name,       setName]       = useState('')
  const [email,      setEmail]      = useState(session?.email ?? '') // pre-fill if logged in
  const [address,    setAddress]    = useState('')
  const [city,       setCity]       = useState('')
  const [state,      setState]      = useState('')
  const [zip,        setZip]        = useState('')
  const [cardNumber, setCardNumber] = useState('')
  const [expiry,     setExpiry]     = useState('')
  const [cvv,        setCvv]        = useState('')

  const [errors,  setErrors]  = useState<Record<string, string>>({})
  const [status, setStatus] = useState<'idle' | 'visitor-modal' | 'processing' | 'success' | 'error'>('idle')
  const [apiError, setApiError] = useState<string | null>(null)

  /** Resolves the final donation amount from preset or custom input. */
  const resolvedAmount = (): number | null => {
    if (customAmount.trim() !== '') {
      const parsed = parseFloat(customAmount)
      return isNaN(parsed) || parsed <= 0 ? null : parsed
    }
    return selectedPreset
  }

  // ── Submit handler ────────────────────────────────────────────────────────

  const handleSubmit = () => {
    const amount = resolvedAmount()

    // Validate all fields before proceeding.
    const validationErrors = validateForm({
      amount,
      name, email, address, city, state, zip, cardNumber, expiry, cvv,
    })

    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors)
      // Scroll to first error to make it visible.
      document.querySelector('.donate-field-error')?.scrollIntoView({ behavior: 'smooth', block: 'center' })
      return
    }

    setErrors({})

    // Visitors see a prompt to sign in before completing the donation.
    if (!session) {
      setStatus('visitor-modal')
      return
    }

    // Logged-in donor: proceed directly to payment simulation.
    void processPayment(amount!)
  }

  /**
   * Simulates payment processing.
   * For logged-in donors, also POSTs to the backend to record the contribution.
   * TODO: Replace the 1.5s delay + backend call with real Stripe Elements when a
   *       payment processor is chosen.
   */
  const processPayment = async (amount: number) => {
    setStatus('processing')
    setApiError(null)

    // Simulate network/processing delay (1.5 seconds).
    await new Promise(resolve => setTimeout(resolve, 1500))

    if (isDonor && session?.email) {
      try {
        // Record the donation in the backend so it appears on /donor/dashboard.
        await submitDonation(session.email, amount, isMonthly ? 'Monthly' : 'One-time')
      } catch (err) {
        // Non-fatal: show a warning but still celebrate the simulated success.
        console.warn('[DonatePage] Failed to record donation to backend:', err)
      }
    }

    setStatus('success')
  }

  // ── Derived values ────────────────────────────────────────────────────────

  const amount       = resolvedAmount()
  const buttonLabel  = status === 'processing'
    ? 'Processing…'
    : amount
    ? `Donate ${formatDonateAmount(amount)}${isMonthly ? ' Monthly' : ''} Now →`
    : 'Donate Now →'

  // ── Render: success celebration ───────────────────────────────────────────

  if (status === 'success') {
    return (
      <div className="donate-celebration-wrapper">
         {/* You can update your Celebration component to say "Monthly Donation" here */}
         <Celebration amount={amount ?? 0} isDonor={isDonor} isMonthly={isMonthly} />
         {isMonthly && <p className="recurring-note">Your first monthly gift is being processed!</p>}
      </div>
    )
  }

  // ── Render: donation form ─────────────────────────────────────────────────

  return (
    <div className="donate-page">

      {/* Visitor modal — shown when an unauthenticated user clicks submit */}
      {status === 'visitor-modal' && (
        <VisitorModal
          onSignIn={() => navigate('/login')}
          onContinue={() => {
            setStatus('idle')
            void processPayment(amount ?? 0)
          }}
        />
      )}

      {/* ── Hero ──────────────────────────────────────────────────────────── */}
      <section className="donate-hero" aria-label="Donation page hero">
        <p className="eyebrow">501(c)(3) Nonprofit Organization</p>
        <h1>Make a Difference</h1>
        <p className="lead">
          Every dollar you give helps SafeHarbor International rescue, rehabilitate,
          and reintegrate girls who are survivors of trafficking and abuse.
        </p>
      </section>

      {/* ── Step 1: Gift Amount ────────────────────────────────────────────── */}
      <div className="donate-form-card">
        <p className="donate-step-label">Step 1 — Choose your gift frequency and amount</p>

        <div className="donation-frequency-toggle">
          <button 
            type="button"
            className={`toggle-btn ${!isMonthly ? 'active' : ''}`}
            onClick={() => setIsMonthly(false)}
          >
            One-time
          </button>
          <button 
            type="button"
            className={`toggle-btn ${isMonthly ? 'active' : ''}`}
            onClick={() => setIsMonthly(true)}
          >
            Monthly
          </button>
        </div>
        
        {/* Preset amount quick-select buttons */}
        <div className="donor-amount-presets" role="group" aria-label="Preset donation amounts">
          {PRESET_AMOUNTS.map(preset => (
            <button
              key={preset}
              type="button"
              // Highlight when selected and no custom amount has been typed
              className={`donor-amount-btn${selectedPreset === preset && customAmount === '' ? ' selected' : ''}`}
              onClick={() => {
                setSelectedPreset(preset)
                setCustomAmount('')    // clear custom when preset is chosen
                setErrors(e => ({ ...e, amount: '' }))
              }}
              aria-pressed={selectedPreset === preset && customAmount === ''}
            >
              {formatDonateAmount(preset)}
              {isMonthly && <span className="per-month">/mo</span>}
            </button>
          ))}
        </div>

        {/* Custom amount input */}
        <label htmlFor="donate-custom-amount">Or enter a custom amount</label>
        <div className="donate-amount-input-row">
          <span className="donate-currency-prefix" aria-hidden="true">$</span>
          <input
            id="donate-custom-amount"
            type="number"
            min="1"
            step="any"
            placeholder="0.00"
            value={customAmount}
            onChange={e => {
              setCustomAmount(e.target.value)
              setErrors(err => ({ ...err, amount: '' }))
            }}
            aria-label="Custom donation amount in US dollars"
            aria-invalid={!!errors.amount}
          />
        </div>
        {errors.amount && <p className="donate-field-error">{errors.amount}</p>}
      </div>

      {/* ── Step 2: Personal Information ──────────────────────────────────── */}
      <div className="donate-form-card">
        <p className="donate-step-label">Step 2 — Your information</p>

        <label htmlFor="donate-name">Full name</label>
        <input
          id="donate-name"
          type="text"
          autoComplete="name"
          value={name}
          onChange={e => { setName(e.target.value); setErrors(err => ({ ...err, name: '' })) }}
          aria-invalid={!!errors.name}
        />
        {errors.name && <p className="donate-field-error">{errors.name}</p>}

        <label htmlFor="donate-email">Email address</label>
        <input
          id="donate-email"
          type="email"
          autoComplete="email"
          value={email}
          onChange={e => { setEmail(e.target.value); setErrors(err => ({ ...err, email: '' })) }}
          aria-invalid={!!errors.email}
        />
        {errors.email && <p className="donate-field-error">{errors.email}</p>}
      </div>

      {/* ── Step 3: Billing Address ────────────────────────────────────────── */}
      <div className="donate-form-card">
        <p className="donate-step-label">Step 3 — Billing address</p>

        <label htmlFor="donate-address">Street address</label>
        <input
          id="donate-address"
          type="text"
          autoComplete="street-address"
          value={address}
          onChange={e => { setAddress(e.target.value); setErrors(err => ({ ...err, address: '' })) }}
          aria-invalid={!!errors.address}
        />
        {errors.address && <p className="donate-field-error">{errors.address}</p>}

        <label htmlFor="donate-city">City</label>
        <input
          id="donate-city"
          type="text"
          autoComplete="address-level2"
          value={city}
          onChange={e => { setCity(e.target.value); setErrors(err => ({ ...err, city: '' })) }}
          aria-invalid={!!errors.city}
        />
        {errors.city && <p className="donate-field-error">{errors.city}</p>}

        {/* State + ZIP side by side */}
        <div className="donate-field-row">
          <div style={{ flex: 1 }}>
            <label htmlFor="donate-state">State</label>
            <input
              id="donate-state"
              type="text"
              maxLength={2}
              autoComplete="address-level1"
              placeholder="CA"
              value={state}
              onChange={e => { setState(e.target.value.toUpperCase()); setErrors(err => ({ ...err, state: '' })) }}
              aria-invalid={!!errors.state}
            />
            {errors.state && <p className="donate-field-error">{errors.state}</p>}
          </div>

          <div style={{ flex: 1 }}>
            <label htmlFor="donate-zip">ZIP code</label>
            <input
              id="donate-zip"
              type="text"
              maxLength={10}
              autoComplete="postal-code"
              placeholder="90210"
              value={zip}
              onChange={e => { setZip(e.target.value); setErrors(err => ({ ...err, zip: '' })) }}
              aria-invalid={!!errors.zip}
            />
            {errors.zip && <p className="donate-field-error">{errors.zip}</p>}
          </div>
        </div>
      </div>

      {/* ── Step 4: Card Information ───────────────────────────────────────── */}
      <div className="donate-form-card">
        <p className="donate-step-label">Step 4 — Card information</p>
        <p className="caption" style={{ marginBottom: '1rem' }}>
          🔒 This is a simulated checkout. No real payment is processed.
        </p>

        <label htmlFor="donate-card-number">Card number</label>
        <input
          id="donate-card-number"
          type="text"
          inputMode="numeric"
          autoComplete="cc-number"
          placeholder="1234 5678 9012 3456"
          maxLength={19} // 16 digits + 3 spaces
          value={cardNumber}
          onChange={e => {
            // Auto-format: strip non-digits, re-insert spaces every 4 chars.
            setCardNumber(formatCardNumber(e.target.value))
            setErrors(err => ({ ...err, cardNumber: '' }))
          }}
          aria-invalid={!!errors.cardNumber}
        />
        {errors.cardNumber && <p className="donate-field-error">{errors.cardNumber}</p>}

        {/* Expiry + CVV side by side */}
        <div className="donate-field-row">
          <div style={{ flex: 1 }}>
            <label htmlFor="donate-expiry">Expiry (MM/YY)</label>
            <input
              id="donate-expiry"
              type="text"
              inputMode="numeric"
              autoComplete="cc-exp"
              placeholder="12/27"
              maxLength={5} // "MM/YY"
              value={expiry}
              onChange={e => {
                // Auto-format: strip non-digits, insert slash after 2 digits.
                setExpiry(formatExpiry(e.target.value))
                setErrors(err => ({ ...err, expiry: '' }))
              }}
              aria-invalid={!!errors.expiry}
            />
            {errors.expiry && <p className="donate-field-error">{errors.expiry}</p>}
          </div>

          <div style={{ flex: 1 }}>
            <label htmlFor="donate-cvv">CVV</label>
            <input
              id="donate-cvv"
              type="text"
              inputMode="numeric"
              autoComplete="cc-csc"
              placeholder="123"
              maxLength={4}
              value={cvv}
              onChange={e => {
                setCvv(e.target.value.replace(/\D/g, '').slice(0, 4))
                setErrors(err => ({ ...err, cvv: '' }))
              }}
              aria-invalid={!!errors.cvv}
            />
            {errors.cvv && <p className="donate-field-error">{errors.cvv}</p>}
          </div>
        </div>
      </div>

      {/* ── Submit ─────────────────────────────────────────────────────────── */}
      <div className="donate-submit-section">
        <button
          type="button"
          className="button donate-cta-button"
          onClick={handleSubmit}
          disabled={status === 'processing'}
          aria-busy={status === 'processing'}
        >
          {buttonLabel}
        </button>

        {apiError && (
          <p role="alert" style={{ color: 'var(--color-danger)', marginTop: '0.75rem' }}>
            {apiError}
          </p>
        )}
      </div>
    </div>
  )
}
