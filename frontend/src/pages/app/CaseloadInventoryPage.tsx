import React, { useEffect, useRef, useState } from 'react'
import { 
  fetchCaseloadLookups, 
  fetchResidentCases, 
  deleteResidentCase,
  createResident, 
  updateResident 
} from '../../services/adminOperationsApi'
// This was the missing link causing the red line in your useEffect!
import { fetchResidentReadinessFlags } from '../../services/mlInsightsApi' 
import { toUserFacingError } from '../../services/httpErrors'
import { ReadinessBadge } from '../../components/ReadinessBadge'
import { useAuth } from '../../auth/AuthContext'
import type { CaseloadLookupsResponse, ResidentCaseListItem } from '../../types/adminOperations'
import type { ResidentReadinessFlag } from '../../services/mlInsightsApi'

// ... (Rest of your STATUS_COLORS and StatusBadge component stays the same)

// ─── Status badge ────────────────────────────────────────────────────────────

const STATUS_COLORS: Record<string, { bg: string; color: string }> = {
  Active:   { bg: '#dcfce7', color: '#166534' },
  Pending:  { bg: '#fef9c3', color: '#854d0e' },
  Closed:   { bg: '#f1f5f9', color: '#475569' },
  'At Risk': { bg: '#fee2e2', color: '#991b1b' },
}

function StatusBadge({ status }: { status: string }) {
  const style = STATUS_COLORS[status] ?? { bg: '#e2e8f0', color: '#334155' }
  return (
    <span style={{
      display: 'inline-block',
      padding: '2px 10px',
      borderRadius: '999px',
      fontSize: '0.78rem',
      fontWeight: 600,
      background: style.bg,
      color: style.color,
    }}>
      {status}
    </span>
  )
}

// ─── Expanded detail row ──────────────────────────────────────────────────────

function CaseDetail({
  item,
  onDelete,
  isDeleting,
  hasReadiness,
}: {
  item: ResidentCaseListItem
  onDelete?: () => void
  isDeleting?: boolean
  hasReadiness: boolean
}) {
  const shortId = item.id.split('-')[0].toUpperCase()
  return (
    <tr>
      <td colSpan={hasReadiness ? 8 : 7} style={{ padding: '0 1rem 0.75rem 1rem', background: '#f8fafc' }}>
        <div style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
          gap: '0.5rem 1.5rem',
          padding: '0.75rem',
          background: '#fff',
          border: '1px solid #e2e8f0',
          borderRadius: '6px',
          fontSize: '0.85rem',
        }}>
          <div><span style={{ color: '#64748b' }}>Case ID</span><br /><code style={{ fontSize: '0.8rem' }}>…{shortId}</code></div>
          <div><span style={{ color: '#64748b' }}>Full ID</span><br /><code style={{ fontSize: '0.72rem', wordBreak: 'break-all' }}>{item.id}</code></div>
          <div><span style={{ color: '#64748b' }}>Social Worker</span><br />{item.socialWorkerExternalId ?? '—'}</div>
          <div><span style={{ color: '#64748b' }}>Opened</span><br />{new Date(item.openedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })}</div>
          <div><span style={{ color: '#64748b' }}>Closed</span><br />{item.closedAt ? new Date(item.closedAt).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }) : 'Open'}</div>
          <div><span style={{ color: '#64748b' }}>Safehouse</span><br />{item.safehouse}</div>
        </div>
        {onDelete && (
          <div style={{ marginTop: '0.5rem', display: 'flex', justifyContent: 'flex-end' }}>
            <button
              onClick={onDelete}
              disabled={isDeleting}
              style={{
                padding: '4px 14px',
                fontSize: '0.8rem',
                border: '1px solid #fca5a5',
                borderRadius: '5px',
                background: '#fee2e2',
                color: '#991b1b',
                cursor: isDeleting ? 'not-allowed' : 'pointer',
                opacity: isDeleting ? 0.6 : 1,
              }}
            >
              {isDeleting ? 'Deleting…' : 'Delete case'}
            </button>
          </div>
        )}
      </td>
    </tr>
  )
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function CaseloadInventoryPage() {
  const { session } = useAuth()
  const isAdmin = session?.role === 'Admin'

  const [lookups, setLookups] = useState<CaseloadLookupsResponse | null>(null)
  const [items, setItems] = useState<ResidentCaseListItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [expandedId, setExpandedId] = useState<string | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)

  // Secondary readiness flag fetch — silent fallback on error
  const [readinessFlags, setReadinessFlags] = useState<Map<string, ResidentReadinessFlag>>(new Map())

  // filters
  const [search, setSearch] = useState('')
  const [statusStateId, setStatusStateId] = useState('')
  const [categoryId, setCategoryId] = useState('')
  const [safehouseId, setSafehouseId] = useState('')
  const [desc, setDesc] = useState(true)
  const [page, setPage] = useState(1)
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [selectedResident, setSelectedResident] = useState<any | null>(null);
  const PAGE_SIZE = 15

  // debounced search
  const searchTimer = useRef<ReturnType<typeof setTimeout> | null>(null)
  const [debouncedSearch, setDebouncedSearch] = useState('')

  useEffect(() => {
    if (searchTimer.current) clearTimeout(searchTimer.current)
    searchTimer.current = setTimeout(() => setDebouncedSearch(search), 300)
    return () => { if (searchTimer.current) clearTimeout(searchTimer.current) }
  }, [search])

  // load lookups once
  useEffect(() => {
    fetchCaseloadLookups()
      .then(setLookups)
      .catch(() => {/* non-fatal */})
  }, [])

  // Load readiness flags once on mount — independent of pagination
  useEffect(() => {
    let cancelled = false
    async function loadFlags() {
      try {
        const flags = await fetchResidentReadinessFlags()
        if (!cancelled) {
          const map = new Map<string, ResidentReadinessFlag>()
          for (const f of flags) map.set(f.residentId, f)
          setReadinessFlags(map)
        }
      } catch {
        // Silent fallback — readiness column simply won't render
      }
    }
    void loadFlags()
    return () => { cancelled = true }
  }, [])

  // load cases when filters change
  useEffect(() => {
    let cancelled = false
    async function load() {
      try {
        setLoading(true)
        const result = await fetchResidentCases({
          page,
          pageSize: PAGE_SIZE,
          search: debouncedSearch || undefined,
          statusStateId: statusStateId ? Number(statusStateId) : undefined,
          categoryId: categoryId ? Number(categoryId) : undefined,
          safehouseId: safehouseId || undefined,
          desc,
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
    return () => { cancelled = true }
  }, [page, PAGE_SIZE, debouncedSearch, statusStateId, categoryId, safehouseId, desc])

  async function handleDelete(id: string) {
    if (!window.confirm('Are you sure you want to delete this case? This cannot be undone.')) return
    setDeletingId(id)
    try {
      await deleteResidentCase(id)
      setItems((prev) => prev.filter((x) => x.id !== id))
      setTotalCount((prev) => prev - 1)
      if (expandedId === id) setExpandedId(null)
    } catch (err) {
      setError(toUserFacingError(err, 'Failed to delete case'))
    } finally {
      setDeletingId(null)
    }
  }

  function resetFilters() {
    setSearch(''); setStatusStateId(''); setCategoryId(''); setSafehouseId(''); setPage(1)
  }

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE))
  const hasFilters = !!(search || statusStateId || categoryId || safehouseId)
  const handleSave = async (formData: any) => {
    try {
      const welfareData = `[Welfare Status: ${formData.socioStatus}] [Category: ${formData.category}] | Notes: ${formData.notes}`;

      const payload = {
        fullName: formData.fullName,
        medicalNotes: welfareData,
        dateOfBirth: "2000-01-01", // Placeholder for demo
        caseWorkerEmail: "assigned@safeharbor.org"
      };

      if (selectedResident) {
        await updateResident(selectedResident.id, payload);
      } else {
        await createResident(payload);
      }

      setIsModalOpen(false);
      setSelectedResident(null);
      
      // This will refresh your list so the new data shows up immediately
      window.location.reload(); 
    } catch (err) {
      // This utilizes your existing error handling structure
      alert("Save failed. Make sure your C# API is running locally!");
      console.error(err);
    }
  };
  const hasReadiness = readinessFlags.size > 0

  return (
    <section>
      {/* ── Header ── */}
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '1.5rem' }}>
        <div>
          <h1 style={{ marginBottom: '0.25rem' }}>Caseload Inventory</h1>
          <p className="lead" style={{ margin: 0 }}>View, search, and manage resident cases, placement, and status.</p>
        </div>
        <button 
          className="button button-primary" 
          onClick={() => setIsModalOpen(true)}
          style={{ padding: '10px 20px' }}
        >
          Add New Resident
        </button>
      </div>

      {/* ── Filter bar ── */}
      <div style={{
        display: 'flex',
        flexWrap: 'wrap',
        gap: '0.6rem',
        padding: '1rem',
        background: '#f8fafc',
        border: '1px solid #e2e8f0',
        borderRadius: '8px',
        marginBottom: '1rem',
      }}>
        <input
          style={{ flex: '1 1 200px', minWidth: '180px' }}
          placeholder="Search by name, safehouse, category…"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1) }}
        />

        <select
          style={{ flex: '1 1 140px' }}
          value={statusStateId}
          onChange={(e) => { setStatusStateId(e.target.value); setPage(1) }}
        >
          <option value="">All statuses</option>
          {lookups?.statusStates.map((s) => (
            <option key={s.id} value={s.id}>{s.name}</option>
          ))}
        </select>

        <select
          style={{ flex: '1 1 140px' }}
          value={categoryId}
          onChange={(e) => { setCategoryId(e.target.value); setPage(1) }}
        >
          <option value="">All categories</option>
          {lookups?.caseCategories.map((c) => (
            <option key={c.id} value={c.id}>{c.name}</option>
          ))}
        </select>

        <select
          style={{ flex: '1 1 140px' }}
          value={safehouseId}
          onChange={(e) => { setSafehouseId(e.target.value); setPage(1) }}
        >
          <option value="">All safehouses</option>
          {lookups?.safehouses.map((s) => (
            <option key={s.id} value={s.id}>{s.name}</option>
          ))}
        </select>

        <select
          style={{ flex: '0 0 auto' }}
          value={desc ? 'desc' : 'asc'}
          onChange={(e) => { setDesc(e.target.value === 'desc'); setPage(1) }}
        >
          <option value="desc">Newest first</option>
          <option value="asc">Oldest first</option>
        </select>

        {hasFilters && (
          <button onClick={resetFilters} style={{ flex: '0 0 auto', background: 'transparent', border: '1px solid #cbd5e1', color: '#64748b', borderRadius: '6px', padding: '0 0.75rem', cursor: 'pointer' }}>
            Clear filters
          </button>
        )}
      </div>

      {/* ── Summary line ── */}
      <div style={{ marginBottom: '0.75rem', fontSize: '0.85rem', color: '#64748b' }}>
        {loading ? 'Loading…' : `${totalCount} case${totalCount !== 1 ? 's' : ''} found`}
      </div>

      {/* ── Error ── */}
      {error && <p role="alert" style={{ color: '#dc2626', marginBottom: '1rem' }}>{error}</p>}

      {/* ── Table ── */}
      <div style={{ overflowX: 'auto', border: '1px solid #e2e8f0', borderRadius: '8px' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.9rem' }}>
          <thead>
            <tr style={{ background: '#f1f5f9', borderBottom: '2px solid #e2e8f0' }}>
              <th style={thStyle}>Resident</th>
              <th style={thStyle}>Status</th>
              <th style={thStyle}>Category</th>
              <th style={thStyle}>Safehouse</th>
              <th style={thStyle}>Social Worker</th>
              <th style={thStyle}>Opened</th>
              {readinessFlags.size > 0 && <th style={thStyle}>Readiness</th>}
              <th style={{ ...thStyle, textAlign: 'center' }}>Details</th>
            </tr>
          </thead>
          <tbody>
            {!loading && items.length === 0 && (
              <tr>
                <td colSpan={readinessFlags.size > 0 ? 8 : 7} style={{ textAlign: 'center', padding: '2.5rem', color: '#94a3b8' }}>
                  {hasFilters ? 'No cases match your filters.' : 'No cases found.'}
                </td>
              </tr>
            )}
            {loading && items.length === 0 && (
              <tr>
                <td colSpan={readinessFlags.size > 0 ? 8 : 7} style={{ textAlign: 'center', padding: '2.5rem', color: '#94a3b8' }}>
                  Loading cases…
                </td>
              </tr>
            )}
            {items.map((item) => (
              <React.Fragment key={item.id}>
                <tr
                  style={{
                    borderBottom: expandedId === item.id ? 'none' : '1px solid #f1f5f9',
                    background: expandedId === item.id ? '#f8fafc' : 'white',
                    transition: 'background 0.15s',
                  }}
                >
                  <td style={tdStyle}>
                    <strong>{item.residentName ?? <span style={{ color: '#94a3b8', fontStyle: 'italic' }}>Unknown</span>}</strong>
                  </td>
                  <td style={tdStyle}><StatusBadge status={item.status} /></td>
                  <td style={tdStyle}>{item.category}</td>
                  <td style={tdStyle}>{item.safehouse}</td>
                  <td style={{ ...tdStyle, color: '#64748b', fontSize: '0.82rem' }}>
                    {item.socialWorkerExternalId ?? '—'}
                  </td>
                  <td style={{ ...tdStyle, color: '#64748b', whiteSpace: 'nowrap' }}>
                    {new Date(item.openedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                  </td>
                  {readinessFlags.size > 0 && (
                    <td style={tdStyle}>
                      {item.residentEntityId && readinessFlags.has(item.residentEntityId) && (
                        <ReadinessBadge
                          level={readinessFlags.get(item.residentEntityId)!.level}
                          action={readinessFlags.get(item.residentEntityId)!.action}
                        />
                      )}
                    </td>
                  )}
                  <td style={{ ...tdStyle, textAlign: 'center' }}>
                    <button
                      onClick={() => setExpandedId(expandedId === item.id ? null : item.id)}
                      style={{
                        padding: '3px 12px',
                        fontSize: '0.8rem',
                        border: '1px solid #cbd5e1',
                        borderRadius: '5px',
                        background: expandedId === item.id ? '#e2e8f0' : 'white',
                        cursor: 'pointer',
                        color: '#334155',
                      }}
                    >
                      {expandedId === item.id ? 'Close' : 'View'}
                    </button>
                    <button
                      onClick={() => {
                        setSelectedResident(item);
                        setIsModalOpen(true);
                      }}
                      style={{ ...viewButtonStyle, borderColor: '#0d9488', color: '#0d9488' }}
                    >
                      Edit
                    </button>

                  <button 
                    onClick={() => handleDelete(item.id)} // handleDelete already uses deleteResidentCase internally
                    style={{ ...viewButtonStyle, borderColor: '#be123c', color: '#be123c' }}
                  >
                    Delete
                  </button>
                  </td>
                </tr>
                {expandedId === item.id && (
                  <CaseDetail
                    key={`detail-${item.id}`}
                    item={item}
                    onDelete={isAdmin ? () => void handleDelete(item.id) : undefined}
                    isDeleting={deletingId === item.id}
                    hasReadiness={hasReadiness}
                  />
                )}
              </React.Fragment> // Ensure this is closed
            ))}
          </tbody>
        </table>
      </div>

      {/* ── Pagination ── */}
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '1rem', marginTop: '1.25rem' }}>
        <button
          disabled={page <= 1}
          onClick={() => setPage((v) => v - 1)}
          style={{ padding: '6px 16px', borderRadius: '6px', border: '1px solid #cbd5e1', cursor: page <= 1 ? 'not-allowed' : 'pointer', opacity: page <= 1 ? 0.4 : 1 }}
        >
          ← Previous
        </button>
        <span style={{ fontSize: '0.9rem', color: '#475569' }}>
          Page {page} of {totalPages}
        </span>
        <button
          disabled={page >= totalPages}
          onClick={() => setPage((v) => v + 1)}
          style={{ padding: '6px 16px', borderRadius: '6px', border: '1px solid #cbd5e1', cursor: page >= totalPages ? 'not-allowed' : 'pointer', opacity: page >= totalPages ? 0.4 : 1 }}
        >
          Next →
        </button>
      </div>

      {/* ── Welfare Management Modal (CRUD: Create & Update) ── */}
      {isModalOpen && (
        <div style={{
          position: 'fixed', top: 0, left: 0, width: '100%', height: '100%',
          background: 'rgba(0,0,0,0.5)', display: 'flex', justifyContent: 'center', alignItems: 'center', zIndex: 1000
        }}>
          <div style={{ background: 'white', padding: '2rem', borderRadius: '12px', width: '500px', boxShadow: '0 20px 25px -5px rgba(0,0,0,0.1)' }}>
            {/* Title changes dynamically based on if we are Creating or Updating */}
            <h2 style={{ marginBottom: '1rem' }}>
              {selectedResident ? `Edit ${selectedResident.residentName}` : 'Add New Resident'}
            </h2>
            
            <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
              <label style={{ fontSize: '0.8rem', fontWeight: 600 }}>Full Name</label>
              <input 
                placeholder="Full Name" 
                style={{ padding: '8px', borderRadius: '4px', border: '1px solid #cbd5e1' }} 
                defaultValue={selectedResident?.residentName || ''} 
              />
              
              <label style={{ fontSize: '0.8rem', fontWeight: 600 }}>Case Category</label>
              <select 
                style={{ padding: '8px' }} 
                defaultValue={selectedResident?.category || ''}
              >
                <option value="">Select Category...</option>
                <option>Trafficked</option>
                <option>Victim of Physical Abuse</option>
                <option>Neglected</option>
                <option>Abandoned</option>
              </select>

              <label style={{ fontSize: '0.8rem', fontWeight: 600 }}>Socio-Demographic Profile (Welfare Status)</label>
              <select style={{ padding: '8px' }}>
                <option>4Ps Beneficiary</option>
                <option>Solo Parent</option>
                <option>Indigenous Group</option>
                <option>Informal Settler</option>
                <option>None of the above</option>
              </select>

              <label style={{ fontSize: '0.8rem', fontWeight: 600 }}>Medical & Case Notes</label>
              <textarea 
                placeholder="Enter detailed case notes here..." 
                style={{ padding: '8px', minHeight: '100px', borderRadius: '4px', border: '1px solid #cbd5e1' }} 
                defaultValue={selectedResident?.medicalNotes || ''}
              />
            </div>

            <div style={{ marginTop: '1.5rem', display: 'flex', justifyContent: 'flex-end', gap: '0.5rem' }}>
              <button 
                onClick={() => {
                  setIsModalOpen(false);
                  setSelectedResident(null); // Reset when canceling
                }} 
                className="button button-secondary"
              >
                Cancel
              </button>

              <button 
                onClick={() => {
                  // Grab the values from the modal inputs
                  const fullName = (document.querySelector('input[placeholder="Full Name"]') as HTMLInputElement).value;
                  const category = (document.querySelector('select:nth-of-type(1)') as HTMLSelectElement).value;
                  const socio = (document.querySelector('select:nth-of-type(2)') as HTMLSelectElement).value;
                  const notes = (document.querySelector('textarea') as HTMLTextAreaElement).value;

                  handleSave({
                    fullName,
                    category,
                    socioStatus: socio,
                    notes
                  });
                }} 
                className="button button-primary"
              >
                {selectedResident ? 'Save Changes' : 'Create Resident'}
              </button>
            </div>
          </div>
        </div>
      )}
    </section>
  );
}
  

const thStyle: React.CSSProperties = {
  padding: '0.65rem 1rem',
  textAlign: 'left',
  fontWeight: 600,
  fontSize: '0.8rem',
  color: '#475569',
  textTransform: 'uppercase',
  letterSpacing: '0.04em',
}

const tdStyle: React.CSSProperties = {
  padding: '0.75rem 1rem',
  verticalAlign: 'middle',
}

const viewButtonStyle: React.CSSProperties = {
  padding: '3px 12px',
  fontSize: '0.8rem',
  border: '1px solid #cbd5e1',
  borderRadius: '5px',
  background: 'white',
  cursor: 'pointer',
  marginLeft: '4px'
}