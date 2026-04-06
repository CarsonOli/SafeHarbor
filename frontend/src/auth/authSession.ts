export const roles = ['Admin', 'SocialWorker', 'Viewer'] as const

export type AppRole = (typeof roles)[number]

export type AuthSession = {
  email: string
  role: AppRole
}

const AUTH_KEY = 'safeharbor.auth.session'

export function loadSession(): AuthSession | null {
  const value = window.localStorage.getItem(AUTH_KEY)
  if (!value) {
    return null
  }

  try {
    const parsed = JSON.parse(value) as AuthSession
    if (!parsed.email || !roles.includes(parsed.role)) {
      return null
    }

    return parsed
  } catch {
    return null
  }
}

export function persistSession(session: AuthSession | null): void {
  if (!session) {
    window.localStorage.removeItem(AUTH_KEY)
    return
  }

  window.localStorage.setItem(AUTH_KEY, JSON.stringify(session))
}
