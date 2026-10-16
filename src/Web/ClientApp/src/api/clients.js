import {
  AccountsClient,
  AccountTypesClient,
  AIsClient,
  AppViewersClient,
  CategoriesClient,
  EpisodesClient,
  LanguagesClient,
  MonetizationClient,
  MoviesClient,
  PromptsClient,
  PromptTypesClient,
  StorySourcesClient,
  StoryVideoTypesClient,
  UserProfilesClient,
  UserProfileTypesClient,
  WebSourcesClient,
  WorksClient
} from '../web-api-client';
import { authorizedHttp } from './httpClient.js';

// Mọi client phải được tiêm authorizedHttp, nếu không request sẽ đi ra mà không kèm
// JWT và mất luôn cơ chế tự refresh token khi gặp 401.
export const accountsClient = new AccountsClient(undefined, authorizedHttp);
export const accountTypesClient = new AccountTypesClient(undefined, authorizedHttp);
export const aisClient = new AIsClient(undefined, authorizedHttp);
export const promptTypesClient = new PromptTypesClient(undefined, authorizedHttp);
export const promptsClient = new PromptsClient(undefined, authorizedHttp);
export const worksClient = new WorksClient(undefined, authorizedHttp);
export const languagesClient = new LanguagesClient(undefined, authorizedHttp);
export const moviesClient = new MoviesClient(undefined, authorizedHttp);
export const episodesClient = new EpisodesClient(undefined, authorizedHttp);
export const categoriesClient = new CategoriesClient(undefined, authorizedHttp);
export const storySourcesClient = new StorySourcesClient(undefined, authorizedHttp);
export const storyVideoTypesClient = new StoryVideoTypesClient(undefined, authorizedHttp);
export const userProfilesClient = new UserProfilesClient(undefined, authorizedHttp);
export const userProfileTypesClient = new UserProfileTypesClient(undefined, authorizedHttp);
export const webSourcesClient = new WebSourcesClient(undefined, authorizedHttp);
export const monetizationClient = new MonetizationClient(undefined, authorizedHttp);
export const appViewersClient = new AppViewersClient(undefined, authorizedHttp);

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
