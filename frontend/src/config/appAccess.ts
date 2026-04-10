import type { AppRole } from '../auth/authSession'

export type AppNavRoute = {
  to: string
  label: string
  roles?: AppRole[]
}

// Permission matrix is derived from backend controller policies so nav links remain
// aligned with endpoints callers can actually invoke.
export const appNavRoutes: AppNavRoute[] = [
  { to: '/app/dashboard', label: 'Admin Dashboard', roles: ['Admin', 'SocialWorker'] },
  { to: '/app/donors', label: 'Donors & Contributions', roles: ['Admin', 'SocialWorker'] },
  { to: '/app/donor-analytics', label: 'Donor Analytics', roles: ['Admin'] },
  { to: '/app/caseload', label: 'Caseload', roles: ['Admin', 'SocialWorker'] },
  { to: '/app/contributions', label: 'Manage Contributions', roles: ['Admin'] },
  { to: '/app/process-recording', label: 'Process Recording', roles: ['SocialWorker'] },
  { to: '/app/visitation-conferences', label: 'Visitation & Conferences', roles: ['Admin', 'SocialWorker'] },
  { to: '/app/reports', label: 'Reports', roles: ['Admin', 'SocialWorker'] },
]
