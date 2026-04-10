import { useState } from 'react'
import { scorePost } from '../../services/socialMediaApi'
import { toUserFacingError } from '../../services/httpErrors'
import { ApiErrorNotice } from '../../components/ApiErrorNotice'
import type { PostScoreRequest, PostScoreResponse } from '../../services/socialMediaApi'

// ── Option helpers ────────────────────────────────────────────────────────────
// Each entry is [displayLabel, modelValue]. Display labels are human-readable;
// modelValue must match the training data column names exactly.

const PLATFORMS = [
  ['Facebook', 'Facebook'],
  ['Instagram', 'Instagram'],
  ['Twitter / X', 'Twitter'],
  ['TikTok', 'TikTok'],
  ['YouTube', 'YouTube'],
] as const

const POST_TYPES = [
  ['Impact Story', 'ImpactStory'],
  ['Fundraising Appeal', 'FundraisingAppeal'],
  ['Awareness Campaign', 'Awareness'],
  ['Event Promotion', 'EventPromo'],
  ['General Update', 'Update'],
] as const

const MEDIA_TYPES = [
  ['Photo / Image', 'Photo'],
  ['Video', 'Video'],
  ['Reel / Short-form video', 'Reel'],
  ['Carousel (multiple images)', 'Carousel'],
  ['Text only', 'Text'],
] as const

const CONTENT_TOPICS = [
  ['Resident story', 'ResidentStory'],
  ['Donor spotlight', 'DonorSpotlight'],
  ['Program update', 'ProgramUpdate'],
  ['Event announcement', 'EventAnnouncement'],
  ['Statistics / Data', 'Statistics'],
] as const

const TONES = [
  ['Emotional', 'Emotional'],
  ['Urgent', 'Urgent'],
  ['Celebratory', 'Celebratory'],
  ['Informative', 'Informative'],
  ['Grateful / Thank-you', 'Grateful'],
] as const

const CTA_TYPES = [
  ['Donate now', 'Donate'],
  ['Volunteer', 'Volunteer'],
  ['Share this post', 'Share'],
  ['Learn more', 'LearnMore'],
  ['No call to action', 'None'],
] as const

const DAYS = [
  'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday',
] as const

// ── Result colours ────────────────────────────────────────────────────────────

const LIKELIHOOD_STYLES: Record<string, { background: string; color: string }> = {
  High:   { background: '#1D9E75', color: '#fff' },
  Medium: { background: '#F5A623', color: '#fff' },
  Low:    { background: '#E24B4A', color: '#fff' },
}

// ── Defaults ──────────────────────────────────────────────────────────────────

const DEFAULT_FORM: PostScoreRequest = {
  platform:              'Facebook',
  postType:              'ImpactStory',
  mediaType:             'Photo',
  contentTopic:          'ResidentStory',
  sentimentTone:         'Emotional',
  featuresResidentStory: false,
  hasCallToAction:       true,
  callToActionType:      'Donate',
  isBoosted:             false,
  boostBudgetPhp:        0,
  postHour:              10,
  dayOfWeek:             'Tuesday',
  numHashtags:           3,
  captionLength:         150,
  mentionsCount:         1,
}

// ── Small layout helpers ──────────────────────────────────────────────────────

function FieldGroup({ label, hint, children }: { label: string; hint?: string; children: React.ReactNode }) {
  return (
    <label style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
      <span style={{ fontWeight: 600, fontSize: '0.85rem', color: 'var(--color-text)' }}>{label}</span>
      {hint && <span style={{ fontSize: '0.75rem', color: '#64748b' }}>{hint}</span>}
      {children}
    </label>
  )
}

function SectionHeading({ children }: { children: React.ReactNode }) {
  return (
    <div style={{ gridColumn: '1 / -1', borderBottom: '1px solid #e2e8f0', paddingBottom: '4px', marginTop: '0.5rem' }}>
      <span style={{ fontSize: '0.72rem', fontWeight: 700, textTransform: 'uppercase', letterSpacing: '0.08em', color: '#94a3b8' }}>
        {children}
      </span>
    </div>
  )
}

// ── Page ──────────────────────────────────────────────────────────────────────

export function SocialMediaScorerPage() {
  const [form, setForm] = useState<PostScoreRequest>(DEFAULT_FORM)
  const [result, setResult] = useState<PostScoreResponse | null>(null)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  function setField<K extends keyof PostScoreRequest>(key: K, value: PostScoreRequest[K]) {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError(null)
    setResult(null)
    try {
      const res = await scorePost(form)
      setResult(res)
    } catch (err) {
      setError(toUserFacingError(err, 'Unable to score post right now.'))
    } finally {
      setLoading(false)
    }
  }

  function handleReset() {
    setResult(null)
    setError(null)
    setForm(DEFAULT_FORM)
  }

  // ── Result view ──
  if (result) {
    const liStyle = LIKELIHOOD_STYLES[result.conversionLikelihood] ?? { background: '#999', color: '#fff' }
    return (
      <section>
        <p className="eyebrow">ML Insights</p>
        <h1>Social Media Post Scorer</h1>

        <div className="metric-card" style={{ maxWidth: '640px' }}>
          <p className="eyebrow">Conversion Prediction</p>

          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem', flexWrap: 'wrap', marginBottom: '1.25rem' }}>
            <span style={{ ...liStyle, borderRadius: '6px', padding: '6px 20px', fontSize: '1rem', fontWeight: 700 }}>
              {result.conversionLikelihood} likelihood
            </span>
            <span style={{ fontSize: '1.5rem', fontWeight: 700 }}>
              {(result.probability * 100).toFixed(1)}%
            </span>
            <span style={{ fontSize: '0.9rem', color: '#64748b' }}>estimated chance of driving a donation referral</span>
          </div>

          <p className="eyebrow" style={{ marginBottom: '0.5rem' }}>Recommendations to improve conversion</p>
          <ul style={{ paddingLeft: '1.25rem', lineHeight: 1.8, margin: 0 }}>
            {result.recommendations.map((rec, i) => <li key={i}>{rec}</li>)}
          </ul>

          <button className="button button-primary" onClick={handleReset} style={{ marginTop: '1.5rem' }}>
            ← Score another post
          </button>
        </div>
      </section>
    )
  }

  // ── Form view ──
  return (
    <section>
      <p className="eyebrow">ML Insights</p>
      <h1>Social Media Post Scorer</h1>
      <p className="lead">
        Fill in the details of a planned post to predict how likely it is to generate a donation.
        Our XGBoost model (AUC 0.898) was trained on 812 SafeHarbor posts.
      </p>

      {error && <ApiErrorNotice error={error} />}

      <form className="chart-card" style={{ maxWidth: '700px' }} onSubmit={handleSubmit}>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0.85rem 1.5rem' }}>

          <SectionHeading>Content</SectionHeading>

          <FieldGroup label="Platform">
            <select value={form.platform} onChange={(e) => setField('platform', e.target.value)}>
              {PLATFORMS.map(([label, val]) => <option key={val} value={val}>{label}</option>)}
            </select>
          </FieldGroup>

          <FieldGroup label="Post type">
            <select value={form.postType} onChange={(e) => setField('postType', e.target.value)}>
              {POST_TYPES.map(([label, val]) => <option key={val} value={val}>{label}</option>)}
            </select>
          </FieldGroup>

          <FieldGroup label="Media format">
            <select value={form.mediaType} onChange={(e) => setField('mediaType', e.target.value)}>
              {MEDIA_TYPES.map(([label, val]) => <option key={val} value={val}>{label}</option>)}
            </select>
          </FieldGroup>

          <FieldGroup label="Content topic">
            <select value={form.contentTopic} onChange={(e) => setField('contentTopic', e.target.value)}>
              {CONTENT_TOPICS.map(([label, val]) => <option key={val} value={val}>{label}</option>)}
            </select>
          </FieldGroup>

          <FieldGroup label="Tone / sentiment">
            <select value={form.sentimentTone} onChange={(e) => setField('sentimentTone', e.target.value)}>
              {TONES.map(([label, val]) => <option key={val} value={val}>{label}</option>)}
            </select>
          </FieldGroup>

          <FieldGroup label="Call to action">
            <select value={form.callToActionType} onChange={(e) => setField('callToActionType', e.target.value)}>
              {CTA_TYPES.map(([label, val]) => <option key={val} value={val}>{label}</option>)}
            </select>
          </FieldGroup>

          <SectionHeading>Timing</SectionHeading>

          <FieldGroup label="Day of week">
            <select value={form.dayOfWeek} onChange={(e) => setField('dayOfWeek', e.target.value)}>
              {DAYS.map((d) => <option key={d}>{d}</option>)}
            </select>
          </FieldGroup>

          <FieldGroup label="Hour of day" hint="0 = midnight · 10 = 10 am · 20 = 8 pm">
            <input
              type="number" min={0} max={23}
              value={form.postHour}
              onChange={(e) => setField('postHour', Number(e.target.value))}
            />
          </FieldGroup>

          <SectionHeading>Engagement details</SectionHeading>

          <FieldGroup label="Number of hashtags">
            <input
              type="number" min={0} max={30}
              value={form.numHashtags}
              onChange={(e) => setField('numHashtags', Number(e.target.value))}
            />
          </FieldGroup>

          <FieldGroup label="Caption length" hint="Total characters in the caption">
            <input
              type="number" min={0}
              value={form.captionLength}
              onChange={(e) => setField('captionLength', Number(e.target.value))}
            />
          </FieldGroup>

          <FieldGroup label="Mentions / tags" hint="@ accounts tagged in the post">
            <input
              type="number" min={0}
              value={form.mentionsCount}
              onChange={(e) => setField('mentionsCount', Number(e.target.value))}
            />
          </FieldGroup>

          <FieldGroup label="Boost budget (₱)" hint="0 if not boosted">
            <input
              type="number" min={0} step={100}
              value={form.boostBudgetPhp}
              onChange={(e) => setField('boostBudgetPhp', Number(e.target.value))}
            />
          </FieldGroup>

          <SectionHeading>Options</SectionHeading>

          <label style={{ display: 'flex', alignItems: 'center', gap: '0.6rem', cursor: 'pointer' }}>
            <input
              type="checkbox"
              checked={form.featuresResidentStory}
              onChange={(e) => setField('featuresResidentStory', e.target.checked)}
            />
            <span style={{ fontSize: '0.9rem' }}>Post features a resident story</span>
          </label>

          <label style={{ display: 'flex', alignItems: 'center', gap: '0.6rem', cursor: 'pointer' }}>
            <input
              type="checkbox"
              checked={form.hasCallToAction}
              onChange={(e) => setField('hasCallToAction', e.target.checked)}
            />
            <span style={{ fontSize: '0.9rem' }}>Post has a call to action</span>
          </label>

          <label style={{ display: 'flex', alignItems: 'center', gap: '0.6rem', cursor: 'pointer' }}>
            <input
              type="checkbox"
              checked={form.isBoosted}
              onChange={(e) => setField('isBoosted', e.target.checked)}
            />
            <span style={{ fontSize: '0.9rem' }}>Post will be boosted / paid promotion</span>
          </label>

        </div>

        <button
          className="button button-primary"
          type="submit"
          disabled={loading}
          style={{ marginTop: '1.5rem' }}
        >
          {loading ? 'Scoring…' : 'Score this post →'}
        </button>
      </form>
    </section>
  )
}
