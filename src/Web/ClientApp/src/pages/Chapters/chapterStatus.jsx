// Trùng enum ChapterStatus ở backend (Domain/Enums/ChapterStatus.cs).
export const ChapterStatus = { Listed: 1, Fetched: 2, Edited: 3, FetchFailed: 4, EditFailed: 5 };

export const CHAPTER_STATUS_INFO = {
  [ChapterStatus.Listed]: { label: 'Chờ tải', tone: '' },
  [ChapterStatus.Fetched]: { label: 'Đã tải, chờ biên tập', tone: 'warn' },
  [ChapterStatus.Edited]: { label: 'Đã biên tập', tone: 'ok' },
  [ChapterStatus.FetchFailed]: { label: 'Lỗi tải', tone: 'danger' },
  [ChapterStatus.EditFailed]: { label: 'Lỗi biên tập', tone: 'danger' }
};

export function ChapterStatusBadge({ status, error }) {
  const info = CHAPTER_STATUS_INFO[status] ?? { label: '—', tone: '' };
  return <span className={`status-badge ${info.tone}`} title={error || undefined}>{info.label}</span>;
}
