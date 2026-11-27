import type { UserProfile } from '../types/identity'

const STORAGE_KEY = 'bms.auth'

export interface StoredAuth {
  accessToken: string
  expiresAt: string
  user: UserProfile
}

function isExpired(expiresAt: string): boolean {
  const time = Date.parse(expiresAt)
  return Number.isFinite(time) && time <= Date.now()
}

export function loadAuth(): StoredAuth | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw) as StoredAuth
    if (!parsed?.accessToken || !parsed.user || isExpired(parsed.expiresAt)) {
      localStorage.removeItem(STORAGE_KEY)
      return null
    }
    return parsed
  } catch {
    localStorage.removeItem(STORAGE_KEY)
    return null
  }
}

export function saveAuth(auth: StoredAuth): void {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(auth))
}

export function clearAuth(): void {
  localStorage.removeItem(STORAGE_KEY)
}

export function getAccessToken(): string | null {
  return loadAuth()?.accessToken ?? null
}
