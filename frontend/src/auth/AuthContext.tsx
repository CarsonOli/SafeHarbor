import { createContext, useContext, useMemo, useState, type PropsWithChildren } from 'react'
import { roles, type AppRole, type AuthSession, loadSession, persistSession } from './authSession'

type AuthContextValue = {
  session: AuthSession | null
  loginWithToken: (idToken: string) => AppRole
  logout: () => void
}

type JwtClaims = {
  email?: string
  preferred_username?: string
  upn?: string
  roles?: unknown
  role?: unknown
}

const AuthContext = createContext<AuthContextValue | null>(null)

function decodeJwtClaims(idToken: string): JwtClaims {
  const [, payload] = idToken.split('.')
  if (!payload) {
    throw new Error('Invalid identity token payload.')
  }

  const base64 = payload.replace(/-/g, '+').replace(/_/g, '/')
  const paddedBase64 = base64.padEnd(Math.ceil(base64.length / 4) * 4, '=')
  const decoded = window.atob(paddedBase64)

  try {
    return JSON.parse(decoded) as JwtClaims
  } catch {
    throw new Error('Invalid identity token claims payload.')
  }
}

function resolveRoleFromClaims(claims: JwtClaims): AppRole {
  const candidateRoles: string[] = []

  if (Array.isArray(claims.roles)) {
    candidateRoles.push(...claims.roles.filter((claimRole): claimRole is string => typeof claimRole === 'string'))
  }

  if (typeof claims.role === 'string') {
    candidateRoles.push(claims.role)
  }

  const matchedRole = candidateRoles.find((candidateRole): candidateRole is AppRole =>
    roles.includes(candidateRole as AppRole),
  )

  if (!matchedRole) {
    throw new Error('No supported Safe Harbor role claim was found in token.')
  }

  return matchedRole
}

function resolveEmailFromClaims(claims: JwtClaims): string {
  const email = claims.email ?? claims.preferred_username ?? claims.upn
  if (!email) {
    throw new Error('No email claim was found in token.')
  }

  return email
}

export function AuthProvider({ children }: PropsWithChildren) {
  const [session, setSession] = useState<AuthSession | null>(() => loadSession())

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      loginWithToken: (idToken) => {
        const claims = decodeJwtClaims(idToken)
        // Derive authorization role from JWT claims so route access remains aligned
        // with backend authorization rather than user-entered UI values.
        const role = resolveRoleFromClaims(claims)
        const email = resolveEmailFromClaims(claims)
        const nextSession = { email, role, idToken }
        setSession(nextSession)
        persistSession(nextSession)
        return role
      },
      logout: () => {
        setSession(null)
        persistSession(null)
      },
    }),
    [session],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const value = useContext(AuthContext)
  if (!value) {
    throw new Error('useAuth must be used within an AuthProvider')
  }

  return value
}
