import { authorizedHttp } from './httpClient.js';

// Tài khoản lưu trữ video (Cloudflare R2). Gọi thẳng bằng fetch vì web-api-client.ts (NSwag) chưa sinh lại;
// sau khi chạy `npm run generate-api` có thể thay bằng StorageAccountsClient / MoviesClient.

async function send(method, url, body) {
  const response = await authorizedHttp.fetch(url, {
    method,
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body)
  });
  if (response.ok) return response.status === 204 ? null : response.json();

  const problem = await response.json().catch(() => null);
  const fieldError = problem?.errors && Object.values(problem.errors)[0]?.[0];
  throw new Error(fieldError || problem?.detail || problem?.title || `Yêu cầu thất bại (HTTP ${response.status}).`);
}

export const getStorageAccounts = () => send('GET', '/api/StorageAccounts');
export const getStorageAccount = id => send('GET', `/api/StorageAccounts/${id}`);
export const createStorageAccount = account => send('POST', '/api/StorageAccounts', account);
export const updateStorageAccount = (id, account) => send('PUT', `/api/StorageAccounts/${id}`, { id, ...account });
export const deleteStorageAccount = id => send('DELETE', `/api/StorageAccounts/${id}`);

/** Kết nối thử tới bucket và đối soát dung lượng đã dùng. Trả về `{ succeeded, error, usedBytes, objects }`. */
export const checkStorageAccount = id => send('POST', `/api/StorageAccounts/${id}/check`);

/**
 * Đưa dung lượng bucket về dưới hạn mức. `apply = false` chỉ trả kế hoạch (không xoá gì); `deleteOrphans` xoá cả file
 * trong thư mục movies/ mà không tập nào trỏ tới.
 */
export const trimStorageAccount = (id, apply, deleteOrphans) =>
  send('POST', `/api/StorageAccounts/${id}/trim`, { apply, deleteOrphans });

/** Cho hệ thống thử lưu lại video của các tập đã lỗi trong phim. Trả về số tập được đưa lại vào hàng chờ. */
export const retryMovieVideoStorage = movieId => send('POST', `/api/Movies/${movieId}/video-storage/retry`);

/** Tải video của một tập từ TikTok và lưu vào kho ngay. Trả về `{ succeeded, streamUrl, sizeBytes, error }`. */
export const storeMovieEpisodeVideo = (movieId, episodeId) =>
  send('POST', `/api/Movies/${movieId}/episodes/${episodeId}/video-storage/store`);
