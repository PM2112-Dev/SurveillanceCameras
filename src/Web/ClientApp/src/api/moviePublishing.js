import { authorizedHttp } from './httpClient.js';

// Các endpoint xuất bản phim lên app xem phim. Gọi thẳng bằng fetch vì web-api-client.ts (NSwag) chưa sinh lại;
// sau khi chạy `npm run generate-api` có thể thay bằng MoviesClient.

async function send(method, url, body) {
  const response = await authorizedHttp.fetch(url, {
    method,
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(body)
  });
  if (response.ok) return response.status === 204 ? null : response.json();

  const problem = await response.json().catch(() => null);
  const fieldError = problem?.errors && Object.values(problem.errors)[0]?.[0];
  throw new Error(fieldError || problem?.detail || problem?.title || `Yêu cầu thất bại (HTTP ${response.status}).`);
}

/** Bật/tắt hiển thị phim trên app; không truyền ids thì áp dụng cho mọi phim. Trả về số phim đã đổi. */
export const setMoviesPublished = (isPublished, ids) => send('POST', '/api/Movies/published', { isPublished, ids });

/** Gán link file video tự lưu trữ (https) cho một tập; chuỗi rỗng để gỡ. */
export const setMovieEpisodeStream = (movieId, episodeId, streamUrl) =>
  send('PUT', `/api/Movies/${movieId}/episodes/${episodeId}/stream`, { streamUrl });
