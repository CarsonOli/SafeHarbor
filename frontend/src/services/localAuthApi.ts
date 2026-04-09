import type { AppRole } from '../auth/authSession'

const LOCAL_LOGIN_ENDPOINT = '/api/auth/local-login'
const LOCAL_REGISTER_ENDPOINT = '/api/auth/local-register'

type LocalLoginResponse = {
  idToken: string
}

type LocalRegisterRequest = {
  email: string
  role: AppRole
  password: string
}

function resolveApiBaseCandidates(): string[] {
  const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL
  if (configuredBaseUrl) {
    return [configuredBaseUrl]
  }

  if (import.meta.env.DEV) {
    // NOTE: We try same-origin first so teams with a local reverse-proxy keep working.
    // If that returns 404 from the Vite dev server, we fall back to common ASP.NET local ports.
    // Prefer :7217 first to avoid HTTP->HTTPS redirect preflight issues in browsers.
    // Keep :5264 as a secondary fallback for devs who disable TLS locally.
    return ['', 'https://localhost:7217', 'http://localhost:5264', 'http://localhost:5000']
  }

  return ['']
}

async function readApiError(response: Response, fallbackMessage: string): Promise<Error> {
  const errorBody = (await response.json().catch(() => ({}))) as { error?: string }
  return new Error(errorBody.error ?? fallbackMessage)
}

async function postLocalAuthJson(endpoint: string, payload: object): Promise<Response> {
  const baseCandidates = resolveApiBaseCandidates()
  let hadNetworkFailure = false

  for (const baseUrl of baseCandidates) {
    try {
      const response = await fetch(`${baseUrl}${endpoint}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
        },
        body: JSON.stringify(payload),
      })

      if (response.status === 404 && baseUrl === '' && import.meta.env.DEV) {
        // In local Vite development, a 404 on same-origin usually means "/api" hit the frontend
        // server instead of the backend. Continue to explicit backend URL fallbacks.
        continue
      }

      return response
    } catch {
      hadNetworkFailure = true
    }
  }

  if (hadNetworkFailure) {
    const attemptedHosts = baseCandidates.map((baseUrl) => (baseUrl || window.location.origin)).join(', ')
    throw new Error(
      `Unable to reach local auth server. Start the backend API and/or set VITE_API_BASE_URL. Tried: ${attemptedHosts}`
    )
  }

  throw new Error('Unable to reach local auth server.')
}

/**
 * Development-only helper that exchanges an email+role selection for a signed JWT
 * from the backend. This lets local testing exercise real bearer-token plumbing.
 */
export async function requestLocalDevelopmentToken(email: string, role: AppRole, password: string): Promise<string> {
  const response = await postLocalAuthJson(LOCAL_LOGIN_ENDPOINT, { email, role, password })

  if (!response.ok) {
    throw await readApiError(response, `Local login failed with status ${response.status}`)
  }

  const body = (await response.json()) as LocalLoginResponse
  return body.idToken
}

/**
 * Creates a local-development account that can later request JWTs via /local-login.
 * The backend stores accounts in-memory only, so this is intentionally local and ephemeral.
 */
export async function registerLocalDevelopmentAccount(request: LocalRegisterRequest): Promise<void> {
  const response = await postLocalAuthJson(LOCAL_REGISTER_ENDPOINT, request)

  if (!response.ok) {
    throw await readApiError(response, `Local account creation failed with status ${response.status}`)
  }
}
