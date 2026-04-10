import { useEffect, useMemo, useState } from 'react'
import { useAuth } from '../../auth/AuthContext'
import { createProcessRecording, fetchProcessRecordings, fetchResidentCases } from '../../services/adminOperationsApi'
import type { ProcessRecordItem, ResidentCaseListItem } from '../../types/adminOperations'

const EMOTIONAL_STATES = ['Angry', 'Distressed', 'Anxious', 'Hopeful', 'Happy', 'Calm', 'Sad', 'Withdrawn']
const SESSION_TYPES = ['Individual', 'Group']

const INTERVENTION_OPTIONS = [
  'Active Listening', 'Safety Planning', 'Psychoeducation', 'Crisis Support',
  'Emotional Regulation Coaching', 'Coping Skills Practice', 'Trauma-Informed Counseling',
  'Group Discussion', 'Referral Coordination', 'Family Support Discussion',
  'Legal Service Coordination', 'Case Management', 'Goal Setting',
  'Caring', 'Healing', 'Teaching',
]

const FOLLOWUP_OPTIONS = [
  'Schedule next session', 'Monitor emotional state', 'Follow up with resident',
  'Contact guardian/family', 'Coordinate with shelter staff', 'Refer to specialist',
  'Legal follow-up', 'Education support follow-up', 'Medical follow-up',
  'Safety check', 'No follow-up required',
]

function MultiSelectCheckboxes({
  options, selected, onChange, otherId,
}: {
  options: string[]
  selected: string[]
  onChange: (next: string[]) => void
  otherId: string
}) {
  const [otherText, setOtherText] = useState('')

  function toggle(option: string) {
    onChange(selected.includes(option) ? selected.filter((s) => s !== option) : [...selected, option])
  }

  function commitOther() {
    const trimmed = otherText.trim()
    if (trimmed && !selected.includes(trimmed)) {
      onChange([...selected, trimmed])
    }
    setOtherText('')
  }

  return (
    <div style={{ border: '1px solid var(--color-border, #e2e8f0)', borderRadius: '6px', padding: '0.75rem', background: '#fafafa' }}>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))', gap: '0.35rem 1rem' }}>
        {options.map((opt) => (
          <label key={opt} style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', cursor: 'pointer', fontSize: '0.9rem' }}>
            <input type="checkbox" checked={selected.includes(opt)} onChange={() => toggle(opt)} />
            {opt}
          </label>
        ))}
      </div>
      <div style={{ marginTop: '0.6rem', display: 'flex', gap: '0.5rem' }}>
        <input
          id={otherId}
          placeholder="Other (type and press Add)"
          value={otherText}
          onChange={(e) => setOtherText(e.target.value)}
          onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); commitOther() } }}
          style={{ flex: 1, fontSize: '0.9rem' }}
        />
        <button type="button" onClick={commitOther} style={{ whiteSpace: 'nowrap', fontSize: '0.85rem' }}>
          Add
        </button>
      </div>
      {selected.filter((s) => !options.includes(s)).length > 0 && (
        <div style={{ marginTop: '0.4rem', fontSize: '0.8rem', color: 'var(--color-primary)' }}>
          Custom: {selected.filter((s) => !options.includes(s)).map((s) => (
            <span key={s} style={{ marginRight: '0.5rem' }}>
              {s} <button type="button" onClick={() => onChange(selected.filter((x) => x !== s))} style={{ background: 'none', border: 'none', cursor: 'pointer', color: '#c0392b', fontSize: '0.8rem' }}>✕</button>
            </span>
          ))}
        </div>
      )}
    </div>
  )
}

function Badge({ label, color }: { label: string; color: string }) {
  return (
    <span style={{
      display: 'inline-block', padding: '0.15rem 0.6rem', borderRadius: '999px',
      fontSize: '0.75rem', fontWeight: 600, background: color, color: '#fff', marginRight: '0.35rem',
    }}>
      {label}
    </span>
  )
}

function SessionCard({ item, expanded, onToggle }: { item: ProcessRecordItem; expanded: boolean; onToggle: () => void }) {
  const date = new Date(item.recordedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })

  return (
    <article className="feature-card" style={{ marginBottom: '0.75rem', padding: '1rem 1.25rem' }}>
      <button type="button" onClick={onToggle} style={{ width: '100%', background: 'none', border: 'none', cursor: 'pointer', textAlign: 'left', padding: 0 }} aria-expanded={expanded}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.5rem' }}>
          <div>
            <strong style={{ color: 'var(--color-text)' }}>{date}</strong>
            <span style={{ color: '#666', marginLeft: '0.75rem', fontSize: '0.9rem' }}>
              {item.socialWorker} · {item.sessionType}
              {item.sessionDurationMinutes ? ` · ${item.sessionDurationMinutes} min` : ''}
            </span>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '0.25rem', flexWrap: 'wrap' }}>
            {item.progressNoted && <Badge label="Progress Observed" color="var(--color-primary)" />}
            {item.concernsFlagged && <Badge label="Concern Flagged" color="#c0392b" />}
            {item.referralMade && <Badge label="Referral Initiated" color="var(--color-accent)" />}
            {item.notesRestricted && <Badge label="Restricted" color="#7f8c8d" />}
            <span style={{ fontSize: '0.85rem', color: 'var(--color-primary)', marginLeft: '0.5rem' }}>{expanded ? '▲' : '▼'}</span>
          </div>
        </div>
        {item.emotionalStateObserved && (
          <div style={{ marginTop: '0.4rem', fontSize: '0.88rem', color: '#555' }}>
            <span style={{ fontWeight: 500 }}>{item.emotionalStateObserved}</span>
            {item.emotionalStateEnd && (
              <><span style={{ margin: '0 0.4rem', color: 'var(--color-primary)' }}>→</span><span style={{ fontWeight: 500 }}>{item.emotionalStateEnd}</span></>
            )}
          </div>
        )}
      </button>

      {expanded && (
        <div style={{ marginTop: '1rem', borderTop: '1px solid var(--color-border, #e2e8f0)', paddingTop: '1rem' }}>
          {item.notesRestricted ? (
            <p style={{ color: '#7f8c8d', fontStyle: 'italic' }}>This session note is restricted. Contact an administrator to view the content.</p>
          ) : (
            <p style={{ whiteSpace: 'pre-wrap', marginBottom: item.interventionsApplied || item.followUpActions ? '0.75rem' : 0 }}>{item.summary}</p>
          )}
          {item.interventionsApplied && (
            <p style={{ fontSize: '0.9rem', marginBottom: '0.5rem' }}><strong>Interventions:</strong> {item.interventionsApplied}</p>
          )}
          {item.followUpActions && (
            <div style={{ background: 'rgba(42, 92, 92, 0.07)', border: '1px solid rgba(42, 92, 92, 0.2)', borderRadius: '6px', padding: '0.6rem 0.9rem', fontSize: '0.9rem', marginTop: '0.5rem' }}>
              <strong style={{ color: 'var(--color-primary)' }}>Follow-up:</strong> {item.followUpActions}
            </div>
          )}
        </div>
      )}
    </article>
  )
}

export function ProcessRecordingPage() {
  const { session } = useAuth()

  const emptyForm = useMemo(() => ({
    residentCaseId: '',
    residentSearch: '',
    sessionDate: new Date().toISOString().slice(0, 10),
    socialWorker: session?.email ?? '',
    sessionType: 'Individual',
    sessionDurationMinutes: '',
    emotionalStateObserved: '',
    emotionalStateEnd: '',
    summary: '',
    interventions: [] as string[],
    followUps: [] as string[],
    progressNoted: false,
    concernsFlagged: false,
    referralMade: false,
    notesRestricted: '',
  }), [session?.email])

  const [form, setForm] = useState(emptyForm)
  const [residentCases, setResidentCases] = useState<ResidentCaseListItem[]>([])
  const [items, setItems] = useState<ProcessRecordItem[]>([])
  const [filterResidentCaseId, setFilterResidentCaseId] = useState('')
  const [filterSessionType, setFilterSessionType] = useState('')
  const [desc, setDesc] = useState(true)
  const [page, setPage] = useState(1)
  const [pageSize] = useState(10)
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState<string | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [expandedId, setExpandedId] = useState<string | null>(null)

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize))

  // Load resident cases for the dropdown
  useEffect(() => {
    fetchResidentCases({ page: 1, pageSize: 200 })
      .then((data) => setResidentCases(data.items))
      .catch(() => { /* non-fatal */ })
  }, [])

  const query = useMemo(
    () => ({ page, pageSize, residentCaseId: filterResidentCaseId || undefined, desc }),
    [page, pageSize, filterResidentCaseId, desc],
  )

  useEffect(() => {
    let cancelled = false
    async function load() {
      setLoading(true)
      try {
        const data = await fetchProcessRecordings(query)
        if (cancelled) return
        setItems(data.items)
        setTotalCount(data.totalCount)
        setError(null)
      } catch (err) {
        if (cancelled) return
        setError(err instanceof Error ? err.message : 'Failed to load recordings')
      } finally {
        if (!cancelled) setLoading(false)
      }
    }
    void load()
    return () => { cancelled = true }
  }, [query])

  const filteredResidents = form.residentSearch.trim()
    ? residentCases.filter((c) =>
        (c.residentName ?? '').toLowerCase().includes(form.residentSearch.toLowerCase()) ||
        c.safehouse.toLowerCase().includes(form.residentSearch.toLowerCase())
      )
    : residentCases

  const visibleItems = filterSessionType ? items.filter((i) => i.sessionType === filterSessionType) : items

  function setField<K extends keyof typeof emptyForm>(key: K, value: (typeof emptyForm)[K]) {
    setForm((prev) => ({ ...prev, [key]: value }))
  }

  function openForm() {
    setForm({ ...emptyForm, socialWorker: session?.email ?? '' })
    setShowForm(true)
    setError(null)
    setSuccess(null)
  }

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault()
    if (!form.residentCaseId) { setError('Please select a resident.'); return }
    if (!form.emotionalStateObserved) { setError('Emotional state at start is required.'); return }
    if (form.summary.trim().length < 3) { setError('Session narrative must be at least 3 characters.'); return }

    setSubmitting(true); setError(null); setSuccess(null)

    try {
      await createProcessRecording({
        residentCaseId: form.residentCaseId,
        socialWorker: form.socialWorker.trim(),
        sessionType: form.sessionType,
        sessionDurationMinutes: form.sessionDurationMinutes ? Number(form.sessionDurationMinutes) : null,
        emotionalStateObserved: form.emotionalStateObserved,
        emotionalStateEnd: form.emotionalStateEnd,
        summary: form.summary.trim(),
        interventionsApplied: form.interventions.join(', '),
        followUpActions: form.followUps.join(', '),
        progressNoted: form.progressNoted,
        concernsFlagged: form.concernsFlagged,
        referralMade: form.referralMade,
        notesRestricted: form.notesRestricted.trim(),
        recordedAt: form.sessionDate ? new Date(form.sessionDate).toISOString() : undefined,
      })
      setShowForm(false)
      setSuccess('Session note saved.')
      setPage(1)
      const data = await fetchProcessRecordings({ page: 1, pageSize, residentCaseId: filterResidentCaseId || undefined, desc })
      setItems(data.items)
      setTotalCount(data.totalCount)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to save recording')
    } finally {
      setSubmitting(false)
    }
  }

  const sectionLabel = (text: string) => (
    <p style={{ fontWeight: 600, color: 'var(--color-primary)', fontSize: '0.8rem', textTransform: 'uppercase', letterSpacing: '0.08em', marginBottom: '0.75rem', marginTop: '1.25rem' }}>
      {text}
    </p>
  )

  return (
    <section>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem', marginBottom: '1.5rem' }}>
        <div>
          <h1 style={{ marginBottom: '0.25rem' }}>Counseling Logs</h1>
          <p className="lead" style={{ margin: 0 }}>Process recordings — documented counseling sessions for each resident.</p>
        </div>
        <button type="button" className="button" onClick={showForm ? () => setShowForm(false) : openForm}>
          {showForm ? 'Cancel' : '+ Log New Session'}
        </button>
      </div>

      {success && <p role="status" style={{ color: 'var(--color-primary)', marginBottom: '1rem' }}>{success}</p>}
      {error && <p role="alert" style={{ color: '#c0392b', marginBottom: '1rem' }}>{error}</p>}

      {/* ── NEW SESSION FORM ── */}
      {showForm && (
        <form className="feature-card" onSubmit={handleCreate} style={{ marginBottom: '2rem' }}>
          <h2 style={{ marginBottom: '0' }}>New Session Note</h2>

          {sectionLabel('Session Details')}
          <div style={{ marginBottom: '0.75rem' }}>
            <label htmlFor="pr-resident-search">Resident</label>
            <input
              id="pr-resident-search"
              placeholder="Type to search by name or safehouse…"
              value={form.residentSearch}
              onChange={(e) => setField('residentSearch', e.target.value)}
              style={{ marginBottom: '0.4rem' }}
            />
            <select
              id="pr-resident-select"
              value={form.residentCaseId}
              onChange={(e) => setField('residentCaseId', e.target.value)}
              required
              size={Math.min(6, filteredResidents.length + 1)}
              style={{ width: '100%' }}
            >
              <option value="">— Select a resident —</option>
              {filteredResidents.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.residentName ?? 'Unknown Resident'} — {c.safehouse} ({c.status})
                </option>
              ))}
            </select>
            {form.residentCaseId && (
              <p style={{ fontSize: '0.8rem', color: '#666', marginTop: '0.25rem' }}>
                Case ID: {form.residentCaseId}
              </p>
            )}
          </div>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '0.75rem' }}>
            <div>
              <label htmlFor="pr-date">Session Date *</label>
              <input id="pr-date" type="date" value={form.sessionDate} onChange={(e) => setField('sessionDate', e.target.value)} required />
            </div>
            <div>
              <label htmlFor="pr-sw">Social Worker</label>
              <input id="pr-sw" value={form.socialWorker} onChange={(e) => setField('socialWorker', e.target.value)} placeholder="Auto-filled from login" />
            </div>
            <div>
              <label htmlFor="pr-type">Session Type</label>
              <select id="pr-type" value={form.sessionType} onChange={(e) => setField('sessionType', e.target.value)}>
                {SESSION_TYPES.map((t) => <option key={t}>{t}</option>)}
              </select>
            </div>
            <div>
              <label htmlFor="pr-duration">Duration (minutes)</label>
              <input id="pr-duration" type="number" min={5} step={5} max={480} placeholder="e.g. 60" value={form.sessionDurationMinutes} onChange={(e) => setField('sessionDurationMinutes', e.target.value)} />
            </div>
          </div>

          {sectionLabel('Emotional Snapshot')}
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '0.75rem' }}>
            <div>
              <label htmlFor="pr-emo-start">Emotional State at Start *</label>
              <select id="pr-emo-start" value={form.emotionalStateObserved} onChange={(e) => setField('emotionalStateObserved', e.target.value)} required>
                <option value="">Select…</option>
                {EMOTIONAL_STATES.map((s) => <option key={s}>{s}</option>)}
              </select>
            </div>
            <div>
              <label htmlFor="pr-emo-end">Emotional State at End</label>
              <select id="pr-emo-end" value={form.emotionalStateEnd} onChange={(e) => setField('emotionalStateEnd', e.target.value)}>
                <option value="">Select…</option>
                {EMOTIONAL_STATES.map((s) => <option key={s}>{s}</option>)}
              </select>
            </div>
          </div>

          {sectionLabel('Session Documentation')}
          <div>
            <label htmlFor="pr-narrative">Session Narrative * <span style={{ fontWeight: 400, color: '#666', fontSize: '0.85rem' }}>— Summarize the main concerns discussed, key observations, and resident response.</span></label>
            <textarea id="pr-narrative" rows={5} value={form.summary} onChange={(e) => setField('summary', e.target.value)} required minLength={3} style={{ width: '100%', resize: 'vertical' }} />
          </div>

          <div style={{ marginTop: '0.75rem' }}>
            <label>Interventions Applied</label>
            <MultiSelectCheckboxes options={INTERVENTION_OPTIONS} selected={form.interventions} onChange={(v) => setField('interventions', v)} otherId="pr-intervention-other" />
          </div>

          <div style={{ marginTop: '0.75rem' }}>
            <label>Follow-Up Actions</label>
            <MultiSelectCheckboxes options={FOLLOWUP_OPTIONS} selected={form.followUps} onChange={(v) => setField('followUps', v)} otherId="pr-followup-other" />
          </div>

          {sectionLabel('Clinical Flags')}
          <div style={{ display: 'flex', gap: '1.5rem', flexWrap: 'wrap', marginBottom: '1rem' }}>
            {([
              ['progressNoted', 'Progress Observed'],
              ['concernsFlagged', 'Concern Flagged'],
              ['referralMade', 'Referral Initiated'],
            ] as const).map(([key, label]) => (
              <label key={key} style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', cursor: 'pointer' }}>
                <input type="checkbox" checked={form[key] as boolean} onChange={(e) => setField(key, e.target.checked)} />
                {label}
              </label>
            ))}
          </div>

          <div style={{ background: 'rgba(220, 80, 80, 0.05)', border: '1px solid rgba(220, 80, 80, 0.2)', borderRadius: '6px', padding: '0.75rem 1rem', marginBottom: '1.25rem' }}>
            <label htmlFor="pr-restricted" style={{ display: 'flex', alignItems: 'center', gap: '0.4rem' }}>
              🔒 Restricted Notes <span style={{ fontWeight: 400, color: '#666', fontSize: '0.85rem' }}>— Only visible to authorized staff. Leave blank if not restricted.</span>
            </label>
            <textarea id="pr-restricted" rows={3} value={form.notesRestricted} onChange={(e) => setField('notesRestricted', e.target.value)} placeholder="If this session contains sensitive information requiring restricted access, enter it here…" style={{ width: '100%', resize: 'vertical', marginTop: '0.4rem' }} />
          </div>

          <button className="button" type="submit" disabled={submitting}>
            {submitting ? 'Saving…' : 'Save Session Note'}
          </button>
        </form>
      )}

      {/* ── SESSION HISTORY ── */}
      <div className="feature-card">
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.75rem', marginBottom: '1rem' }}>
          <h2 style={{ margin: 0 }}>Session History</h2>
          <div style={{ display: 'flex', gap: '0.5rem', flexWrap: 'wrap', alignItems: 'center' }}>
            <input placeholder="Filter by case ID" value={filterResidentCaseId} onChange={(e) => { setFilterResidentCaseId(e.target.value); setPage(1) }} style={{ width: '180px' }} />
            <select value={filterSessionType} onChange={(e) => setFilterSessionType(e.target.value)} style={{ width: '140px' }}>
              <option value="">All Types</option>
              {SESSION_TYPES.map((t) => <option key={t}>{t}</option>)}
            </select>
            <select value={desc ? 'desc' : 'asc'} onChange={(e) => { setDesc(e.target.value === 'desc'); setPage(1) }} style={{ width: '150px' }}>
              <option value="desc">Newest First</option>
              <option value="asc">Oldest First</option>
            </select>
            {(filterResidentCaseId || filterSessionType) && (
              <button type="button" onClick={() => { setFilterResidentCaseId(''); setFilterSessionType(''); setPage(1) }}>Clear Filters</button>
            )}
          </div>
        </div>

        {loading && <p role="status">Loading sessions…</p>}

        {!loading && !error && (
          <>
            {visibleItems.length === 0 ? (
              <p style={{ color: '#666' }}>No session notes found.</p>
            ) : (
              visibleItems.map((item) => (
                <SessionCard key={item.id} item={item} expanded={expandedId === item.id} onToggle={() => setExpandedId(expandedId === item.id ? null : item.id)} />
              ))
            )}
            {totalPages > 1 && (
              <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', marginTop: '1rem' }}>
                <button disabled={page <= 1} onClick={() => setPage((v) => v - 1)}>Previous</button>
                <span>Page {page} of {totalPages}</span>
                <button disabled={page >= totalPages} onClick={() => setPage((v) => v + 1)}>Next</button>
              </div>
            )}
          </>
        )}
      </div>
    </section>
  )
}
