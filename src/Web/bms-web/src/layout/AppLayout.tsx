import { Link, NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../auth/authContextValue'
import { navigation } from './navigation'

export function AppLayout() {
  const { user, logout, hasPermission } = useAuth()

  const sections = navigation
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => !item.permission || hasPermission(item.permission)),
    }))
    .filter((section) => section.items.length > 0)

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">BMS</div>
        <nav>
          {sections.map((section, i) => (
            <div className="nav-section" key={section.title ?? i}>
              {section.title && <div className="nav-section-title">{section.title}</div>}
              {section.items.map((item) => (
                <NavLink
                  key={item.path}
                  to={item.path}
                  end={item.path === '/'}
                  className={({ isActive }) => (isActive ? 'nav-link active' : 'nav-link')}
                >
                  {item.label}
                </NavLink>
              ))}
            </div>
          ))}
        </nav>
      </aside>
      <div className="main">
        <header className="header">
          <div className="header-title">Business Management System</div>
          <div className="header-user">
            <span title={user?.email}>
              {user?.fullName || user?.username}
              <span className="muted"> ({user?.username})</span>
            </span>
            <Link to="/change-password">Change password</Link>
            <button type="button" className="btn btn-secondary btn-sm" onClick={logout}>
              Log out
            </button>
          </div>
        </header>
        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
