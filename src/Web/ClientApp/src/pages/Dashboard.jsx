import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Globe, BookOpen, Tags } from 'lucide-react';
import { categoriesClient, storySourcesClient, webSourcesClient, getErrorMessage } from '../api/clients';
import { PageHeader } from '../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../components/common/StateBlocks';

export function Dashboard() {
  const [stats, setStats] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    Promise.all([
      webSourcesClient.getWebSource(undefined, 1, 1),
      storySourcesClient.getStorySources(undefined, undefined, 1, 1),
      categoriesClient.getCategories(undefined, 1, 1)
    ])
      .then(([webSources, storySources, categories]) =>
        setStats({
          webSources: webSources.totalCount ?? 0,
          storySources: storySources.totalCount ?? 0,
          categories: categories.totalCount ?? 0
        })
      )
      .catch(err => setError(getErrorMessage(err)));
  }, []);

  const cards = [
    { label: 'Nguồn web', value: stats?.webSources, to: '/web-sources', icon: Globe },
    { label: 'Nguồn truyện', value: stats?.storySources, to: '/story-sources', icon: BookOpen },
    { label: 'Thể loại', value: stats?.categories, to: '/categories', icon: Tags }
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
