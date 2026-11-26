export interface ProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  errors?: Record<string, string[]>
}

export class ApiError extends Error {
  readonly status: number
  readonly problem: ProblemDetails | null

  constructor(status: number, problem: ProblemDetails | null, fallback?: string) {
    super(problem?.detail || problem?.title || fallback || `Request failed with status ${status}`)
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }

  /** Flattened list of validation error messages (if any). */
  get validationErrors(): string[] {
    const errors = this.problem?.errors
    if (!errors) return []
    return Object.entries(errors).flatMap(([field, messages]) =>
      messages.map((m) => (field ? `${field}: ${m}` : m)),
    )
  }
}

/** Builds human-readable messages from any thrown error. */
export function getErrorMessages(error: unknown): string[] {
  if (error instanceof ApiError) {
    const validation = error.validationErrors
    if (validation.length > 0) {
      const head = error.problem?.detail ? [error.problem.detail] : []
      return [...head, ...validation]
    }
    return [error.message]
  }
  if (error instanceof Error) return [error.message]
  return ['An unexpected error occurred.']
}

const API_BASE = '/api'
const AUTH_SCHEME = 'Bearer'

type TokenProvider = () => string | null
type UnauthorizedHandler = () => void

let tokenProvider: TokenProvider = () => null
let unauthorizedHandler: UnauthorizedHandler = () => {}

export function configureApiClient(options: {
  getToken: TokenProvider
  onUnauthorized: UnauthorizedHandler
}): void {
  tokenProvider = options.getToken
  unauthorizedHandler = options.onUnauthorized
}

export interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE'
  body?: unknown
  /** Skip the Authorization header and 401 handling (e.g. for login). */
  anonymous?: boolean
  signal?: AbortSignal
}

async function parseBody(response: Response): Promise<unknown> {
  const text = await response.text()
  if (!text) return null
  const contentType = response.headers.get('content-type') ?? ''
  if (contentType.includes('json')) {
    try {
      return JSON.parse(text)
    } catch {
      return null
    }
  }
  return text
}

export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' }
  if (options.body !== undefined) headers['Content-Type'] = 'application/json'

  if (!options.anonymous) {
    const token = tokenProvider()
    if (token) headers.Authorization = `${AUTH_SCHEME} ${token}`
  }

  let response: Response
  try {
    response = await fetch(`${API_BASE}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
      signal: options.signal,
    })
  } catch (err) {
    if (err instanceof DOMException && err.name === 'AbortError') throw err
    throw new ApiError(0, null, 'Unable to reach the server. Please check your connection.')
  }

  const data = await parseBody(response)

  if (!response.ok) {
    if (response.status === 401 && !options.anonymous) {
      unauthorizedHandler()
    }
    const problem = data && typeof data === 'object' ? (data as ProblemDetails) : null
    const fallback =
      typeof data === 'string' && data.length < 300 ? data : response.statusText || undefined
    throw new ApiError(response.status, problem, fallback)
  }

  return data as T
}

export const api = {
  get: <T>(path: string, signal?: AbortSignal) => apiRequest<T>(path, { signal }),
  post: <T>(path: string, body?: unknown, options?: Omit<RequestOptions, 'method' | 'body'>) =>
    apiRequest<T>(path, { ...options, method: 'POST', body }),
  put: <T>(path: string, body?: unknown) => apiRequest<T>(path, { method: 'PUT', body }),
  delete: <T = void>(path: string) => apiRequest<T>(path, { method: 'DELETE' }),
}
