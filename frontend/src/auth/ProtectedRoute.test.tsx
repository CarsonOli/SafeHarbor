;(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true

import { act } from 'react'
import { createRoot } from 'react-dom/client'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ProtectedRoute } from './ProtectedRoute'
import type { AuthSession } from './authSession'

const mockUseAuth = vi.fn<() => { session: AuthSession | null }>()

vi.mock('./AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}))

function LoginProbe() {
  const location = useLocation()
  const fromPath = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname

  return (
    <div>
      login-page
      <span data-testid="from-path">{fromPath ?? 'none'}</span>
    </div>
  )
}

function renderRoute(initialPath: string) {
  const container = document.createElement('div')
  document.body.appendChild(container)
  const root = createRoot(container)

  act(() => {
    root.render(
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/" element={<div>home-page</div>} />
          <Route path="/login" element={<LoginProbe />} />
          <Route path="/app" element={<ProtectedRoute allowedRoles={['Admin']} />}>
            <Route path="dashboard" element={<div>admin-dashboard</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    )
  })

  return {
    container,
    cleanup: () => {
      act(() => {
        root.unmount()
      })
      container.remove()
    },
  }
}

describe('ProtectedRoute', () => {
  afterEach(() => {
    document.body.innerHTML = ''
    vi.clearAllMocks()
  })

  it('redirects unauthenticated users to /login and preserves source path in navigation state', () => {
    mockUseAuth.mockReturnValue({ session: null })

    const { container, cleanup } = renderRoute('/app/dashboard')

    expect(container.textContent).toContain('login-page')
    expect(container.querySelector('[data-testid="from-path"]')?.textContent).toBe('/app/dashboard')

    cleanup()
  })

  it('redirects authenticated users without the required role to /', () => {
    mockUseAuth.mockReturnValue({
      session: { email: 'donor@example.com', role: 'Donor' },
    })

    const { container, cleanup } = renderRoute('/app/dashboard')

    expect(container.textContent).toContain('home-page')
    expect(container.textContent).not.toContain('admin-dashboard')

    cleanup()
  })

  it('renders nested routes when the session role is allowed', () => {
    mockUseAuth.mockReturnValue({
      session: { email: 'admin@safeharbor.org', role: 'Admin' },
    })

    const { container, cleanup } = renderRoute('/app/dashboard')

    expect(container.textContent).toContain('admin-dashboard')

    cleanup()
  })
})
