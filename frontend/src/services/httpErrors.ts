export class HttpError extends Error {
  readonly status: number
  readonly endpoint?: string
  readonly method?: string

  constructor(status: number, message: string, options?: { endpoint?: string; method?: string }) {
    super(message)
    this.name = 'HttpError'
    this.status = status
    this.endpoint = options?.endpoint
    this.method = options?.method
  }
}

export const NOT_AUTHORIZED_MESSAGE =
  'Not authorized: your current role does not have permission to access this resource.'

/**
 * Maps HTTP failures into a user-facing message.
 * Keep this centralized so all pages render authorization failures consistently.
 */
export function toUserFacingError(error: unknown, fallbackMessage: string): string {
  if (error instanceof HttpError && error.status === 403) {
    return NOT_AUTHORIZED_MESSAGE
  }

  if (error instanceof HttpError) {
    const method = error.method ?? 'REQUEST'
    const endpoint = error.endpoint ?? 'unknown endpoint'
    return `${fallbackMessage} (${method} ${endpoint} → HTTP ${error.status})`
  }

  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message
  }

  return fallbackMessage
}
