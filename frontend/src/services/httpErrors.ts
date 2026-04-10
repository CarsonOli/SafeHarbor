export class HttpError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.name = 'HttpError'
    this.status = status
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

  if (error instanceof Error && error.message.trim().length > 0) {
    return error.message
  }

  return fallbackMessage
}
