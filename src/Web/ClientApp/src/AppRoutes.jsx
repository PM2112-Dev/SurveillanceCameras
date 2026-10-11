import { Route, Routes } from 'react-router-dom';
import { AppLayout } from './components/AppLayout';
import { AuthLayout } from './components/AuthLayout';
import { LoginPage } from './components/api-authorization/LoginPage';
import { RegisterPage } from './components/api-authorization/RegisterPage';
import { ProtectedRoute } from './components/api-authorization/ProtectedRoute';
import { Dashboard } from './pages/Dashboard';
import { WebSourceList } from './pages/WebSources/WebSourceList';
import { WebSourceForm } from './pages/WebSources/WebSourceForm';
import { CrawledStoryList } from './pages/WebSources/crawl/CrawledStoryList';
import { CrawledStoryDetail } from './pages/WebSources/crawl/CrawledStoryDetail';
import { CrawledChapter } from './pages/WebSources/crawl/CrawledChapter';
import { StorySourceList } from './pages/StorySources/StorySourceList';
import { StorySourceForm } from './pages/StorySources/StorySourceForm';
import { StorySourceDetail } from './pages/StorySources/StorySourceDetail';
import { CategoryList } from './pages/Categories/CategoryList';
import { CategoryForm } from './pages/Categories/CategoryForm';
import { AccountList } from './pages/Accounts/AccountList';
import { AccountForm } from './pages/Accounts/AccountForm';
import { TikTokChannel } from './pages/Accounts/TikTokChannel';
import { StorageAccountList } from './pages/StorageAccounts/StorageAccountList';
import { StorageAccountForm } from './pages/StorageAccounts/StorageAccountForm';
import { AccountTypeList } from './pages/AccountTypes/AccountTypeList';
import { AccountTypeForm } from './pages/AccountTypes/AccountTypeForm';
import { AIList } from './pages/AIs/AIList';
import { AIForm } from './pages/AIs/AIForm';
import { PromptTypeList } from './pages/PromptTypes/PromptTypeList';
import { PromptTypeForm } from './pages/PromptTypes/PromptTypeForm';
import { PromptList } from './pages/Prompts/PromptList';
import { PromptForm } from './pages/Prompts/PromptForm';
import { StoryList } from './pages/Stories/StoryList';
import { StoryForm } from './pages/Stories/StoryForm';
import { StoryDetail } from './pages/Stories/StoryDetail';
import { MovieList } from './pages/Movies/MovieList';
import { MovieDetail } from './pages/Movies/MovieDetail';
import { ChannelDramas } from './pages/Movies/ChannelDramas';
import { ChapterDetail } from './pages/Chapters/ChapterDetail';
import { StoryVideoTypeList } from './pages/StoryVideoTypes/StoryVideoTypeList';
import { StoryVideoTypeForm } from './pages/StoryVideoTypes/StoryVideoTypeForm';
import { UserProfileList } from './pages/UserProfiles/UserProfileList';
import { UserProfileTypeList } from './pages/UserProfileTypes/UserProfileTypeList';
import { UserProfileTypeForm } from './pages/UserProfileTypes/UserProfileTypeForm';
import { ChapterForm } from './pages/Chapters/ChapterForm';

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
        <Route path="/web-sources/:id/stories" element={<CrawledStoryList />} />
        <Route path="/web-sources/:id/story" element={<CrawledStoryDetail />} />
        <Route path="/web-sources/:id/chapter" element={<CrawledChapter />} />

        <Route path="/story-sources" element={<StorySourceList />} />
        <Route path="/story-sources/new" element={<StorySourceForm />} />
        <Route path="/story-sources/:id" element={<StorySourceDetail />} />
        <Route path="/story-sources/:id/edit" element={<StorySourceForm />} />

        <Route path="/categories" element={<CategoryList />} />
        <Route path="/categories/new" element={<CategoryForm />} />
        <Route path="/categories/:id" element={<CategoryForm />} />

        <Route path="/accounts" element={<AccountList />} />
        <Route path="/accounts/new" element={<AccountForm />} />
        <Route path="/accounts/:id" element={<AccountForm />} />
        <Route path="/accounts/:id/tiktok" element={<TikTokChannel />} />

        <Route path="/account-types" element={<AccountTypeList />} />
        <Route path="/account-types/new" element={<AccountTypeForm />} />
        <Route path="/account-types/:id" element={<AccountTypeForm />} />

        <Route path="/storage-accounts" element={<StorageAccountList />} />
        <Route path="/storage-accounts/new" element={<StorageAccountForm />} />
        <Route path="/storage-accounts/:id" element={<StorageAccountForm />} />

        <Route path="/user-profiles" element={<UserProfileList />} />
        <Route path="/user-profiles/:id/dramas" element={<ChannelDramas />} />
        <Route path="/movies" element={<MovieList />} />
        <Route path="/movies/:id" element={<MovieDetail />} />

        <Route path="/user-profile-types" element={<UserProfileTypeList />} />
        <Route path="/user-profile-types/new" element={<UserProfileTypeForm />} />
        <Route path="/user-profile-types/:id" element={<UserProfileTypeForm />} />

        <Route path="/ais" element={<AIList />} />
        <Route path="/ais/new" element={<AIForm />} />
        <Route path="/ais/:id" element={<AIForm />} />

        <Route path="/prompt-types" element={<PromptTypeList />} />
        <Route path="/prompt-types/new" element={<PromptTypeForm />} />
        <Route path="/prompt-types/:id" element={<PromptTypeForm />} />

        <Route path="/prompts" element={<PromptList />} />
        <Route path="/prompts/new" element={<PromptForm />} />
        <Route path="/prompts/:id" element={<PromptForm />} />

        <Route path="/stories" element={<StoryList />} />
        <Route path="/stories/new" element={<StoryForm />} />
        <Route path="/stories/:id" element={<StoryDetail />} />
        <Route path="/stories/:id/edit" element={<StoryForm />} />
        {/* Chương nằm trong truyện, không có menu riêng. */}
        <Route path="/stories/:storyId/chapters/new" element={<ChapterForm />} />
        <Route path="/stories/:storyId/chapters/:id" element={<ChapterDetail />} />
        <Route path="/stories/:storyId/chapters/:id/edit" element={<ChapterForm />} />

        <Route path="/story-video-types" element={<StoryVideoTypeList />} />
        <Route path="/story-video-types/new" element={<StoryVideoTypeForm />} />
        <Route path="/story-video-types/:id" element={<StoryVideoTypeForm />} />

      </Route>
    </Routes>
  );
}
