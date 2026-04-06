export type CookieConsentDecision = 'accepted' | 'declined'

export interface CookieConsentState {
  decision: CookieConsentDecision
  decidedAtIso: string
  policyVersion: string
}

export const COOKIE_CONSENT_STORAGE_KEY = 'safeharbor.cookie-consent'
