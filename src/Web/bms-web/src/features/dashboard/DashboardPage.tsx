import { useAuth } from '../../auth/authContextValue'

export function DashboardPage() {
  const { user } = useAuth()
  if (!user) return null

  return (
    <div className="page">
      <h1>Welcome, {user.fullName || user.username}</h1>
      <p className="muted">
        Signed in as <strong>{user.username}</strong> ({user.email})
      </p>
      <div className="grid-2">
        <section className="card">
          <h2>Your roles</h2>
          {user.roles.length === 0 ? (
            <p className="muted">No roles assigned.</p>
          ) : (
            <div className="tags">
              {user.roles.map((r) => (
                <span className="tag" key={r}>
                  {r}
                </span>
              ))}
            </div>
          )}
        </section>
        <section className="card">
          <h2>Your permissions</h2>
          {user.permissions.length === 0 ? (
            <p className="muted">No permissions granted.</p>
          ) : (
            <ul className="plain-list">
              {[...user.permissions].sort().map((p) => (
                <li key={p}>
                  <code>{p}</code>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </div>
  )
}
