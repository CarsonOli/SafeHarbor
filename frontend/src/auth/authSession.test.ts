import { beforeEach, describe, expect, it } from 'vitest'
import { loadSession, normalizeRoleToAppRole, persistSession } from './authSession'

describe('authSession storage contract', () => {
  beforeEach(() => {
    window.localStorage.clear()
  })

  it('round-trips a valid session payload', () => {
    persistSession({ email: 'case.worker@safeharbor.org', role: 'Admin' })

    const session = loadSession()

    expect(session).toEqual({ email: 'case.worker@safeharbor.org', role: 'Admin' })
  })

  it('returns null when local storage payload is malformed', () => {
    window.localStorage.setItem('safeharbor.auth.session', '{"role":"Admin"}')

    const session = loadSession()

    expect(session).toBeNull()
  })

  it('maps persisted database roles to app roles for backward-compatible authorization', () => {
    window.localStorage.setItem(
      'safeharbor.auth.session',
      JSON.stringify({ email: 'legacy.staff@safeharbor.org', role: 'staff' }),
    )

    const session = loadSession()

    expect(session).toEqual({ email: 'legacy.staff@safeharbor.org', role: 'SocialWorker' })
  })

  it('normalizes both app and database role values', () => {
    expect(normalizeRoleToAppRole('Admin')).toBe('Admin')
    expect(normalizeRoleToAppRole('user')).toBe('Donor')
    expect(normalizeRoleToAppRole('unknown')).toBeNull()
  })
})
