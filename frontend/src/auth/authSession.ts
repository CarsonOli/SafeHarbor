// Available authentication roles.
// 'Admin' and 'SocialWorker' are internal staff roles that access /app/* routes.
// 'Donor' is for external donors who only access /donor/dashboard.
// 'Viewer' has been replaced by 'Donor' — if a read-only staff role is needed in future,
// add it back here alongside 'Donor'.
export const roles = ['Admin', 'SocialWorker', 'Donor'] as const

export type AppRole = (typeof roles)[number]
export type DatabaseRole = 'admin' | 'staff' | 'user'

const databaseToAppRoleMap: Record<DatabaseRole, AppRole> = {
  admin: 'Admin',
  staff: 'SocialWorker',
  user: 'Donor',
}

const appToDatabaseRoleMap: Record<AppRole, DatabaseRole> = {
  Admin: 'admin',
  SocialWorker: 'staff',
  Donor: 'user',
}

export type AuthSession = {
  email: string
  role: AppRole
  idToken?: string
}

const AUTH_KEY = 'safeharbor.auth.session'

export function normalizeRoleToAppRole(role: unknown): AppRole | null {
  if (typeof role !== 'string') {
    return null
  }

  // NOTE: localStorage and JWTs may contain legacy DB roles from prior clients.
  // Normalize to the app role contract to keep route guards and navbar checks consistent.
  if (roles.includes(role as AppRole)) {
    return role as AppRole
  }

  const mappedRole = databaseToAppRoleMap[role as DatabaseRole]
  return mappedRole ?? null
}

export function mapAppRoleToDatabaseRole(role: AppRole): DatabaseRole {
  return appToDatabaseRoleMap[role]
}

export function loadSession(): AuthSession | null {
  const value = window.localStorage.getItem(AUTH_KEY)
  if (!value) {
    return null
  }

  try {
    const parsed = JSON.parse(value) as AuthSession
    const normalizedRole = normalizeRoleToAppRole(parsed.role)
    if (!parsed.email || !normalizedRole) {
      return null
    }

    // Preserve backward compatibility with older localStorage payloads while still
    // validating shape for new auth flows that persist an ID token.
    if (parsed.idToken !== undefined && typeof parsed.idToken !== 'string') {
      return null
    }

    return {
      ...parsed,
      role: normalizedRole,
    }
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
