import { useMemo, useState } from 'react'
import {
  COOKIE_CONSENT_STORAGE_KEY,
  type CookieConsentDecision,
  type CookieConsentState
} from '../models/cookieConsent'

function parseStoredState(raw: string | null): CookieConsentState | null {
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as CookieConsentState
    if (!parsed.decision || !parsed.decidedAtIso || !parsed.policyVersion) {
      return null
    }

    return parsed
  } catch {
    return null
  }
}

export function useCookieConsent(policyVersion: string) {
  const [state, setState] = useState<CookieConsentState | null>(() => {
    return parseStoredState(window.localStorage.getItem(COOKIE_CONSENT_STORAGE_KEY))
  })

  const hasDecision = useMemo(() => {
    return state?.policyVersion === policyVersion
  }, [state, policyVersion])

  const saveDecision = (decision: CookieConsentDecision) => {
    const nextState: CookieConsentState = {
      decision,
      decidedAtIso: new Date().toISOString(),
      policyVersion
    }

    window.localStorage.setItem(COOKIE_CONSENT_STORAGE_KEY, JSON.stringify(nextState))
    setState(nextState)
  }

  return {
    hasDecision,
    state,
    accept: () => saveDecision('accepted'),
    decline: () => saveDecision('declined')
  }
}
