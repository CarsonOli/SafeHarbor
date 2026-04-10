import { Link } from 'react-router-dom'

const stories = [
  {
    role: "Former Resident",
    quote: "Safe Harbor gave me more than a place to stay — it gave me hope. When I arrived, I was hurting and afraid, but here I found safety, healing, and a chance to start over. Today, I believe my story can be different.",
    rotation: "-2deg",
  },
  {
    role: "Volunteer",
    quote: "I came to Safe Harbor hoping to help, but I never expected how much it would change my own heart. In the small everyday moments, I've seen courage, healing, and joy return. It's a reminder that love and consistency really can change a life.",
    rotation: "1.5deg",
  },
  {
    role: "Donor",
    quote: "When I chose to support Safe Harbor, I knew I wanted to help girls find safety and hope. What moved me most was seeing that every gift helps create real healing and real opportunity. Giving here feels like investing in a future that is truly worth fighting for.",
    rotation: "-1deg",
  },
]

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
          <div className="cta-row" style={{ justifyContent: 'center', marginTop: '2.5rem' }}>
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

      {/* --- STORIES SECTION --- */}
      <section className="stories-section" aria-labelledby="stories-title">
        <div className="container">
          <h2 className="stories-heading" id="stories-title">Stories from Our Community</h2>
          <p className="stories-subheading">In their own words...</p>
          <div className="stories-grid">
            {stories.map((s) => (
              <div
                key={s.role}
                className="notebook-card"
                style={{ '--card-rotation': s.rotation } as React.CSSProperties}
              >
                <div className="notebook-margin-line" />
                <span className="notebook-role">{s.role}</span>
                <blockquote className="notebook-quote">"{s.quote}"</blockquote>
              </div>
            ))}
          </div>
        </div>
      </section>
    </div>
  )
}