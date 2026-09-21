// Khớp TikTokSessionState phía backend (API trả về số thứ tự của enum).
export const SessionState = {
  Stopped: 0,
  WaitingForLogin: 1,
  Active: 2,
  LoggedOut: 3
};

const STATE_INFO = {
  [SessionState.Stopped]: { label: 'Chưa mở', tone: '' },
  [SessionState.WaitingForLogin]: { label: 'Chờ đăng nhập', tone: 'warn' },
  [SessionState.Active]: { label: 'Đang duy trì', tone: 'ok' },
  [SessionState.LoggedOut]: { label: 'Bị đăng xuất', tone: 'danger' }
};

export function getSessionInfo(status) {
  return STATE_INFO[status?.state ?? SessionState.Stopped] ?? STATE_INFO[SessionState.Stopped];
}

export function formatDateTime(value) {
  return value ? new Date(value).toLocaleString('vi-VN') : null;
}
