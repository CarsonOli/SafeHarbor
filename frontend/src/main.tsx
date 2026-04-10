import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { RouterProvider, createBrowserRouter } from 'react-router-dom'
import App from './App'
import './index.css'
import { AuthProvider } from './auth/AuthContext'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { HomePage } from './pages/HomePage'
import { ImpactDashboardPage } from './pages/ImpactDashboardPage'
import { LoginPage } from './pages/LoginPage'
import { PrivacyPage } from './pages/PrivacyPage'
import { AdminDashboardPage } from './pages/app/AdminDashboardPage'
import { CaseloadInventoryPage } from './pages/app/CaseloadInventoryPage'
import { ProcessRecordingPage } from './pages/app/ProcessRecordingPage'
import { HomeVisitationConferencesPage } from './pages/app/HomeVisitationConferencesPage'
import { ReportsAnalyticsPage } from './pages/app/ReportsAnalyticsPage'
import { YourDonationsPage } from './pages/donor/YourDonationsPage'
import { AdminDonorAnalyticsPage } from './pages/app/AdminDonorAnalyticsPage'
import { DonatePage } from './pages/DonatePage'
import { DonorsContributionsPage } from './pages/app/DonorsContributionsPage'
import AdminContributionsPage from './pages/app/AdminContributionsPage'
import SocialMediaStrategy from './pages/SocialMediaStrategy'
import { SocialMediaScorerPage } from './pages/app/SocialMediaScorerPage'

// ── THEME INITIALIZATION ──────────────────────────────────────────────────
// This runs before React boots to prevent the "white flash" on page load.
// It checks the cookie and applies the .dark class to the <html> tag.
const themeCookie = document.cookie
  .split('; ')
  .find((row) => row.startsWith('theme='))
  ?.split('=')[1];

if (themeCookie === 'dark') {
  document.documentElement.classList.add('dark');
} else {
  document.documentElement.classList.remove('dark');
}
// ──────────────────────────────────────────────────────────────────────────

export const appRoutes = [
  {
    path: '/',
    element: <App />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'impact', element: <ImpactDashboardPage /> },
      { path: 'login', element: <LoginPage /> },
      // Keep core GDPR/privacy policy publicly accessible for all visitors.
      { path: 'privacy', element: <PrivacyPage /> },
      // ADR authorization matrix decision:
      // keep /donor/dashboard isolated as donor-role-only, not general authenticated access.
      {
        path: 'donor',
        element: <ProtectedRoute allowedRoles={['Donor']} />,
        children: [
          { path: 'dashboard', element: <YourDonationsPage /> },
          { path: 'donations', element: <YourDonationsPage /> },
        ],
      },
      // Donation UX decision:
      // /donate stays public so guests can complete the "continue as guest" flow,
      // while authenticated donors can also use the same page and post to their history.
      { path: 'donate', element: <DonatePage /> },
      {
        path: 'app',
        // Restrict /app/* to staff roles only. Donors are redirected to / if they try to visit staff routes.
        element: <ProtectedRoute allowedRoles={['Admin', 'SocialWorker']} />,
        children: [
          { path: 'dashboard', element: <AdminDashboardPage /> },
          { path: 'donors', element: <DonorsContributionsPage /> },
          {
            path: 'donor-analytics',
            element: <ProtectedRoute allowedRoles={['Admin']} />,
            children: [{ index: true, element: <AdminDonorAnalyticsPage /> }],
          },
          { path: 'caseload', element: <CaseloadInventoryPage /> },
          {
            path: 'contributions',
            element: <ProtectedRoute allowedRoles={['Admin']} />,
            children: [{ index: true, element: <AdminContributionsPage /> }],
          },
          {
            path: 'process-recording',
            element: <ProtectedRoute allowedRoles={['SocialWorker']} />,
            children: [{ index: true, element: <ProcessRecordingPage /> }],
          },
          { path: 'visitation-conferences', element: <HomeVisitationConferencesPage /> },
          { path: 'reports', element: <ReportsAnalyticsPage /> },
          { path: 'social-media', element: <SocialMediaStrategy /> },
          {
            path: 'social-media-scorer',
            element: <ProtectedRoute allowedRoles={['Admin']} />,
            children: [{ index: true, element: <SocialMediaScorerPage /> }],
          },
        ],
      },
    ],
  },
]

export const router = createBrowserRouter(appRoutes)

const rootElement = document.getElementById('root')

if (rootElement) {
  createRoot(rootElement).render(
    <StrictMode>
      <AuthProvider>
        <RouterProvider router={router} />
      </AuthProvider>
    </StrictMode>,
  )
}
