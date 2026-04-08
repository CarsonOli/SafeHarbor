;(globalThis as { IS_REACT_ACT_ENVIRONMENT?: boolean }).IS_REACT_ACT_ENVIRONMENT = true

import { act } from 'react'
import { createRoot } from 'react-dom/client'
import { Outlet, RouterProvider, createMemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import type { AuthSession } from './auth/authSession'

let mockSession: AuthSession | null = null

vi.mock('./auth/AuthContext', () => ({
  AuthProvider: ({ children }: { children: unknown }) => children,
  useAuth: () => ({ session: mockSession }),
}))

vi.mock('./App', () => ({ default: () => <Outlet /> }))
vi.mock('./pages/HomePage', () => ({ HomePage: () => <div>home-page</div> }))
vi.mock('./pages/ImpactDashboardPage', () => ({ ImpactDashboardPage: () => <div>impact-page</div> }))
vi.mock('./pages/LoginPage', () => ({ LoginPage: () => <div>login-page</div> }))
vi.mock('./pages/PrivacyPage', () => ({ PrivacyPage: () => <div>privacy-page</div> }))
vi.mock('./pages/app/AdminDashboardPage', () => ({ AdminDashboardPage: () => <div>admin-dashboard-page</div> }))
vi.mock('./pages/app/DonorsContributionsPage', () => ({ DonorsContributionsPage: () => <div>donors-contributions-page</div> }))
vi.mock('./pages/app/CaseloadInventoryPage', () => ({ CaseloadInventoryPage: () => <div>caseload-inventory-page</div> }))
vi.mock('./pages/app/ProcessRecordingPage', () => ({ ProcessRecordingPage: () => <div>process-recording-page</div> }))
vi.mock('./pages/app/HomeVisitationConferencesPage', () => ({ HomeVisitationConferencesPage: () => <div>visitation-conferences-page</div> }))
vi.mock('./pages/app/ReportsAnalyticsPage', () => ({ ReportsAnalyticsPage: () => <div>reports-analytics-page</div> }))
vi.mock('./pages/donor/YourDonationsPage', () => ({ YourDonationsPage: () => <div>your-donations-page</div> }))
vi.mock('./pages/app/AdminDonorAnalyticsPage', () => ({ AdminDonorAnalyticsPage: () => <div>admin-donor-analytics-page</div> }))
vi.mock('./pages/DonatePage', () => ({ DonatePage: () => <div>donate-page</div> }))

async function renderRoute(path: string) {
  const { appRoutes } = await import('./main')

  const container = document.createElement('div')
  document.body.appendChild(container)

  const root = createRoot(container)
  const router = createMemoryRouter(appRoutes, { initialEntries: [path] })

  await act(async () => {
    root.render(<RouterProvider router={router} />)
    await Promise.resolve()
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

describe('main routing guards', () => {
  afterEach(() => {
    mockSession = null
    document.body.innerHTML = ''
  })

  it('redirects unauthenticated users away from /app/* to /login', async () => {
    const { container, cleanup } = await renderRoute('/app/dashboard')

    expect(container.textContent).toContain('login-page')
    expect(container.textContent).not.toContain('admin-dashboard-page')

    cleanup()
  })

  it('allows donors to access /donor/dashboard and blocks staff routes', async () => {
    mockSession = { email: 'alice@example.com', role: 'Donor' }
    const donorView = await renderRoute('/donor/dashboard')

    expect(donorView.container.textContent).toContain('your-donations-page')
    donorView.cleanup()

    const blockedStaffView = await renderRoute('/app/dashboard')

    expect(blockedStaffView.container.textContent).toContain('home-page')
    expect(blockedStaffView.container.textContent).not.toContain('admin-dashboard-page')
    blockedStaffView.cleanup()
  })

  it('enforces nested SocialWorker-only route inside /app/process-recording', async () => {
    mockSession = { email: 'social.worker@safeharbor.org', role: 'SocialWorker' }
    const socialWorkerView = await renderRoute('/app/process-recording')

    expect(socialWorkerView.container.textContent).toContain('process-recording-page')
    socialWorkerView.cleanup()

    mockSession = { email: 'admin@safeharbor.org', role: 'Admin' }
    const adminView = await renderRoute('/app/process-recording')

    expect(adminView.container.textContent).toContain('home-page')
    expect(adminView.container.textContent).not.toContain('process-recording-page')
    adminView.cleanup()
  })
})
