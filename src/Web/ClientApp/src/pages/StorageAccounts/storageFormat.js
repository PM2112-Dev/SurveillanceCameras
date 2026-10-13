// Cloudflare tính dung lượng R2 theo hệ thập phân (1 GB = 1 tỷ byte). Dùng 1024³ thì "9,5 GB" nhập ở đây sẽ thành
// 10,2 GB trên Cloudflare và vượt mức miễn phí.
export const GB = 1000 ** 3;

const sizeFormat = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 });

/** Số byte → "512 MB" / "9,5 GB". */
export function formatBytes(bytes) {
  const value = Number(bytes) || 0;
  if (value >= GB) return `${sizeFormat.format(value / GB)} GB`;
  if (value >= 1000 ** 2) return `${sizeFormat.format(value / 1000 ** 2)} MB`;
  if (value >= 1000) return `${sizeFormat.format(value / 1000)} KB`;
  return `${value} B`;
}
