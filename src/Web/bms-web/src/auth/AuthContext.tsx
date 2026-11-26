import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { configureApiClient } from '../api/client'
import { authApi } from '../api/identity'
import type { UserProfile } from '../types/identity'
import { AuthContext, type AuthContextValue } from './authContextValue'
import { clearAuth, getAccessToken, loadAuth, saveAuth } from './authStorage'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<UserProfile | null>(() => loadAuth()?.user ?? null)

  const logout = useCallback(() => {
    clearAuth()
    setUser(null)
  }, [])

  useEffect(() => {
    // On 401 the stored session is cleared; ProtectedRoute then redirects to /login.
    configureApiClient({ getToken: getAccessToken, onUnauthorized: logout })
  }, [logout])

  // Expire the session client-side when the token lifetime ends.
  useEffect(() => {
    if (!user) return
    const stored = loadAuth()
    const remaining = stored ? Date.parse(stored.expiresAt) - Date.now() : 0
    if (!Number.isFinite(remaining)) return
    const timer = window.setTimeout(logout, Math.min(Math.max(remaining, 0), 2_147_483_647))
    return () => window.clearTimeout(timer)
  }, [user, logout])

  // Keep other tabs in sync.
  useEffect(() => {
    const onStorage = () => setUser(loadAuth()?.user ?? null)
    window.addEventListener('storage', onStorage)
    return () => window.removeEventListener('storage', onStorage)
  }, [])

  const login = useCallback(async (username: string, password: string) => {
    const response = await authApi.login({ username, password })
    saveAuth({ accessToken: response.accessToken, expiresAt: response.expiresAt, user: response.user })
    setUser(response.user)
  }, [])

  const refreshProfile = useCallback(async () => {
    const stored = loadAuth()
    if (!stored) return
    const profile = await authApi.me()
    saveAuth({ ...stored, user: profile })
    setUser(profile)
  }, [])

  // Refresh roles/permissions from the server once on startup.
  useEffect(() => {
    const stored = loadAuth()
    if (!stored) return
    let cancelled = false
    authApi
      .me()
      .then((profile) => {
        if (cancelled) return
        saveAuth({ ...stored, user: profile })
        setUser(profile)
      })
      .catch(() => {})
    return () => {
      cancelled = true
    }
  }, [])

  const hasPermission = useCallback(
    (code: string) => !!user && user.permissions.includes(code),
    [user],
  )

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: !!user, login, logout, refreshProfile, hasPermission }),
    [user, login, logout, refreshProfile, hasPermission],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
