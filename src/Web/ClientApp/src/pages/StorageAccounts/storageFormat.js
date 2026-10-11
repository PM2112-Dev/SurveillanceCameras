export const GB = 1024 ** 3;

const sizeFormat = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 });

/** Số byte → "512 MB" / "9,5 GB". */
export function formatBytes(bytes) {
  const value = Number(bytes) || 0;
  if (value >= GB) return `${sizeFormat.format(value / GB)} GB`;
  if (value >= 1024 ** 2) return `${sizeFormat.format(value / 1024 ** 2)} MB`;
  if (value >= 1024) return `${sizeFormat.format(value / 1024)} KB`;
  return `${value} B`;
}
