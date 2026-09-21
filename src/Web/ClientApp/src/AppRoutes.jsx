import { Route, Routes } from 'react-router-dom';
import { AppLayout } from './components/AppLayout';
import { AuthLayout } from './components/AuthLayout';
import { LoginPage } from './components/api-authorization/LoginPage';
import { RegisterPage } from './components/api-authorization/RegisterPage';
import { ProtectedRoute } from './components/api-authorization/ProtectedRoute';
import { Dashboard } from './pages/Dashboard';
import { WebSourceList } from './pages/WebSources/WebSourceList';
import { WebSourceForm } from './pages/WebSources/WebSourceForm';
import { StorySourceList } from './pages/StorySources/StorySourceList';
import { StorySourceForm } from './pages/StorySources/StorySourceForm';
import { CategoryList } from './pages/Categories/CategoryList';
import { CategoryForm } from './pages/Categories/CategoryForm';
import { AccountList } from './pages/Accounts/AccountList';
import { AccountForm } from './pages/Accounts/AccountForm';
import { TikTokChannel } from './pages/Accounts/TikTokChannel';

export default function AppRoutes() {
  return (
    <Routes>
      <Route element={<AuthLayout />}>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
      </Route>

      <Route
        element={
          <ProtectedRoute>
            <AppLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Dashboard />} />

        <Route path="/web-sources" element={<WebSourceList />} />
        <Route path="/web-sources/new" element={<WebSourceForm />} />
        <Route path="/web-sources/:id" element={<WebSourceForm />} />

        <Route path="/story-sources" element={<StorySourceList />} />
        <Route path="/story-sources/new" element={<StorySourceForm />} />
        <Route path="/story-sources/:id" element={<StorySourceForm />} />

        <Route path="/categories" element={<CategoryList />} />
        <Route path="/categories/new" element={<CategoryForm />} />
        <Route path="/categories/:id" element={<CategoryForm />} />

        <Route path="/accounts" element={<AccountList />} />
        <Route path="/accounts/new" element={<AccountForm />} />
        <Route path="/accounts/:id" element={<AccountForm />} />
        <Route path="/accounts/:id/tiktok" element={<TikTokChannel />} />
      </Route>
    </Routes>
  );
}
