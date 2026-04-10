import { Link } from 'react-router-dom'

const missionPoints = [
  {
    title: "Privacy First",
    text: "Protect survivor privacy with anonymized reporting and role-based access."
  },
  {
    title: "Global Coordination",
    text: "Coordinate services across housing, healthcare, legal aid, and community partners."
  },
  {
    title: "Transparent Impact",
    text: "Measure impact transparently while safeguarding sensitive records."
  }
]

export function HomePage() {
  return (
    <div className="home-page">
      {/* --- HERO SECTION WITH TEAL OVERLAY --- */}
      <section className="hero-wrapper" aria-labelledby="home-title">
        <div className="hero-overlay"></div>
        
        <div className="hero-content">
          <p className="eyebrow" style={{ color: 'rgba(255,255,255,0.8)', marginBottom: '1rem' }}>
            501(c)(3) Nonprofit Organization
          </p>
          <h1 id="home-title">Every Girl Deserves a Safe Harbor</h1>
          <p className="lead">
            SafeHarbor International provides rescue, rehabilitation, and reintegration
            for girls who are survivors of sexual abuse and trafficking — because healing
            is possible.
          </p>
          <div className="cta-row" style={{ 
              display: 'flex', 
              justifyContent: 'center', 
              alignItems: 'center', // Added this to ensure vertical alignment
              gap: '1.5rem',        // This handles the spacing between them
              marginTop: '2.5rem',
              width: '100%'         // Ensures the row spans the whole content area
            }}>
            <Link to="/donate" className="button-donate-hero">
              Donate Now →
            </Link>
            {/* Using the 'ghost' style for a cleaner look against the image */}
            <Link to="/impact" className="button-ghost">
              Stories of Hope
            </Link>
          </div>
        </div>
      </section>

      {/* --- MISSION POINTS (The 3 Cards) --- */}
      <div className="container" style={{ marginTop: '-4rem', position: 'relative', zIndex: 10 }}>
        <div className="feature-grid" aria-label="Mission highlights">
          {missionPoints.map((point) => (
            <article key={point.title} className="feature-card">
              <span className="eyebrow" style={{ color: 'var(--color-primary)' }}>Mission focus</span>
              <h3>{point.title}</h3>
              <p>{point.text}</p>
            </article>
          ))}
        </div>
      </div>
    </div>
  )
}