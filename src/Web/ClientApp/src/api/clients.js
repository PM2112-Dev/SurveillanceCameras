import {
  CategoriesClient,
  StorySourcesClient,
  WebSourcesClient
} from '../web-api-client';
import { authorizedHttp } from './httpClient.js';

// Mọi client phải được tiêm authorizedHttp, nếu không request sẽ đi ra mà không kèm
// JWT và mất luôn cơ chế tự refresh token khi gặp 401.
export const categoriesClient = new CategoriesClient(undefined, authorizedHttp);
export const storySourcesClient = new StorySourcesClient(undefined, authorizedHttp);
export const webSourcesClient = new WebSourcesClient(undefined, authorizedHttp);

/** Lấy thông điệp lỗi dễ đọc từ SwaggerException / ProblemDetails trả về bởi API. */
export function getErrorMessage(error, fallback = 'Đã có lỗi xảy ra, vui lòng thử lại.') {
  if (!error) return fallback;

  let payload = error.result ?? error.response;
  if (typeof payload === 'string') {
    try {
      payload = JSON.parse(payload);
    } catch {
      return payload || error.message || fallback;
    }
  }

  if (payload?.errors) {
    const messages = Object.values(payload.errors).flat();
    if (messages.length > 0) return messages.join(' ');
  }

  return payload?.detail || payload?.title || error.message || fallback;
}
