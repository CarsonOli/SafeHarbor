const LOGIN_ENDPOINT = '/api/auth/login'
const LOCAL_REGISTER_ENDPOINT = '/api/auth/register'
const STATIC_WEB_APP_FALLBACK_API_HOSTS = [
  'https://safeharborbackend-ggdyhzdggag9d3df.canadacentral-01.azurewebsites.net',
  'https://safeharbor-api-staging.azurewebsites.net',
]

type LocalLoginResponse = {
  idToken: string
}

type LocalRegisterRequest = {
  email: string
  firstName: string
  lastName: string
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

  // In deployed Azure Static Web Apps, frontend and backend may live on separate hosts.
  // Keep same-origin first for environments with an API proxy, then fall back to known
  // App Service hosts so auth flows still work when proxy wiring/env config is absent.
  if (typeof window !== 'undefined' && window.location.hostname.endsWith('.azurestaticapps.net')) {
    // NOTE: Prefer explicit backend hosts first to avoid 405s from the static host
    // when no API proxy is configured for /api/*.
    return [...STATIC_WEB_APP_FALLBACK_API_HOSTS, '']
  }

  return ['']
}

async function readApiError(response: Response, fallbackMessage: string): Promise<Error> {
  const errorBody = (await response.json().catch(() => ({}))) as {
    error?: string
    Error?: string
    message?: string
    Message?: string
    errorCode?: string
    ErrorCode?: string
  }

  const message =
    errorBody.error ??
    errorBody.Error ??
    errorBody.message ??
    errorBody.Message ??
    fallbackMessage

  return new Error(message)
}

async function postLocalAuthJson(endpoint: string, payload: object): Promise<Response> {
  const baseCandidates = resolveApiBaseCandidates()
  let hadNetworkFailure = false

  for (const baseUrl of baseCandidates) {
    let response: Response

    try {
      response = await fetch(`${baseUrl}${endpoint}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Accept: 'application/json',
        },
        body: JSON.stringify(payload),
      })
    } catch {
      hadNetworkFailure = true
      continue
    }

    if (response.status === 404 && baseUrl === '' && import.meta.env.DEV) {
      // In local Vite development, a 404 on same-origin usually means "/api" hit the frontend
      // server instead of the backend. Continue to explicit backend URL fallbacks.
      continue
    }

    if ((response.status === 404 || response.status === 405) && baseUrl === '' && !import.meta.env.DEV) {
      if (baseCandidates.length > 1) {
        // Preserve compatibility with environments that provide a secondary API host fallback.
        continue
      }

      // In deployed environments a relative "/api/*" call should only be used when the frontend host
      // actually proxies API traffic. A 404/405 here usually means the request hit static hosting
      // instead of the backend API, so include actionable guidance in the surfaced error.
      throw new Error(
        `Auth endpoint ${endpoint} is not reachable on this frontend origin (HTTP ${response.status}). ` +
          'Set VITE_API_BASE_URL to the backend URL or configure frontend hosting to proxy /api/* to the API.'
      )
    }

    return response
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
 * Exchanges email/password credentials for a signed JWT
 * from the backend. This keeps frontend auth storage aligned with backend token issuance.
 */
export async function requestLocalDevelopmentToken(email: string, password: string): Promise<string> {
  const response = await postLocalAuthJson(LOGIN_ENDPOINT, { email, password })

  if (!response.ok) {
    throw await readApiError(response, `Local login failed with status ${response.status}`)
  }

  const body = (await response.json()) as LocalLoginResponse
  return body.idToken
}

/**
 * Creates a local-development account that can later request JWTs via /api/auth/login.
 * The backend stores accounts in-memory only, so this is intentionally local and ephemeral.
 */
export async function registerLocalDevelopmentAccount(request: LocalRegisterRequest): Promise<void> {
  const response = await postLocalAuthJson(LOCAL_REGISTER_ENDPOINT, request)

  if (!response.ok) {
    throw await readApiError(response, `Local account creation failed with status ${response.status}`)
  }
}
