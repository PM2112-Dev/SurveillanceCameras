/** Thời lượng (giây) → "1 giờ 5 phút" / "12 phút" / "45 giây". */
export function formatDuration(totalSeconds) {
  const seconds = Number(totalSeconds) || 0;
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);

  if (hours > 0) return `${hours} giờ ${minutes} phút`;
  if (minutes > 0) return `${minutes} phút`;
  return `${seconds} giây`;
}

export const numberFormat = new Intl.NumberFormat('vi-VN');
export const dateTimeFormat = new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' });
