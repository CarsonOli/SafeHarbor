import { NavLink, Outlet } from 'react-router-dom'
import { CookieConsentBanner } from './components/CookieConsentBanner'
import { useAuth } from './auth/AuthContext'

function App() {
  const { session, logout } = useAuth()

  const navigation = [
    { to: '/', label: 'Home' },
    { to: '/impact', label: 'Impact Dashboard' },
    ...(session
      ? [
          { to: '/app/dashboard', label: 'Admin Dashboard' },
          { to: '/app/donors', label: 'Donors' },
          { to: '/app/caseload', label: 'Caseload' },
          { to: '/app/process-recording', label: 'Process Recording' },
          { to: '/app/visitation-conferences', label: 'Visitation & Conferences' },
          { to: '/app/reports', label: 'Reports' },
        ]
      : []),
    { to: '/login', label: session ? 'Switch User' : 'Login' },
    { to: '/privacy', label: 'Privacy' },
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
