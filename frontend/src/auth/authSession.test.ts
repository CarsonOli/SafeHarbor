import { beforeEach, describe, expect, it } from 'vitest'
import { loadSession, persistSession } from './authSession'

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
})
