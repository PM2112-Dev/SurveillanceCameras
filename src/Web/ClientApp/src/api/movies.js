import { authorizedHttp } from './httpClient.js';

function buildQuery(params) {
  const searchParams = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== '') {
      searchParams.set(key, String(value));
    }
  });
  const query = searchParams.toString();
  return query ? `?${query}` : '';
}

async function getJson(url) {
  const response = await authorizedHttp.fetch(url, {
    method: 'GET',
    headers: { Accept: 'application/json' }
  });
  if (response.ok) return response.json();

  const problem = await response.json().catch(() => null);
  const fieldError = problem?.errors && Object.values(problem.errors)[0]?.[0];
  throw new Error(fieldError || problem?.detail || problem?.title || `Yêu cầu thất bại (HTTP ${response.status}).`);
}

function hydrateMovie(item) {
  if (!item) return item;
  return {
    ...item,
    created: item.created ? new Date(item.created) : null,
    lastModified: item.lastModified ? new Date(item.lastModified) : null,
    episodesSyncedAt: item.episodesSyncedAt ? new Date(item.episodesSyncedAt) : null
  };
}

/** Lấy danh sách phim kèm đầy đủ bộ lọc mở rộng. */
export async function getMoviesFiltered(params = {}) {
  const data = await getJson(`/api/Movies${buildQuery(params)}`);
  if (Array.isArray(data?.items)) {
    data.items = data.items.map(hydrateMovie);
  }
  return data;
}

/** Lấy danh sách tập của một phim kèm bộ lọc và tìm kiếm. */
export async function getMovieEpisodesFiltered(movieId, params = {}) {
  return getJson(`/api/Movies/${movieId}/episodes${buildQuery(params)}`);
}
