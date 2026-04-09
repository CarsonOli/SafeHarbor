import { useState } from 'react'
import './SocialMediaStrategy.css'
import { buildAuthHeaders } from '../services/authHeaders'

interface ScoreResult {
  conversion_likelihood: 'High' | 'Medium' | 'Low'
  conversion_probability: number
  recommendations: string[]
}

const OPTIONS = {
  platform:       ['Instagram', 'Facebook', 'Twitter', 'YouTube', 'WhatsApp', 'TikTok'],
  post_type:      ['ImpactStory', 'FundraisingAppeal', 'EducationalContent', 'EventPromotion', 'BehindTheScenes', 'ThankYou'],
  media_type:     ['Photo', 'Reel', 'Video', 'Text', 'Carousel', 'Story'],
  content_topic:  ['Reintegration', 'Education', 'Health', 'Safety', 'Fundraising', 'Awareness', 'Community'],
  sentiment_tone: ['Emotional', 'Urgent', 'Celebratory', 'Informative', 'Grateful', 'Hopeful'],
  cta_type:       ['DonateNow', 'LearnMore', 'ShareThis', 'VolunteerNow', 'SignUp'],
  days:           ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'],
}

const DAY_ABBREV: Record<string, string> = {
  Monday: 'Mon', Tuesday: 'Tue', Wednesday: 'Wed', Thursday: 'Thu',
  Friday: 'Fri', Saturday: 'Sat', Sunday: 'Sun',
}

function formatLabel(s: string) {
  return s.replace(/([A-Z])/g, ' $1').trim()
}

const defaultForm = {
  platform: 'Instagram',
  post_type: 'ImpactStory',
  media_type: 'Photo',
  content_topic: 'Reintegration',
  sentiment_tone: 'Emotional',
  features_resident_story: false,
  has_call_to_action: true,
  call_to_action_type: 'DonateNow',
  is_boosted: false,
  boost_budget_php: 0,
  post_hour: 10,
  day_of_week: 'Tuesday',
  num_hashtags: 3,
  caption_length: 150,
  mentions_count: 0,
}

// Separate raw string state so number inputs can be empty while the user types
type NumberField = 'post_hour' | 'num_hashtags' | 'caption_length' | 'mentions_count' | 'boost_budget_php'
const defaultRaw: Record<NumberField, string> = {
  post_hour: '10',
  num_hashtags: '3',
  caption_length: '150',
  mentions_count: '0',
  boost_budget_php: '0',
}

function PillGroup({
  name, options, value, onChange, abbrev, formatFn,
}: {
  name: string
  options: string[]
  value: string
  onChange: (name: string, val: string) => void
  abbrev?: Record<string, string>
  formatFn?: (s: string) => string
}) {
  return (
    <div className="sm-pill-group">
      {options.map(opt => (
        <button
          key={opt}
          type="button"
          className={`sm-pill ${value === opt ? 'sm-pill-active' : ''}`}
          onClick={() => onChange(name, opt)}
        >
          {abbrev ? abbrev[opt] : formatFn ? formatFn(opt) : opt}
        </button>
      ))}
    </div>
  )
}

export default function SocialMediaStrategy() {
  const [form, setForm] = useState(defaultForm)
  const [rawNumbers, setRawNumbers] = useState(defaultRaw)
  const [result, setResult] = useState<ScoreResult | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function handlePillChange(name: string, value: string) {
    setForm(prev => ({ ...prev, [name]: value }))
  }

  function handleNumberChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, value } = e.target
    setRawNumbers(prev => ({ ...prev, [name]: value }))
    if (value !== '') {
      setForm(prev => ({ ...prev, [name]: Number(value) }))
    }
  }

  function handleChange(e: React.ChangeEvent<HTMLInputElement>) {
    const { name, checked } = e.target
    setForm(prev => ({ ...prev, [name]: checked }))
  }

  async function handleSubmit(e: { preventDefault(): void }) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    setResult(null)

    try {
      const res = await fetch('/api/admin/social-media/score-post', {
        method: 'POST',
        headers: buildAuthHeaders({ 'Content-Type': 'application/json' }),
        body: JSON.stringify(form),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({}))
        throw new Error(err.error || `Server error ${res.status}`)
      }
      setResult(await res.json())
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Something went wrong')
    } finally {
      setLoading(false)
    }
  }

  const pct = result ? Math.round(result.conversion_probability * 100) : 0
  const badgeClass = result
    ? result.conversion_likelihood === 'High' ? 'badge-high'
    : result.conversion_likelihood === 'Medium' ? 'badge-medium'
    : 'badge-low'
    : ''

  return (
    <div className="sm-page">
      <div className="sm-header">
        <div>
          <h1>Social Media Strategy</h1>
          <p>Score your post before publishing — see how likely it is to drive donations.</p>
        </div>
        <div className="sm-model-badge">XGBoost · AUC 0.898 · 812 posts</div>
      </div>

      <div className="sm-layout">

        {/* ── FORM ─────────────────────────────── */}
        <form className="sm-card sm-form" onSubmit={handleSubmit}>

          <div className="sm-section">
            <span className="sm-section-label">Platform</span>
            <PillGroup name="platform" options={OPTIONS.platform} value={form.platform} onChange={handlePillChange} />
          </div>

          <div className="sm-section">
            <span className="sm-section-label">Post Type</span>
            <PillGroup name="post_type" options={OPTIONS.post_type} value={form.post_type} onChange={handlePillChange} formatFn={formatLabel} />
          </div>

          <div className="sm-two-col">
            <div className="sm-section">
              <span className="sm-section-label">Media Type</span>
              <PillGroup name="media_type" options={OPTIONS.media_type} value={form.media_type} onChange={handlePillChange} />
            </div>
            <div className="sm-section">
              <span className="sm-section-label">Sentiment</span>
              <PillGroup name="sentiment_tone" options={OPTIONS.sentiment_tone} value={form.sentiment_tone} onChange={handlePillChange} />
            </div>
          </div>

          <div className="sm-section">
            <span className="sm-section-label">Content Topic</span>
            <PillGroup name="content_topic" options={OPTIONS.content_topic} value={form.content_topic} onChange={handlePillChange} />
          </div>

          <div className="sm-section">
            <span className="sm-section-label">Day of Week</span>
            <PillGroup name="day_of_week" options={OPTIONS.days} value={form.day_of_week} onChange={handlePillChange} abbrev={DAY_ABBREV} />
          </div>

          <div className="sm-numbers-row">
            <div className="sm-number-field">
              <label>Hour (0–23)</label>
              <input type="number" name="post_hour" min={0} max={23} value={rawNumbers.post_hour} onChange={handleNumberChange} />
            </div>
            <div className="sm-number-field">
              <label>Hashtags</label>
              <input type="number" name="num_hashtags" min={0} max={30} value={rawNumbers.num_hashtags} onChange={handleNumberChange} />
            </div>
            <div className="sm-number-field">
              <label>Caption Length</label>
              <input type="number" name="caption_length" min={0} value={rawNumbers.caption_length} onChange={handleNumberChange} />
            </div>
            <div className="sm-number-field">
              <label>Mentions</label>
              <input type="number" name="mentions_count" min={0} value={rawNumbers.mentions_count} onChange={handleNumberChange} />
            </div>
          </div>

          <div className="sm-options">
            {([
              ['features_resident_story', 'Features a resident story', ''],
              ['has_call_to_action',      'Has call to action', ''],
              ['is_boosted',             'Boosted post', 'A paid promotion that extends the post\'s reach beyond your existing followers using platform ad targeting (e.g. Meta Ads). Set a budget below when enabled.'],
            ] as [string, string, string][]).map(([name, label, hint]) => (
              <label className="sm-toggle" key={name}>
                <input type="checkbox" name={name}
                  checked={(form as Record<string, unknown>)[name] as boolean}
                  onChange={handleChange} />
                <span className="sm-toggle-track" />
                <span className="sm-toggle-label-group">
                  {label}
                  {hint && <span className="sm-toggle-hint">{hint}</span>}
                </span>
              </label>
            ))}
          </div>

          {(form.has_call_to_action || form.is_boosted) && (
            <div className="sm-conditional">
              {form.has_call_to_action && (
                <div className="sm-section">
                  <span className="sm-section-label">Call to Action</span>
                  <PillGroup name="call_to_action_type" options={OPTIONS.cta_type} value={form.call_to_action_type} onChange={handlePillChange} formatFn={formatLabel} />
                </div>
              )}
              {form.is_boosted && (
                <div className="sm-number-field">
                  <label>Boost Budget (PHP)</label>
                  <input type="number" name="boost_budget_php" min={0} value={rawNumbers.boost_budget_php} onChange={handleNumberChange} />
                </div>
              )}
            </div>
          )}

          <button type="submit" className="sm-submit" disabled={loading}>
            {loading ? <span className="sm-spinner" /> : null}
            {loading ? 'Analyzing...' : 'Score This Post'}
          </button>

          {error && <p className="sm-error">{error}</p>}
        </form>

        {/* ── RESULTS ──────────────────────────── */}
        <div className="sm-results">

          <div className="sm-card sm-score-card">
            {!result && !loading && (
              <div className="sm-empty">
                <div className="sm-empty-icon">📊</div>
                <p>Fill in post details and click <strong>Score This Post</strong> to get your conversion prediction.</p>
              </div>
            )}

            {loading && (
              <div className="sm-empty">
                <div className="sm-loading-ring" />
                <p>Analyzing your post...</p>
              </div>
            )}

            {result && !loading && (
              <>
                <div className="sm-score-header">
                  <span>Conversion Prediction</span>
                  <span className={`sm-badge ${badgeClass}`}>{result.conversion_likelihood}</span>
                </div>

                <div className="sm-prob-section">
                  <div className="sm-prob-labels">
                    <span>Probability</span>
                    <strong>{pct}%</strong>
                  </div>
                  <div className="sm-bar-bg">
                    <div className={`sm-bar-fill ${badgeClass}`} style={{ width: `${pct}%` }} />
                  </div>
                </div>

                {result.recommendations.length > 0 ? (
                  <div className="sm-recs">
                    <h3>Recommendations</h3>
                    <ul>
                      {result.recommendations.map((r, i) => (
                        <li key={i}><span className="sm-rec-dot" />{r}</li>
                      ))}
                    </ul>
                  </div>
                ) : (
                  <div className="sm-all-good">
                    ✓ This post looks well-optimized for donation conversions!
                  </div>
                )}
              </>
            )}
          </div>

          <div className="sm-card sm-insights">
            <h3>What the model learned</h3>
            <ul>
              {[
                { dot: 'high',   text: 'Resident stories are the single strongest driver of referrals' },
                { dot: 'high',   text: 'Impact Story & Fundraising Appeal posts outperform all other post types' },
                { dot: 'medium', text: 'Emotional and Urgent sentiment outperforms Informative by approximately 2×' },
                { dot: 'medium', text: 'Tuesday–Thursday mornings (9–11 AM) are peak conversion windows' },
                { dot: 'low',    text: 'Posts with a clear Call to Action consistently convert better' },
              ].map((item, i) => (
                <li key={i}>
                  <span className={`sm-dot ${item.dot}`} />
                  {item.text}
                </li>
              ))}
            </ul>
          </div>

        </div>
      </div>
    </div>
  )
}
