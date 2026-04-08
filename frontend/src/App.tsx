import { useEffect, useState } from 'react'
import { Link, NavLink, Outlet } from 'react-router-dom'
import { CookieConsentBanner } from './components/CookieConsentBanner'
import { useAuth } from './auth/AuthContext'

function App() {
  const { session, logout } = useAuth()
  const [isMenuOpen, setIsMenuOpen] = useState(false)
  const isStaff = session?.role === 'Admin' || session?.role === 'SocialWorker'
  const isDonor = session?.role === 'Donor'

  // Build the navigation list based on the logged-in role.
  // - Visitors (no session): see only public pages + login link.
  // - Donors: see only their donor dashboard link (matrix keeps donor and staff areas separate).
  // - Staff (Admin, SocialWorker): see all staff-only routes, including /privacy and /donate.
  const navigation = [
    { to: '/', label: 'Home' },
    { to: '/impact', label: 'Impact Dashboard' },

    // Staff-only nav links — hidden from donors and visitors.
    ...(isStaff
      ? [
          { to: '/app/dashboard', label: 'Admin Dashboard' },
          { to: '/app/donors', label: 'Manage Contributions' },
          { to: '/app/donor-analytics', label: 'Donor Analytics' },
          { to: '/app/caseload', label: 'Caseload' },
          { to: '/app/process-recording', label: 'Process Recording' },
          { to: '/app/visitation-conferences', label: 'Visitation & Conferences' },
          { to: '/app/reports', label: 'Reports' },
          { to: '/privacy', label: 'Privacy' },
          { to: '/donate', label: 'Donate' },
        ]
      : []),

    // Donor-only nav link — shown only when logged in as a Donor.
    ...(isDonor ? [{ to: '/donor/dashboard', label: 'My Donations' }] : []),

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
            <div className="brand" aria-label="Safe Harbor">
              Safe Harbor
            </div>
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
          {/* Keep CTA aligned with route guards: only staff can navigate to /donate. */}
          {isStaff && (
            <Link to="/donate" className="button nav-donate-button" onClick={() => setIsMenuOpen(false)}>
              Donate Now
            </Link>
          )}
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
        </div>
      </footer>

      <CookieConsentBanner />
    </div>
  )
}

export default App
