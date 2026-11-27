import { useState, type FormEvent } from 'react'
import { authApi } from '../../api/identity'
import { ErrorAlert } from '../../components/ErrorAlert'
import { validatePassword } from '../../utils/validation'

export function ChangePasswordPage() {
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState<unknown>(null)
  const [success, setSuccess] = useState(false)
  const [submitting, setSubmitting] = useState(false)

  const onSubmit = async (e: FormEvent) => {
    e.preventDefault()
    setSuccess(false)
    if (!currentPassword) {
      setError(new Error('Current password is required.'))
      return
    }
    const passwordError = validatePassword(newPassword)
    if (passwordError) {
      setError(new Error(passwordError))
      return
    }
    if (newPassword !== confirmPassword) {
      setError(new Error('New password and confirmation do not match.'))
      return
    }
    setSubmitting(true)
    setError(null)
    try {
      await authApi.changePassword({ currentPassword, newPassword })
      setSuccess(true)
      setCurrentPassword('')
      setNewPassword('')
      setConfirmPassword('')
    } catch (err) {
      setError(err)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="page narrow">
      <h1>Change password</h1>
      <form className="card form" onSubmit={onSubmit} noValidate>
        <ErrorAlert error={error} />
        {success && <div className="alert alert-success">Your password has been changed.</div>}
        <label className="field">
          <span>Current password</span>
          <input
            type="password"
            value={currentPassword}
            onChange={(e) => setCurrentPassword(e.target.value)}
            autoComplete="current-password"
          />
        </label>
        <label className="field">
          <span>New password</span>
          <input
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            autoComplete="new-password"
            minLength={8}
          />
          <small className="muted">At least 8 characters.</small>
        </label>
        <label className="field">
          <span>Confirm new password</span>
          <input
            type="password"
            value={confirmPassword}
            onChange={(e) => setConfirmPassword(e.target.value)}
            autoComplete="new-password"
          />
        </label>
        <div className="form-actions">
          <button type="submit" className="btn btn-primary" disabled={submitting}>
            {submitting ? 'Saving…' : 'Change password'}
          </button>
        </div>
      </form>
    </div>
  )
}
