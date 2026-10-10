import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Globe, BookOpen, Bot, Library, MessageSquareText, Tags, Users } from 'lucide-react';
import {
  accountsClient,
  aisClient,
  categoriesClient,
  promptsClient,
  worksClient,
  storySourcesClient,
  webSourcesClient,
  getErrorMessage
} from '../api/clients';
import { PageHeader } from '../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../components/common/StateBlocks';

export function Dashboard() {
  const [stats, setStats] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    // Bọc trong then() để lỗi ném đồng bộ (sai tham số client) cũng vào catch thay vì làm trắng trang.
    Promise.resolve()
      .then(() => Promise.all([
        webSourcesClient.getWebSource(undefined, 1, 1),
        storySourcesClient.getStorySources(undefined, undefined, 1, 1),
        categoriesClient.getCategories(undefined, 1, 1),
        worksClient.getWorks(undefined, 1, 1),
        accountsClient.getAccounts(undefined, undefined, 1, 1),
        aisClient.getAIs(undefined, undefined, 1, 1),
        promptsClient.getPrompts(undefined, undefined, 1, 1)
      ]))
      .then(([webSources, storySources, categories, stories, accounts, ais, prompts]) =>
        setStats({
          webSources: webSources.totalCount ?? 0,
          storySources: storySources.totalCount ?? 0,
          categories: categories.totalCount ?? 0,
          stories: stories.totalCount ?? 0,
          accounts: accounts.totalCount ?? 0,
          ais: ais.totalCount ?? 0,
          prompts: prompts.totalCount ?? 0
        })
      )
      .catch(err => setError(getErrorMessage(err)));
  }, []);

  const cards = [
    { label: 'Nguồn web', value: stats?.webSources, to: '/web-sources', icon: Globe },
    { label: 'Nguồn truyện', value: stats?.storySources, to: '/story-sources', icon: BookOpen },
    { label: 'Thể loại', value: stats?.categories, to: '/categories', icon: Tags },
    { label: 'Truyện', value: stats?.stories, to: '/stories', icon: Library },
    { label: 'Tài khoản', value: stats?.accounts, to: '/accounts', icon: Users },
    { label: 'AI', value: stats?.ais, to: '/ais', icon: Bot },
    { label: 'Prompt', value: stats?.prompts, to: '/prompts', icon: MessageSquareText }
  ];

  return (
    <>
      <PageHeader title="Tổng quan" description="Tình hình dữ liệu đang quản lý trong hệ thống." />

      <ErrorAlert message={error} />

      {!stats && !error ? (
        <LoadingBlock />
      ) : (
        <div className="stat-grid">
          {cards.map(({ label, value, to, icon: Icon }) => (
            <Link key={to} to={to} className="stat-card">
              <Icon size={22} aria-hidden="true" />
              <span className="stat-value">{value ?? 0}</span>
              <span className="stat-label">{label}</span>
            </Link>
          ))}
        </div>
      )}
    </>
  );
}
