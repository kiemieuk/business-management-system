import { useCallback, useEffect, useMemo, useState, type FormEvent } from 'react'
import { permissionsApi, rolesApi } from '../../api/identity'
import { useAuth } from '../../auth/authContextValue'
import { ErrorAlert } from '../../components/ErrorAlert'
import { Modal } from '../../components/Modal'
import { Permissions, type PermissionDto, type RoleDto } from '../../types/identity'

const ADMINISTRATOR_ROLE = 'administrator'

/** The Administrator role always holds every permission; the backend rejects changes to it. */
function isAdministratorRole(role: RoleDto): boolean {
  return role.name.trim().toLowerCase() === ADMINISTRATOR_ROLE
}

type Dialog =
  | { kind: 'create' }
  | { kind: 'edit'; role: RoleDto }
  | { kind: 'permissions'; role: RoleDto }
  | { kind: 'delete'; role: RoleDto }
  | null

export function RolesPage() {
  const { hasPermission, refreshProfile } = useAuth()
  const [roles, setRoles] = useState<RoleDto[]>([])
  const [permissions, setPermissions] = useState<PermissionDto[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<unknown>(null)
  const [dialog, setDialog] = useState<Dialog>(null)
  const [notice, setNotice] = useState<string | null>(null)

  const canCreate = hasPermission(Permissions.RolesCreate)
  const canUpdate = hasPermission(Permissions.RolesUpdate)
  const canDelete = hasPermission(Permissions.RolesDelete)

  useEffect(() => {
    let cancelled = false
    Promise.all([rolesApi.list(), permissionsApi.list()])
      .then(([roleList, permissionList]) => {
        if (cancelled) return
        setRoles(roleList)
        setPermissions(permissionList)
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

  const permissionNames = useMemo(
    () => new Map(permissions.map((p) => [p.code, p.name])),
    [permissions],
  )

  const closeDialog = useCallback(() => setDialog(null), [])

  const onSaved = (message: string, role: RoleDto) => {
    setDialog(null)
    setNotice(message)
    setRoles((prev) =>
      prev.some((r) => r.id === role.id) ? prev.map((r) => (r.id === role.id ? role : r)) : [...prev, role],
    )
    // The current user's effective permissions may have changed.
    refreshProfile().catch(() => {})
  }

  return (
    <div className="page">
      <div className="page-header">
        <h1>Roles</h1>
        {canCreate && (
          <button
            type="button"
            className="btn btn-primary"
            onClick={() => {
              setNotice(null)
              setDialog({ kind: 'create' })
            }}
          >
            New role
          </button>
        )}
      </div>

      <ErrorAlert error={error} />
      {notice && <div className="alert alert-success">{notice}</div>}

      <div className="card table-card">
        <table className="table">
          <thead>
            <tr>
              <th>Name</th>
              <th>Description</th>
              <th>Permissions</th>
              <th>Users</th>
              <th className="actions-col">Actions</th>
            </tr>
          </thead>
          <tbody>
            {loading && roles.length === 0 ? (
              <tr>
                <td colSpan={5} className="muted center">
                  Loading…
                </td>
              </tr>
            ) : roles.length === 0 ? (
              <tr>
                <td colSpan={5} className="muted center">
                  No roles found.
                </td>
              </tr>
            ) : (
              roles.map((r) => (
                <tr key={r.id}>
                  <td>
                    {r.name} {r.isSystem && <span className="badge badge-info">System</span>}
                  </td>
                  <td>{r.description || <span className="muted">—</span>}</td>
                  <td>
                    <div className="tags">
                      {r.permissions.length === 0 && <span className="muted">None</span>}
                      {r.permissions.map((code) => (
                        <span className="tag" key={code} title={code}>
                          {permissionNames.get(code) ?? code}
                        </span>
                      ))}
                    </div>
                  </td>
                  <td>{r.userCount}</td>
                  <td className="actions-col">
                    <div className="row-actions">
                      {canUpdate && (
                        <>
                          <button type="button" className="btn btn-sm" onClick={() => setDialog({ kind: 'edit', role: r })}>
                            Edit
                          </button>
                          {isAdministratorRole(r) ? (
                            <span className="muted hint" title="The Administrator role cannot be changed">
                              Always has all permissions
                            </span>
                          ) : (
                            <button
                              type="button"
                              className="btn btn-sm"
                              onClick={() => setDialog({ kind: 'permissions', role: r })}
                            >
                              Permissions
                            </button>
                          )}
                        </>
                      )}
                      {canDelete && !r.isSystem && (
                        <button
                          type="button"
                          className="btn btn-sm btn-danger"
                          onClick={() => setDialog({ kind: 'delete', role: r })}
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
        <CreateRoleDialog
          permissions={permissions}
          onClose={closeDialog}
          onSaved={(r) => onSaved(`Role "${r.name}" created.`, r)}
        />
      )}
      {dialog?.kind === 'edit' && (
        <EditRoleDialog role={dialog.role} onClose={closeDialog} onSaved={(r) => onSaved(`Role "${r.name}" updated.`, r)} />
      )}
      {dialog?.kind === 'permissions' && !isAdministratorRole(dialog.role) && (
        <RolePermissionsDialog
          role={dialog.role}
          permissions={permissions}
          onClose={closeDialog}
          onSaved={(r) => onSaved(`Permissions for "${r.name}" updated.`, r)}
        />
      )}
      {dialog?.kind === 'delete' && (
        <DeleteRoleDialog
          role={dialog.role}
          onClose={closeDialog}
          onDeleted={() => {
            const deleted = dialog.role
            setDialog(null)
            setNotice(`Role "${deleted.name}" deleted.`)
            setRoles((prev) => prev.filter((r) => r.id !== deleted.id))
          }}
        />
      )}
    </div>
  )
}

function PermissionPicker({
  permissions,
  selected,
  onChange,
}: {
  permissions: PermissionDto[]
  selected: string[]
  onChange: (codes: string[]) => void
}) {
  const groups = useMemo(() => {
    const map = new Map<string, PermissionDto[]>()
    for (const p of permissions) {
      const list = map.get(p.group) ?? []
      list.push(p)
      map.set(p.group, list)
    }
    return [...map.entries()]
  }, [permissions])

  if (permissions.length === 0) return <p className="muted">No permissions available.</p>

  const toggle = (code: string, checked: boolean) =>
    onChange(checked ? [...selected, code] : selected.filter((c) => c !== code))

  const toggleGroup = (items: PermissionDto[], checked: boolean) => {
    const codes = items.map((p) => p.code)
    onChange(checked ? [...new Set([...selected, ...codes])] : selected.filter((c) => !codes.includes(c)))
  }

  return (
    <div className="permission-groups">
      {groups.map(([group, items]) => {
        const allChecked = items.every((p) => selected.includes(p.code))
        return (
          <fieldset className="permission-group" key={group}>
            <legend>
              <label className="checkbox">
                <input
                  type="checkbox"
                  checked={allChecked}
                  onChange={(e) => toggleGroup(items, e.target.checked)}
                />
                <strong>{group}</strong>
              </label>
            </legend>
            <div className="checkbox-list">
              {items.map((p) => (
                <label className="checkbox" key={p.code} title={p.code}>
                  <input
                    type="checkbox"
                    checked={selected.includes(p.code)}
                    onChange={(e) => toggle(p.code, e.target.checked)}
                  />
                  <span>
                    {p.name}
                    {p.description && <small className="muted"> — {p.description}</small>}
                  </span>
                </label>
              ))}
            </div>
          </fieldset>
        )
      })}
    </div>
  )
}

function CreateRoleDialog({
  permissions,
  onClose,
  onSaved,
}: {
  permissions: PermissionDto[]
  onClose: () => void
  onSaved: (role: RoleDto) => void
}) {
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [selected, setSelected] = useState<string[]>([])
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!name.trim()) {
      setError(new Error('Name is required.'))
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      onSaved(
        await rolesApi.create({
          name: name.trim(),
          description: description.trim() || null,
          permissions: selected,
        }),
      )
    } catch (err) {
      setError(err)
      setSubmitting(false)
    }
  }

  return (
    <Modal title="New role" onClose={onClose}>
      <form className="form" onSubmit={onSubmit} noValidate>
        <ErrorAlert error={error} />
        <label className="field">
          <span>Name</span>
          <input value={name} onChange={(e) => setName(e.target.value)} autoFocus />
        </label>
        <label className="field">
          <span>Description</span>
          <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={2} />
        </label>
        <div className="field">
          <span>Permissions</span>
          <PermissionPicker permissions={permissions} selected={selected} onChange={setSelected} />
        </div>
        <div className="form-actions">
          <button type="button" className="btn btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" className="btn btn-primary" disabled={submitting}>
            {submitting ? 'Creating…' : 'Create role'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

function EditRoleDialog({
  role,
  onClose,
  onSaved,
}: {
  role: RoleDto
  onClose: () => void
  onSaved: (role: RoleDto) => void
}) {
  const [name, setName] = useState(role.name)
  const [description, setDescription] = useState(role.description ?? '')
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    if (!name.trim()) {
      setError(new Error('Name is required.'))
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      onSaved(
        await rolesApi.update(role.id, {
          name: role.isSystem ? role.name : name.trim(),
          description: description.trim() || null,
        }),
      )
    } catch (err) {
      setError(err)
      setSubmitting(false)
    }
  }

  return (
    <Modal title={`Edit role "${role.name}"`} onClose={onClose}>
      <form className="form" onSubmit={onSubmit} noValidate>
        <ErrorAlert error={error} />
        <label className="field">
          <span>Name</span>
          <input value={name} onChange={(e) => setName(e.target.value)} disabled={role.isSystem} autoFocus />
          {role.isSystem && <small className="muted">System roles cannot be renamed.</small>}
        </label>
        <label className="field">
          <span>Description</span>
          <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={2} />
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

function RolePermissionsDialog({
  role,
  permissions,
  onClose,
  onSaved,
}: {
  role: RoleDto
  permissions: PermissionDto[]
  onClose: () => void
  onSaved: (role: RoleDto) => void
}) {
  const [selected, setSelected] = useState<string[]>(role.permissions)
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      onSaved(await rolesApi.setPermissions(role.id, selected))
    } catch (err) {
      setError(err)
      setSubmitting(false)
    }
  }

  return (
    <Modal title={`Permissions for "${role.name}"`} onClose={onClose}>
      <form className="form" onSubmit={onSubmit}>
        <ErrorAlert error={error} />
        <PermissionPicker permissions={permissions} selected={selected} onChange={setSelected} />
        <div className="form-actions">
          <button type="button" className="btn btn-secondary" onClick={onClose}>
            Cancel
          </button>
          <button type="submit" className="btn btn-primary" disabled={submitting}>
            {submitting ? 'Saving…' : 'Save permissions'}
          </button>
        </div>
      </form>
    </Modal>
  )
}

function DeleteRoleDialog({
  role,
  onClose,
  onDeleted,
}: {
  role: RoleDto
  onClose: () => void
  onDeleted: () => void
}) {
  const [error, setError] = useState<unknown>(null)
  const [submitting, setSubmitting] = useState(false)

  const onConfirm = async () => {
    setSubmitting(true)
    setError(null)
    try {
      await rolesApi.delete(role.id)
      onDeleted()
    } catch (err) {
      setError(err)
      setSubmitting(false)
    }
  }

  return (
    <Modal title="Delete role" onClose={onClose}>
      <ErrorAlert error={error} />
      <p>
        Are you sure you want to delete role <strong>{role.name}</strong>?
        {role.userCount > 0 && ` It is currently assigned to ${role.userCount} user(s).`}
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
