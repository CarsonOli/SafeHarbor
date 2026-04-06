import { useState, type FormEvent } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'
import { roles, type AppRole } from '../auth/authSession'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [role, setRole] = useState<AppRole>('Viewer')
  const [error, setError] = useState<string | null>(null)

  const handleCredentialsSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!email.trim()) {
      setError('Please enter your work email.')
      return
    }

    login(email.trim(), role)
    const destination = (location.state as { from?: { pathname?: string } } | null)?.from?.pathname
    navigate(destination ?? '/app/dashboard', { replace: true })
  }

  return (
    <section aria-labelledby="login-title" className="auth-layout">
      <div className="auth-card">
        <h1 id="login-title">Team login</h1>
        <p className="caption">Use your work identity to access authenticated operational modules.</p>

        {error && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}

        <form onSubmit={handleCredentialsSubmit} className="auth-form">
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

          <button type="submit" className="button button-primary">
            Sign in
          </button>
        </form>
      </div>
    </section>
  )
}
