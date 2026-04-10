import { useEffect, useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { registerLocalDevelopmentAccount, requestLocalDevelopmentToken } from '../services/localAuthApi'

type LocationState = { from?: { pathname?: string } } | null

type AuthMode = 'signin' | 'register'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { session, loginWithToken } = useAuth()
  const [mode, setMode] = useState<AuthMode>('signin')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [statusMessage, setStatusMessage] = useState<string | null>(null)
  const [error, setError] = useState<string | null>(null)

  const locationState = location.state as LocationState

  useEffect(() => {
    if (session) {
      const destination = locationState?.from?.pathname
      navigate(destination ?? '/app/dashboard', { replace: true })
    }
  }, [locationState, navigate, session])

  const normalizeCredentials = (): { email: string; password: string } | null => {
    const normalizedEmail = email.trim()
    const normalizedPassword = password.trim()

    if (!normalizedEmail) {
      setError('Please enter your email.')
      return null
    }

    if (!normalizedPassword) {
      setError('Please enter your password.')
      return null
    }

    return { email: normalizedEmail, password: normalizedPassword }
  }

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setStatusMessage(null)

    const credentials = normalizeCredentials()
    if (!credentials) {
      return
    }

    setIsSubmitting(true)

    try {
      if (mode === 'register') {
        const normalizedFirstName = firstName.trim()
        const normalizedLastName = lastName.trim()
        if (!normalizedFirstName) {
          setError('Please enter your first name.')
          setIsSubmitting(false);
          return
        }
        if (!normalizedLastName) {
          setError('Please enter your last name.')
          setIsSubmitting(false);
          return
        }

        // Keep account creation in the same screen as sign-in so deployed users who start from
        // /login can recover immediately without guessing a hidden route.
        await registerLocalDevelopmentAccount({
          email: credentials.email,
          password: credentials.password,
          firstName: normalizedFirstName,
          lastName: normalizedLastName,
        })

        setStatusMessage('Account created successfully. You can sign in now.')
        setMode('signin')
      }
      
      else {
        // Keep frontend login behavior on backend-issued JWTs so browser routing and
        // API authorization both rely on the same token source.
        const idToken = await requestLocalDevelopmentToken(credentials.email, credentials.password)
        const roleFromToken = loginWithToken(idToken)
        const destination = locationState?.from?.pathname
        navigate(destination ?? (roleFromToken === 'Donor' ? '/donor/dashboard' : '/app/dashboard'), { replace: true })
      }
    } catch (authException) {
      setError(authException instanceof Error ? authException.message : 'Unable to continue.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <section aria-labelledby="login-title" className="auth-layout">
      <div className="auth-card">
        <h1 id="login-title">{mode === 'signin' ? 'Sign in' : 'Create account'}</h1>
        <p className="caption">
          {mode === 'signin'
            ? 'Use your email and password to continue.'
            : 'Create a new Safe Harbor account using your name, email, and password.'}
        </p>

        {statusMessage && <p className="form-success">{statusMessage}</p>}

        {error && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}

        <form onSubmit={handleSubmit} className="auth-form">
          <label htmlFor="email">Email</label>
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
            autoComplete={mode === 'signin' ? 'current-password' : 'new-password'}
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />

          {mode === 'register' && (
            <>
              <label htmlFor="firstName">First name</label>
              <input
                id="firstName"
                name="firstName"
                autoComplete="given-name"
                value={firstName}
                onChange={(event) => setFirstName(event.target.value)}
              />

              <label htmlFor="lastName">Last name</label>
              <input
                id="lastName"
                name="lastName"
                autoComplete="family-name"
                value={lastName}
                onChange={(event) => setLastName(event.target.value)}
              />
            </>
          )}

          <button type="submit" className="button button-primary" disabled={isSubmitting}>
            {isSubmitting ? 'Please wait…' : mode === 'signin' ? 'Sign in' : 'Create account'}
          </button>
        </form>

        <button
          type="button"
          className="button button-secondary auth-mode-toggle"
          onClick={() => {
            setMode((currentMode) => (currentMode === 'signin' ? 'register' : 'signin'))
            setError(null)
            setStatusMessage(null)
          }}
        >
          {mode === 'signin' ? 'Need an account? Create one' : 'Already have an account? Sign in'}
        </button>
      </div>
    </section>
  )
}
