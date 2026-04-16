const ACCESS_TOKEN_KEY = 'auth.accessToken';
const REFRESH_TOKEN_KEY = 'auth.refreshToken';

export const tokenStore = {
  getAccessToken() {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  },
  getRefreshToken() {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  },
  setTokens(accessToken, refreshToken) {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
  },
  clear() {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
  }
};

export const authorizedHttp = {
  async fetch(input, init = {}) {
    const headers = new Headers(init.headers ?? {});
    const accessToken = tokenStore.getAccessToken();
    const requestUrl = typeof input === 'string' ? input : input.url;

    if (accessToken && !headers.has('Authorization')) {
      headers.set('Authorization', `Bearer ${accessToken}`);
    }

    const response = await window.fetch(input, {
      ...init,
      headers
    });

    const isAuthEndpoint = requestUrl.includes('/api/Users/login')
      || requestUrl.includes('/api/Users/register')
      || requestUrl.includes('/api/Users/refresh');

    if (response.status !== 401 || isAuthEndpoint) {
      return response;
    }

    const refreshToken = tokenStore.getRefreshToken();
    if (!refreshToken) {
      tokenStore.clear();
      return response;
    }

    const refreshResponse = await window.fetch('/api/Users/refresh', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'application/json'
      },
      body: JSON.stringify({ refreshToken })
    });

    if (!refreshResponse.ok) {
      tokenStore.clear();
      return response;
    }

    const refreshedTokens = await refreshResponse.json();
    tokenStore.setTokens(refreshedTokens.accessToken, refreshedTokens.refreshToken);

    const retryHeaders = new Headers(init.headers ?? {});
    retryHeaders.set('Authorization', `Bearer ${refreshedTokens.accessToken}`);

    return window.fetch(input, {
      ...init,
      headers: retryHeaders
    });

  }
};



