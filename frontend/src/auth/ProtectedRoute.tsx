import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from './AuthContext'
import type { AppRole } from './authSession'

type ProtectedRouteProps = {
  allowedRoles?: ReadonlyArray<AppRole>
}

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const { session } = useAuth()
  const location = useLocation()

  if (!session) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  if (allowedRoles && !allowedRoles.includes(session.role)) {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
