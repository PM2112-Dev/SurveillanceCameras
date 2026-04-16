import { createContext, useContext, useState, useEffect } from 'react';
import { UsersClient, LoginRequest, RefreshRequest, RegisterRequest } from '../../web-api-client';
import { authorizedHttp, tokenStore } from '../../api/httpClient.js';

const AuthContext = createContext(null);

const client = new UsersClient(undefined, authorizedHttp);

export function AuthProvider({ children }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const bootstrapAuth = async () => {
      const accessToken = tokenStore.getAccessToken();
      const refreshToken = tokenStore.getRefreshToken();

      if (!accessToken && !refreshToken) {
        setIsAuthenticated(false);
        setIsLoading(false);
        return;
      }

      if (!accessToken && refreshToken) {
        try {
          const refreshed = await client.refresh(new RefreshRequest({ refreshToken }));
          tokenStore.setTokens(refreshed.accessToken, refreshed.refreshToken);
        } catch {
          tokenStore.clear();
          setIsAuthenticated(false);
          setIsLoading(false);
          return;
        }
      }

      client.infoGET()
        .then(() => setIsAuthenticated(true))
        .catch(() => {
          tokenStore.clear();
          setIsAuthenticated(false);
        })
        .finally(() => setIsLoading(false));
    };

    bootstrapAuth();
  }, []);

  const login = (email, password) =>
    client.login(new LoginRequest({ email, password }))
      .then(token => {
        tokenStore.setTokens(token.accessToken, token.refreshToken);
        setIsAuthenticated(true);
      });

  const register = (email, password) =>
    client.register(new RegisterRequest({ email, password }));

  const logout = () =>
    client.logout({})
      .finally(() => {
        tokenStore.clear();
        setIsAuthenticated(false);
      });

  return (
    <AuthContext.Provider value={{ isAuthenticated, isLoading, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);