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
import { DonorsContributionsPage } from './pages/app/DonorsContributionsPage'
import { CaseloadInventoryPage } from './pages/app/CaseloadInventoryPage'
import { ProcessRecordingPage } from './pages/app/ProcessRecordingPage'
import { HomeVisitationConferencesPage } from './pages/app/HomeVisitationConferencesPage'
import { ReportsAnalyticsPage } from './pages/app/ReportsAnalyticsPage'
import { YourDonationsPage } from './pages/donor/YourDonationsPage'
import { AdminDonorAnalyticsPage } from './pages/app/AdminDonorAnalyticsPage'
import { DonatePage } from './pages/DonatePage'

const router = createBrowserRouter([
  {
    path: '/',
    element: <App />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'impact', element: <ImpactDashboardPage /> },
      { path: 'login', element: <LoginPage /> },
      // ADR authorization matrix decision:
      // keep /donor/dashboard isolated as donor-role-only, not general authenticated access.
      {
        path: 'donor',
        element: <ProtectedRoute allowedRoles={['Donor']} />,
        children: [{ path: 'dashboard', element: <YourDonationsPage /> }],
      },
      // All non-public/non-donor routes are restricted to staff roles.
      {
        element: <ProtectedRoute allowedRoles={['Admin', 'SocialWorker']} />,
        children: [
          { path: 'privacy', element: <PrivacyPage /> },
          { path: 'donate', element: <DonatePage /> },
        ],
      },
      {
        path: 'app',
        // Restrict /app/* to staff roles only. Donors are redirected to / if they try to visit staff routes.
        element: <ProtectedRoute allowedRoles={['Admin', 'SocialWorker']} />,
        children: [
          { path: 'dashboard', element: <AdminDashboardPage /> },
          { path: 'donors', element: <DonorsContributionsPage /> },
          { path: 'donor-analytics', element: <AdminDonorAnalyticsPage /> },
          { path: 'caseload', element: <CaseloadInventoryPage /> },
          {
            path: 'process-recording',
            element: <ProtectedRoute allowedRoles={['SocialWorker']} />,
            children: [{ index: true, element: <ProcessRecordingPage /> }],
          },
          { path: 'visitation-conferences', element: <HomeVisitationConferencesPage /> },
          { path: 'reports', element: <ReportsAnalyticsPage /> },
        ],
      },
    ],
  },
])

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider>
      <RouterProvider router={router} />
    </AuthProvider>
  </StrictMode>,
)
