import { useEffect, useState } from 'react'
import { Link, NavLink, Outlet } from 'react-router-dom'
import { CookieConsentBanner } from './components/CookieConsentBanner'
import { useAuth } from './auth/AuthContext'
import { appNavRoutes } from './main'

function App() {
  const { session, logout } = useAuth()
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const isDonor = session?.role === 'Donor'
  const role = session?.role

  // Build the navigation list based on the logged-in role.
  // - Visitors (no session): see public pages including /donate.
  // - Donors: see /donate and their donation history dashboard.
  // - Staff (Admin, SocialWorker): see staff-only routes plus public pages.
  const navigation = [
    { to: '/', label: 'Home' },
    { to: '/impact', label: 'Impact Dashboard' },
    // Keep Donate visible to everyone because the page supports both guest and donor flows.
    { to: '/donate', label: 'Donate' },

    // Staff-only nav links — hidden from donors and visitors.
    ...(role
      ? appNavRoutes.filter((route) => route.roles?.includes(role)).map(({ to, label }) => ({ to, label }))
      : []),

    // Donor-only nav link — shown only when logged in as a Donor.
    ...(isDonor ? [{ to: '/donor/dashboard', label: 'My Donations' }] : []),

    { to: '/privacy', label: 'Privacy' },
    { to: '/login', label: session ? 'Switch User' : 'Login' },
  ]

  // Keep drawer state consistent when auth role changes reshape the nav list.
  useEffect(() => {
    setIsMenuOpen(false)
  }, [session?.role])

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main-content">
        Skip to main content
      </a>

      <header className="site-header" role="banner">
        <div className="container nav-container">
          <div className="header-brand-group">
            <button
              type="button"
              className="button button-secondary nav-menu-toggle"
              onClick={() => setIsMenuOpen((previous) => !previous)}
              aria-expanded={isMenuOpen}
              aria-controls="primary-nav-drawer"
              aria-label={isMenuOpen ? 'Close menu' : 'Open menu'}
            >
              <span className="nav-menu-icon" aria-hidden="true">
                ☰
              </span>
              <span className="sr-only">{isMenuOpen ? 'Close menu' : 'Open menu'}</span>
            </button>
            
            {/* Added Link wrapper and Logo Image */}
            <Link to="/" className="brand-link" style={{ display: 'flex', alignItems: 'center', gap: '12px', textDecoration: 'none', position: 'relative', zIndex: 10 }}>
              <img 
                src="/favicon.png"  /* We know this file works because your tab icon works! */
                alt="Safe Harbor Logo" 
                style={{ 
                  height: '38px', 
                  width: 'auto',
                  display: 'inline-block',
                  /* Temporarily removed the filter to ensure it's visible */
                }} 
              />
              <div className="brand" aria-label="Safe Harbor" style={{ color: '#2a5c5c' }}>
                Safe Harbor
              </div>
            </Link>
          </div>
          <div className="header-actions">
            {session && (
              <button type="button" className="button button-secondary" onClick={logout}>
                Sign out ({session.role})
              </button>
            )}
          </div>
        </div>
      </header>

      {isMenuOpen && (
        <button
          type="button"
          className="nav-overlay"
          aria-label="Close menu"
          onClick={() => setIsMenuOpen(false)}
        />
      )}

      <nav
        id="primary-nav-drawer"
        className={`side-nav-drawer${isMenuOpen ? ' side-nav-drawer-open' : ''}`}
        aria-label="Primary"
      >
        <div className="side-nav-header">
          <p className="eyebrow">Navigation</p>
          {/* Keep CTA aligned with route guards: /donate is publicly accessible. */}
          <Link to="/donate" className="button nav-donate-button" onClick={() => setIsMenuOpen(false)}>
            Donate Now
          </Link>
        </div>
        <ul className="side-nav-list">
          {navigation.map((item) => (
            <li key={item.to}>
              <NavLink
                to={item.to}
                className={({ isActive }) => `nav-link${isActive ? ' nav-link-active' : ''}`}
                end={item.to === '/'}
                onClick={() => setIsMenuOpen(false)}
              >
                {item.label}
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      <main id="main-content" className="container page-content" role="main">
        <Outlet />
      </main>

      <footer className="site-footer" role="contentinfo">
        <div className="container footer-content">
          <p>© {new Date().getFullYear()} Safe Harbor</p>
          <p>Anonymized impact insights for responsible care partnerships.</p>
          {/* GDPR policy must remain discoverable to authenticated and non-authenticated users. */}
          <p>
            <Link to="/privacy">Privacy policy</Link>
          </p>
        </div>
      </footer>

      <CookieConsentBanner />
    </div>
  )
}

export default App
