import { Inbox, TriangleAlert } from 'lucide-react';

export function LoadingBlock({ label = 'Đang tải…' }) {
  return <p aria-busy="true">{label}</p>;
}

export function ErrorAlert({ message }) {
  if (!message) return null;

  return (
    <p className="error" role="alert">
      <TriangleAlert size={16} aria-hidden="true" /> {message}
    </p>
  );
}

export function EmptyState({ message = 'Chưa có dữ liệu.' }) {
  return (
    <div className="empty-state">
      <Inbox size={32} aria-hidden="true" />
      <p>{message}</p>
    </div>
  );
}
