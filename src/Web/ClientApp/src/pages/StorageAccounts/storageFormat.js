// Cloudflare tính dung lượng R2 theo hệ thập phân (1 GB = 1 tỷ byte). Dùng 1024³ thì "9,5 GB" nhập ở đây sẽ thành
// 10,2 GB trên Cloudflare và vượt mức miễn phí.
export const GB = 1000 ** 3;
export const MB = 1000 ** 2;

// Hạn mức miễn phí hằng tháng và đơn giá vượt mức của Cloudflare R2 Standard.
export const FREE_STORAGE_BYTES = 10 * GB;
export const FREE_CLASS_A_REQUESTS = 1_000_000;
export const FREE_CLASS_B_REQUESTS = 10_000_000;
export const STORAGE_PRICE_PER_GB_USD = 0.015;
export const CLASS_A_PRICE_PER_MILLION_USD = 4.5;
export const CLASS_B_PRICE_PER_MILLION_USD = 0.36;

const sizeFormat = new Intl.NumberFormat('vi-VN', { maximumFractionDigits: 2 });
const countFormat = new Intl.NumberFormat('vi-VN');
const usdFormat = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 2,
  maximumFractionDigits: 4
});

/** Số byte → "512 MB" / "9,5 GB". */
export function formatBytes(bytes) {
  const value = Number(bytes) || 0;
  if (value >= GB) return `${sizeFormat.format(value / GB)} GB`;
  if (value >= MB) return `${sizeFormat.format(value / MB)} MB`;
  if (value >= 1000) return `${sizeFormat.format(value / 1000)} KB`;
  return `${value} B`;
}

export function formatCount(value) {
  return countFormat.format(Number(value) || 0);
}

export function formatUsd(amount) {
  return usdFormat.format(Number(amount) || 0);
}

export function formatMonth(year, month) {
  return `${String(month).padStart(2, '0')}/${year}`;
}

export function estimateR2Cost(usedBytes, classARequests, classBRequests) {
  const billableStorageGb = Math.max(0, (Number(usedBytes) || 0) - FREE_STORAGE_BYTES) / GB;
  const billableClassAMillions = Math.max(0, (Number(classARequests) || 0) - FREE_CLASS_A_REQUESTS) / 1_000_000;
  const billableClassBMillions = Math.max(0, (Number(classBRequests) || 0) - FREE_CLASS_B_REQUESTS) / 1_000_000;

  const storageCostUsd = billableStorageGb * STORAGE_PRICE_PER_GB_USD;
  const classACostUsd = billableClassAMillions * CLASS_A_PRICE_PER_MILLION_USD;
  const classBCostUsd = billableClassBMillions * CLASS_B_PRICE_PER_MILLION_USD;

  return {
    storageCostUsd,
    classACostUsd,
    classBCostUsd,
    totalEstimatedCostUsd: storageCostUsd + classACostUsd + classBCostUsd
  };
}
