// Available authentication roles.
// 'Admin' and 'SocialWorker' are internal staff roles that access /app/* routes.
// 'Donor' is for external donors who only access /donor/dashboard.
// 'Viewer' has been replaced by 'Donor' — if a read-only staff role is needed in future,
// add it back here alongside 'Donor'.
export const roles = ['Admin', 'SocialWorker', 'Donor'] as const

export type AppRole = (typeof roles)[number]

export type AuthSession = {
  email: string
  role: AppRole
  idToken?: string
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

    // Preserve backward compatibility with older localStorage payloads while still
    // validating shape for new auth flows that persist an ID token.
    if (parsed.idToken !== undefined && typeof parsed.idToken !== 'string') {
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
