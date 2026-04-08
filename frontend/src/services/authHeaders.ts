import { loadSession } from '../auth/authSession'

/**
 * Builds request headers that include the current bearer token when available.
 *
 * Keeping this in one helper preserves the existing service-layer architecture
 * and avoids duplicating auth-header logic across every API client file.
 */
export function buildAuthHeaders(baseHeaders?: HeadersInit): Headers {
  const headers = new Headers(baseHeaders)
  const idToken = loadSession()?.idToken

  if (idToken) {
    headers.set('Authorization', `Bearer ${idToken}`)
  }

  return headers
}
