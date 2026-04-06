import { createContext, useContext, useMemo, useState, type PropsWithChildren } from 'react'
import { type AppRole, type AuthSession, loadSession, persistSession } from './authSession'

type AuthContextValue = {
  session: AuthSession | null
  login: (email: string, role: AppRole) => void
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: PropsWithChildren) {
  const [session, setSession] = useState<AuthSession | null>(() => loadSession())

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      login: (email, role) => {
        const nextSession = { email, role }
        setSession(nextSession)
        persistSession(nextSession)
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
