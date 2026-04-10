interface ReadinessBadgeProps {
  level: 'High' | 'Medium' | 'Low' | string
  action?: string
}

const LEVEL_STYLES: Record<string, { background: string; label: string }> = {
  High:   { background: '#1D9E75', label: 'Ready' },
  Medium: { background: '#F5A623', label: 'In Progress' },
  Low:    { background: '#E24B4A', label: 'Not Ready' },
}

export function ReadinessBadge({ level, action }: ReadinessBadgeProps) {
  const style = LEVEL_STYLES[level] ?? { background: '#999', label: level }
  return (
    <span
      style={{
        background: style.background,
        color: '#fff',
        borderRadius: '4px',
        padding: '2px 7px',
        fontSize: '0.72rem',
        fontWeight: 600,
        whiteSpace: 'nowrap',
        cursor: action ? 'help' : 'default',
      }}
      title={action}
      aria-label={action ? `${style.label} — ${action}` : style.label}
    >
      {style.label}
    </span>
  )
}
