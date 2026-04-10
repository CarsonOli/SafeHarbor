interface ApiErrorNoticeProps {
  error: string
}

/**
 * Standardized API error block for /app pages.
 *
 * Why: centralizes endpoint + status detail rendering so every application page
 * communicates backend failures consistently during triage and QA.
 */
export function ApiErrorNotice({ error }: ApiErrorNoticeProps) {
  return (
    <p role="alert">
      API request failed: {error}
    </p>
  )
}
