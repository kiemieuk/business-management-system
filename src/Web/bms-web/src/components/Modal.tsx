import { useEffect, useRef, type MouseEvent, type ReactNode } from 'react'

interface ModalProps {
  title: string
  onClose: () => void
  children: ReactNode
}

export function Modal({ title, onClose, children }: ModalProps) {
  // Keep the latest onClose in a ref so the keydown listener is registered once
  // and the dialog is never torn down because a parent passed a new callback.
  const onCloseRef = useRef(onClose)
  const pressStartedOnBackdrop = useRef(false)

  useEffect(() => {
    onCloseRef.current = onClose
  }, [onClose])

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onCloseRef.current()
    }
    window.addEventListener('keydown', onKey)
    return () => window.removeEventListener('keydown', onKey)
  }, [])

  // Only close when a full click (press and release) happens on the backdrop itself,
  // so selecting text or dragging out of the dialog does not discard the form.
  const onBackdropMouseDown = (e: MouseEvent<HTMLDivElement>) => {
    pressStartedOnBackdrop.current = e.target === e.currentTarget
  }
  const onBackdropClick = (e: MouseEvent<HTMLDivElement>) => {
    if (pressStartedOnBackdrop.current && e.target === e.currentTarget) onCloseRef.current()
    pressStartedOnBackdrop.current = false
  }

  return (
    <div className="modal-backdrop" onMouseDown={onBackdropMouseDown} onClick={onBackdropClick}>
      <div className="modal" role="dialog" aria-modal="true" aria-label={title}>
        <div className="modal-header">
          <h2>{title}</h2>
          <button type="button" className="btn-icon" onClick={() => onCloseRef.current()} aria-label="Close">
            ×
          </button>
        </div>
        <div className="modal-body">{children}</div>
      </div>
    </div>
  )
}
