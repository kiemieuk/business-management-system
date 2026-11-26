import type { ReactNode } from 'react'
import { Navigate, useLocation } from 'react-router-dom'
import { useAuth } from './authContextValue'

interface ProtectedRouteProps {
  children: ReactNode
  /** Optional permission code required to view the route. */
  permission?: string
}

export function ProtectedRoute({ children, permission }: ProtectedRouteProps) {
  const { isAuthenticated, hasPermission } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" replace state={{ from: location.pathname + location.search }} />
  }

  if (permission && !hasPermission(permission)) {
    return <NotAuthorized />
  }

  return <>{children}</>
}

export function NotAuthorized() {
  return (
    <div className="card status-view">
      <h1>403 - Not authorized</h1>
      <p className="muted">You do not have permission to view this page.</p>
    </div>
  )
}
