import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { ApiError } from '../../api/client'
import { rolesApi, usersApi } from '../../api/identity'
import { useAuth } from '../../auth/authContextValue'
import { ErrorAlert } from '../../components/ErrorAlert'
import { Modal } from '../../components/Modal'
import { Permissions, type RoleDto, type UserDto } from '../../types/identity'
import { formatDate } from '../../utils/format'
import { validatePassword } from '../../utils/validation'

type Dialog =
  | { kind: 'create' }
  | { kind: 'edit'; user: UserDto }
  | { kind: 'roles'; user: UserDto }
  | { kind: 'reset'; user: UserDto }
  | { kind: 'delete'; user: UserDto }
  | null

export function UsersPage() {
  const { hasPermission, user: currentUser } = useAuth()
  const [users, setUsers] = useState<UserDto[]>([])
  const [roles, setRoles] = useState<RoleDto[] | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [dialog, setDialog] = useState<Dialog>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const canCreate = hasPermission(Permissions.UsersCreate)
  const canUpdate = hasPermission(Permissions.UsersUpdate)
  const canDelete = hasPermission(Permissions.UsersDelete)
  const canViewRoles = hasPermission(Permissions.RolesView)

  useEffect(() => {
    let cancelled = false
    usersApi
      .list()
      .then((list) => {
        if (!cancelled) setUsers(list)
      })
      .catch((err: unknown) => {
        if (!cancelled) setError(err)
      })
      .finally(() => {
        if (!cancelled) setLoading(false)
      })
    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!canViewRoles) return
    rolesApi
      .list()
      .then(setRoles)
      .catch((err: unknown) => {
        if (!(err instanceof ApiError && err.status === 403)) setError(err)
      })
  }, [canViewRoles])

  const closeDialog = useCallback(() => setDialog(null), [])

  const onSaved = (message: string, updated: UserDto) => {
    setDialog(null)
    setNotice(message)
    setUsers((prev) =>
      prev.some((u) => u.id === updated.id)
        ? prev.map((u) => (u.id === updated.id ? updated : u))
        : [...prev, updated],
    )
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1>Users</h1>
        {canCreate && (
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => {
              setNotice(null)
              setDialog({ kind: 'create' })
            }}
          >
            New user
          </button>
        )}
      </div>

      <ErrorAlert error={error} />
      {notice && <div className="alert alert-success">{notice}</div>}

      <div className="card table-card">
        <table className="table">
          <thead>
            <tr>
              <th>Username</th>
              <th>Full name</th>
              <th>Email</th>
              <th>Roles</th>
              <th>Status</th>
              <th>Last login</th>
              <th className="actions-col">Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading && users.length === 0 ? (
              <tr>
                <td colSpan={7} className="muted center">
                  Loading…
                </td>
              </tr>
            ) : users.length === 0 ? (
              <tr>
                <td colSpan={7} className="muted center">
                  No users found.
                </td>
              </tr>
            ) : (
              users.map((u) => (
                <tr key={u.id}>
                  <td>{u.username}</td>
                  <td>{u.fullName}</td>
                  <td>{u.email}</td>
                  <td>
                    <div className="tags">
                      {u.roles.map((r) => (
                        <span className="tag" key={r.id}>
                          {r.name}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td>
                    <span className={u.isActive ? 'badge badge-success' : 'badge badge-muted'}>
                      {u.isActive ? 'Active' : 'Inactive'}
                    </span>
                  </td>
                  <td>{formatDate(u.lastLoginAt)}</td>
                  <td className="actions-col">
                    <div className="row-actions">
                      {canUpdate && (
                        <>
                          <button type="button" className="btn btn-sm" onClick={() => setDialog({ kind: 'edit', user: u })}>
                            Edit
                          </button>
                          {roles && (
                            <button type="button" className="btn btn-sm" onClick={() => setDialog({ kind: 'roles', user: u })}>
                              Roles
                            </button>
                          )}
                          <button type="button" className="btn btn-sm" onClick={() => setDialog({ kind: 'reset', user: u })}>
                            Reset password
                          </button>
                        </>
                      )}
                      {canDelete && u.id !== currentUser?.id && (
                        <button
                          type="button"
                          className="btn btn-sm btn-danger"
                          onClick={() => setDialog({ kind: 'delete', user: u })}
                        >
                          Delete
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {dialog?.kind === 'create' && (
        <CreateUserDialog roles={roles} onClose={closeDialog} onSaved={(u) => onSaved(`User "${u.username}" created.`, u)} />
      )}
      {dialog?.kind === 'edit' && (
        <EditUserDialog user={dialog.user} onClose={closeDialog} onSaved={(u) => onSaved(`User "${u.username}" updated.`, u)} />
      )}
      {dialog?.kind === 'roles' && roles && (
        <UserRolesDialog
          user={dialog.user}
          roles={roles}
          onClose={closeDialog}
          onSaved={(u) => onSaved(`Roles for "${u.username}" updated.`, u)}
        />
      )}
      {dialog?.kind === 'reset' && (
        <ResetPasswordDialog
          user={dialog.user}
          onClose={closeDialog}
          onSaved={() => onSaved(`Password for "${dialog.user.username}" has been reset.`, dialog.user)}
        />
      )}
      {dialog?.kind === 'delete' && (
        <DeleteUserDialog
          user={dialog.user}
          onClose={closeDialog}
          onDeleted={() => {
            const deleted = dialog.user
            setDialog(null)
            setNotice(`User "${deleted.username}" deleted.`)
            setUsers((prev) => prev.filter((u) => u.id !== deleted.id))
          }}
        />
      )}
    </div>
  )
}

function RoleCheckboxes({
  roles,
  selected,
  onChange,
}: {
  roles: RoleDto[]
  selected: string[]
  onChange: (ids: string[]) => void
}) {
  if (roles.length === 0) return <p className="muted">No roles available.</p>
  return (
    <div className="checkbox-list">
      {roles.map((r) => (
        <label className="checkbox" key={r.id}>
          <input
            type="checkbox"
            checked={selected.includes(r.id)}
            onChange={(e) =>
              onChange(e.target.checked ? [...selected, r.id] : selected.filter((id) => id !== r.id))
            }
          />
          <span>
            {r.name}
            {r.description && <small className="muted"> — {r.description}</small>}
          </span>
        </label>
      ))}
    </div>
  )
}

function CreateUserDialog({
  roles,
  onClose,
  onSaved,
}: {
  roles: RoleDto[] | null
  onClose: () => void
  onSaved: (user: UserDto) => void
}) {
  const [username, setUsername] = useState('')
  const [email, setEmail] = useState('')
  const [fullName, setFullName] = useState('')
  const [password, setPassword] = useState('')
  const [roleIds, setRoleIds] = useState<string[]>([])
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!username.trim() || !email.trim() || !fullName.trim()) {
      setError(new Error('Username, email and full name are required.'))
      return
    }
    const passwordError = validatePassword(password)
    if (passwordError) {
      setError(new Error(passwordError))
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      const created = await usersApi.create({
        username: username.trim(),
        email: email.trim(),
        fullName: fullName.trim(),
        password,
        roleIds,
      })
      onSaved(created)
    } catch (err) {
      setError(err)
      setSubmitting(false)
    }
  }

  return (
    <Modal title="New user" onClose={onClose}>
      <form className="form" onSubmit={onSubmit} noValidate>
        <ErrorAlert error={error} />
        <label className="field">
          <span>Username</span>
          <input value={username} onChange={(e) => setUsername(e.target.value)} autoComplete="off" autoFocus />
        </label>
        <label className="field">
          <span>Email</span>
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="off" />
        </label>
        <label className="field">
          <span>Full name</span>
          <input value={fullName} onChange={(e) => setFullName(e.target.value)} autoComplete="off" />
        </label>
        <label className="field">
          <span>Password</span>
          <input
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="new-password"
            minLength={8}
          />
          <small className="muted">At least 8 characters.</small>
        </label>
        {roles && (
          <fieldset className="field">
            <legend>Roles</legend>
            <RoleCheckboxes roles={roles} selected={roleIds} onChange={setRoleIds} />
          </fieldset>
        )}
        <div className="form-actions">
          <button type="button" className="btn btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" className="btn btn-primary" disabled={submitting}>
            {submitting ? 'Creating…' : 'Create user'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

function EditUserDialog({
  user,
  onClose,
  onSaved,
}: {
  user: UserDto
  onClose: () => void
  onSaved: (user: UserDto) => void
}) {
  const [email, setEmail] = useState(user.email)
  const [fullName, setFullName] = useState(user.fullName)
  const [isActive, setIsActive] = useState(user.isActive)
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!email.trim() || !fullName.trim()) {
      setError(new Error('Email and full name are required.'))
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      onSaved(await usersApi.update(user.id, { email: email.trim(), fullName: fullName.trim(), isActive }))
    } catch (err) {
      setError(err)
      setSubmitting(false)
    }
  }

  return (
    <Modal title={`Edit user "${user.username}"`} onClose={onClose}>
      <form className="form" onSubmit={onSubmit} noValidate>
        <ErrorAlert error={error} />
        <label className="field">
          <span>Email</span>
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} autoFocus />
        </label>
        <label className="field">
          <span>Full name</span>
          <input value={fullName} onChange={(e) => setFullName(e.target.value)} />
        </label>
        <label className="checkbox">
          <input type="checkbox" checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />
          <span>Active</span>
        </label>
        <div className="form-actions">
          <button type="button" className="btn btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" className="btn btn-primary" disabled={submitting}>
            {submitting ? 'Saving…' : 'Save'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

function UserRolesDialog({
  user,
  roles,
  onClose,
  onSaved,
}: {
  user: UserDto
  roles: RoleDto[]
  onClose: () => void
  onSaved: (user: UserDto) => void
}) {
  const [roleIds, setRoleIds] = useState<string[]>(user.roles.map((r) => r.id))
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      onSaved(await usersApi.setRoles(user.id, roleIds))
    } catch (err) {
      setError(err)
      setSubmitting(false)
    }
  }

  return (
    <Modal title={`Roles for "${user.username}"`} onClose={onClose}>
      <form className="form" onSubmit={onSubmit}>
        <ErrorAlert error={error} />
        <RoleCheckboxes roles={roles} selected={roleIds} onChange={setRoleIds} />
        <div className="form-actions">
          <button type="button" className="btn btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" className="btn btn-primary" disabled={submitting}>
            {submitting ? 'Saving…' : 'Save roles'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

function ResetPasswordDialog({
  user,
  onClose,
  onSaved,
}: {
  user: UserDto
  onClose: () => void
  onSaved: () => void
}) {
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    const passwordError = validatePassword(newPassword)
    if (passwordError) {
      setError(new Error(passwordError))
      return
    }
    if (newPassword !== confirmPassword) {
      setError(new Error('Passwords do not match.'))
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      await usersApi.resetPassword(user.id, newPassword)
      onSaved()
    } catch (err) {
      setError(err)
      setSubmitting(false)
    }
  }

  return (
    <Modal title={`Reset password for "${user.username}"`} onClose={onClose}>
      <form className="form" onSubmit={onSubmit} noValidate>
        <ErrorAlert error={error} />
        <label className="field">
          <span>New password</span>
          <input
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            autoComplete="new-password"
            minLength={8}
            autoFocus
          />
          <small className="muted">At least 8 characters.</small>
        </label>
        <label className="field">
          <span>Confirm password</span>
          <input
            type="password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            autoComplete="new-password"
          />
        </label>
        <div className="form-actions">
          <button type="button" className="btn btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" className="btn btn-primary" disabled={submitting}>
            {submitting ? 'Resetting…' : 'Reset password'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

function DeleteUserDialog({
  user,
  onClose,
  onDeleted,
}: {
  user: UserDto
  onClose: () => void
  onDeleted: () => void
}) {
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  const onConfirm = async () => {
    setSubmitting(true)
    setError(null)
    try {
      await usersApi.delete(user.id)
      onDeleted()
    } catch (err) {
      setError(err)
      setSubmitting(false)
    }
  }

  return (
    <Modal title="Delete user" onClose={onClose}>
      <ErrorAlert error={error} />
      <p>
        Are you sure you want to delete user <strong>{user.username}</strong>? This action cannot be undone.
      </p>
      <div className="form-actions">
        <button type="button" className="btn btn-secondary" onClick={onClose}>
          Cancel
        </button>
        <button type="button" className="btn btn-danger" onClick={onConfirm} disabled={submitting}>
          {submitting ? 'Deleting…' : 'Delete'}
        </button>
      </div>
    </Modal>
  )
}
