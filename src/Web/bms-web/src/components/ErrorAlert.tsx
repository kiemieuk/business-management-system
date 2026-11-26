import { getErrorMessages } from '../api/client'

export function ErrorAlert({ error }: { error: unknown }) {
  if (!error) return null
  const messages = getErrorMessages(error)
  return (
    <div className="alert alert-error" role="alert">
      {messages.length === 1 ? (
        messages[0]
      ) : (
        <ul>
          {messages.map((m, i) => (
            <li key={i}>{m}</li>
          ))}
        </ul>
      )}
    </div>
  )
}
