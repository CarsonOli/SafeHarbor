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

const router = createBrowserRouter([
  {
    path: '/',
    element: <App />,
    children: [
      { index: true, element: <HomePage /> },
      { path: 'impact', element: <ImpactDashboardPage /> },
      { path: 'login', element: <LoginPage /> },
      { path: 'privacy', element: <PrivacyPage /> },
      {
        path: 'app',
        element: <ProtectedRoute />,
        children: [
          { path: 'dashboard', element: <AdminDashboardPage /> },
          { path: 'donors', element: <DonorsContributionsPage /> },
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
