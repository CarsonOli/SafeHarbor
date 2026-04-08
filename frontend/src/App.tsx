import { Link, NavLink, Outlet } from 'react-router-dom'
import { CookieConsentBanner } from './components/CookieConsentBanner'
import { useAuth } from './auth/AuthContext'

function App() {
  const { session, logout } = useAuth()
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
          { to: '/app/donors', label: 'Donors' },
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
    ...(isDonor
      ? [{ to: '/donor/dashboard', label: 'My Donations' }]
      : []),

    { to: '/login', label: session ? 'Switch User' : 'Login' },
  ]

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main-content">
        Skip to main content
      </a>

      <header className="site-header" role="banner">
        <div className="container nav-container">
          <div className="brand" aria-label="Safe Harbor">
            Safe Harbor
          </div>
          <nav aria-label="Primary">
            <ul className="nav-list">
              {navigation.map((item) => (
                <li key={item.to}>
                  <NavLink
                    to={item.to}
                    className={({ isActive }) =>
                      `nav-link${isActive ? ' nav-link-active' : ''}`
                    }
                    end={item.to === '/'}
                  >
                    {item.label}
                  </NavLink>
                </li>
              ))}
              {/* Keep CTA aligned with route guards: only staff can navigate to /donate. */}
              {isStaff && (
                <li>
                  <Link to="/donate" className="button nav-donate-button">
                    Donate Now
                  </Link>
                </li>
              )}
              {session && (
                <li>
                  <button type="button" className="button button-secondary" onClick={logout}>
                    Sign out ({session.role})
                  </button>
                </li>
              )}
            </ul>
          </nav>
        </div>
      </header>

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
