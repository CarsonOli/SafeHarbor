import { useEffect, useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { requestLocalDevelopmentToken } from '../services/localAuthApi'

type LocationState = { from?: { pathname?: string } } | null

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { session, loginWithToken } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)

  const locationState = location.state as LocationState

  useEffect(() => {
    if (session) {
      const destination = locationState?.from?.pathname
      navigate(destination ?? '/app/dashboard', { replace: true })
    }
  }, [locationState, navigate, session])

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)

    if (!email.trim()) {
      setError('Please enter your work email.')
      return
    }
    if (!password.trim()) {
      setError('Please enter your password.')
      return
    }

    try {
      const normalizedEmail = email.trim()
      // Keep frontend login behavior on backend-issued JWTs so browser routing and
      // API authorization both rely on the same token source.
      const idToken = await requestLocalDevelopmentToken(normalizedEmail, password.trim())
      const roleFromToken = loginWithToken(idToken)
      const destination = locationState?.from?.pathname
      navigate(destination ?? (roleFromToken === 'Donor' ? '/donor/dashboard' : '/app/dashboard'), { replace: true })
    } catch (authException) {
      setError(authException instanceof Error ? authException.message : 'Unable to sign in.')
    }
  }

  return (
    <section aria-labelledby="login-title" className="auth-layout">
      <div className="auth-card">
        <h1 id="login-title">Sign in</h1>
        <p className="caption">Use your Safe Harbor email and password to continue.</p>

        {error && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}

        <form onSubmit={handleSubmit} className="auth-form">
          <label htmlFor="email">Work email</label>
          <input
            id="email"
            name="email"
            autoComplete="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
          />

          <label htmlFor="password">Password</label>
          <input
            id="password"
            name="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />

          <button type="submit" className="button button-primary">
            Sign in
          </button>
        </form>
      </div>
    </section>
  )
}
