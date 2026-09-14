export function ConfirmDialog({ open, title, message, confirmLabel = 'Xoá', onConfirm, onCancel, busy }) {
  if (!open) return null;

  return (
    <dialog open>
      <article>
        <header>
          <strong>{title}</strong>
        </header>
        <p>{message}</p>
        <footer>
          <button type="button" className="secondary" onClick={onCancel} disabled={busy}>
            Huỷ
          </button>
          <button type="button" className="danger" onClick={onConfirm} disabled={busy} aria-busy={busy}>
            {confirmLabel}
          </button>
        </footer>
      </article>
    </dialog>
  );
}
