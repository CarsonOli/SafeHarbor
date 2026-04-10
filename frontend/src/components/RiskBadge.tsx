interface RiskBadgeProps {
  level: 'High' | 'Medium' | 'Low' | string
  size?: 'sm' | 'md'
}

const LEVEL_STYLES: Record<string, { background: string; label: string }> = {
  High:   { background: '#E24B4A', label: 'High Risk' },
  Medium: { background: '#F5A623', label: 'Medium Risk' },
  Low:    { background: '#1D9E75', label: 'Low Risk' },
}

export function RiskBadge({ level, size = 'sm' }: RiskBadgeProps) {
  const style = LEVEL_STYLES[level] ?? { background: '#999', label: level }
  const fontSize = size === 'sm' ? '0.72rem' : '0.85rem'
  return (
    <span
      style={{
        background: style.background,
        color: '#fff',
        borderRadius: '4px',
        padding: size === 'sm' ? '2px 7px' : '3px 10px',
        fontSize,
        fontWeight: 600,
        whiteSpace: 'nowrap',
      }}
      aria-label={`${style.label}`}
    >
      {style.label}
    </span>
  )
}
