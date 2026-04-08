import type { AppRole } from '../auth/authSession'

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? ''
const LOCAL_LOGIN_ENDPOINT = '/api/auth/local-login'

type LocalLoginResponse = {
  idToken: string
}

/**
 * Development-only helper that exchanges an email+role selection for a signed JWT
 * from the backend. This lets local testing exercise real bearer-token plumbing.
 */
export async function requestLocalDevelopmentToken(email: string, role: AppRole): Promise<string> {
  const response = await fetch(`${API_BASE}${LOCAL_LOGIN_ENDPOINT}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      Accept: 'application/json',
    },
    body: JSON.stringify({ email, role }),
  })

  if (!response.ok) {
    const errorBody = (await response.json().catch(() => ({}))) as { error?: string }
    throw new Error(errorBody.error ?? `Local login failed with status ${response.status}`)
  }

  const body = (await response.json()) as LocalLoginResponse
  return body.idToken
}
