import { useEffect, useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { roles, type AppRole } from '../auth/authSession'

type LocationState = { from?: { pathname?: string } } | null

type IdentityProviderState = {
  fromPath?: string
}

const IDENTITY_PROVIDER_CONFIGURED =
  Boolean(import.meta.env.VITE_AUTH_AUTHORIZE_URL) && Boolean(import.meta.env.VITE_AUTH_CLIENT_ID)

const DEV_ROLE_SIMULATION_ENABLED =
  import.meta.env.DEV &&
  // Keep explicit opt-in support while also unblocking local login flows when IdP env vars are absent.
  (import.meta.env.VITE_ENABLE_DEV_ROLE_SIMULATION === 'true' || !IDENTITY_PROVIDER_CONFIGURED)

function defaultDestinationForRole(role: AppRole): string {
  return role === 'Donor' ? '/donor/dashboard' : '/app/dashboard'
}

function decodeProviderState(encodedState: string | null): IdentityProviderState {
  if (!encodedState) {
    return {}
  }

  try {
    return JSON.parse(window.atob(encodedState)) as IdentityProviderState
  } catch {
    return {}
  }
}

function buildIdentityProviderAuthorizeUrl(fromPath?: string): string {
  const authorizeEndpoint = import.meta.env.VITE_AUTH_AUTHORIZE_URL
  const clientId = import.meta.env.VITE_AUTH_CLIENT_ID
  const redirectUri = import.meta.env.VITE_AUTH_REDIRECT_URI ?? `${window.location.origin}/login`
  const scope = import.meta.env.VITE_AUTH_SCOPE ?? 'openid profile email'

  if (!authorizeEndpoint || !clientId) {
    throw new Error(
      'Identity provider settings are missing. Add VITE_AUTH_AUTHORIZE_URL and VITE_AUTH_CLIENT_ID to frontend/.env.local.'
    )
  }

  const params = new URLSearchParams({
    client_id: clientId,
    response_type: 'id_token',
    redirect_uri: redirectUri,
    scope,
    response_mode: 'fragment',
    state: window.btoa(JSON.stringify({ fromPath })),
  })

  return `${authorizeEndpoint}?${params.toString()}`
}

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { session, loginWithIdentityToken, loginForDevelopment } = useAuth()
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<AppRole>('Admin')
  const [error, setError] = useState<string | null>(null)

  const locationState = location.state as LocationState

  useEffect(() => {
    if (session) {
      const destination = locationState?.from?.pathname
      navigate(destination ?? defaultDestinationForRole(session.role), { replace: true })
    }
  }, [locationState, navigate, session])

  useEffect(() => {
    const hashParams = new URLSearchParams(window.location.hash.replace(/^#/, ''))
    const idToken = hashParams.get('id_token')
    const authError = hashParams.get('error_description') ?? hashParams.get('error')

    if (!idToken && !authError) {
      return
    }

    if (authError) {
      setError(authError)
      return
    }
    if (!idToken) {
      return
    }

    try {
      const roleFromToken = loginWithIdentityToken(idToken)
      const providerState = decodeProviderState(hashParams.get('state'))
      const destination = locationState?.from?.pathname ?? providerState.fromPath
      window.history.replaceState(null, document.title, window.location.pathname + window.location.search)
      navigate(destination ?? defaultDestinationForRole(roleFromToken), { replace: true })
    } catch (authException) {
      setError(authException instanceof Error ? authException.message : 'Unable to sign in with identity provider.')
    }
  }, [locationState, loginWithIdentityToken, navigate])

  const handleIdentityProviderSignIn = () => {
    setError(null)

    if (!IDENTITY_PROVIDER_CONFIGURED) {
      setError('Identity provider settings are missing. Configure frontend/.env.local or use Development role simulation below.')
      return
    }

    try {
      const fromPath = locationState?.from?.pathname
      window.location.assign(buildIdentityProviderAuthorizeUrl(fromPath))
    } catch (authException) {
      setError(authException instanceof Error ? authException.message : 'Unable to start identity provider sign-in.')
    }
  }

  const handleDevelopmentSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!email.trim()) {
      setError('Please enter your work email.')
      return
    }

    loginForDevelopment(email.trim(), role)
    const destination = locationState?.from?.pathname
    navigate(destination ?? defaultDestinationForRole(role), { replace: true })
  }

  return (
    <section aria-labelledby="login-title" className="auth-layout">
      <div className="auth-card">
        <h1 id="login-title">Sign in</h1>
        <p className="caption">Use your organization identity-provider account to continue.</p>

        {error && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}

        <button
          type="button"
          className="button button-primary"
          onClick={handleIdentityProviderSignIn}
          disabled={!IDENTITY_PROVIDER_CONFIGURED}
          title={!IDENTITY_PROVIDER_CONFIGURED ? 'Set VITE_AUTH_AUTHORIZE_URL and VITE_AUTH_CLIENT_ID in frontend/.env.local.' : undefined}
        >
          Sign in with Identity Provider
        </button>

        {DEV_ROLE_SIMULATION_ENABLED && (
          <>
            {/*
              Development-only login simulation exists to unblock local route testing when
              an IdP tenant is unavailable in offline/local environments.
            */}
            <hr aria-hidden="true" />
            <p className="caption">Development role simulation (VITE_ENABLE_DEV_ROLE_SIMULATION=true)</p>

            <form onSubmit={handleDevelopmentSubmit} className="auth-form">
              <label htmlFor="email">Work email</label>
              <input
                id="email"
                name="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
              />

              <label htmlFor="role">Role</label>
              <select id="role" value={role} onChange={(event) => setRole(event.target.value as AppRole)}>
                {roles.map((roleOption) => (
                  <option key={roleOption} value={roleOption}>
                    {roleOption}
                  </option>
                ))}
              </select>

              <button type="submit" className="button button-secondary">
                Simulate sign-in
              </button>
            </form>
          </>
        )}
      </div>
    </section>
  )
}
