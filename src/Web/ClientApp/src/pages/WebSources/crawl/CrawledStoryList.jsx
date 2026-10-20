import { useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { ArrowLeft, CheckCircle2, ExternalLink, RefreshCw } from 'lucide-react';
import { SyncCrawledStoriesCommand, SyncCrawledStoryItem } from '../../../web-api-client';
import { getErrorMessage, webSourcesClient } from '../../../api/clients';
import { PageHeader } from '../../../components/common/PageHeader';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../../components/common/StateBlocks';
import { useAsyncData } from '../../../hooks/useAsyncData';
import { CrawlPager, pageFromParams } from './CrawlPager';

/** Danh sách truyện đọc thẳng từ trang nguồn của một nguồn web (không lưu DB). */
export function CrawledStoryList() {
  const { id } = useParams();
  const webSourceId = Number(id);
  const [params, setParams] = useSearchParams();
  const page = pageFromParams(params);

  const source = useAsyncData(() => webSourcesClient.getWebSourceById(webSourceId), [webSourceId]);
  // Tăng để tải lại trang hiện tại sau khi đồng bộ (cập nhật nhãn "Đã đồng bộ").
  const [reloadKey, setReloadKey] = useState(0);
  const list = useAsyncData(() => webSourcesClient.getCrawledStories(webSourceId, page), [webSourceId, page, reloadKey]);

  // Truyện đã chọn, theo link. Giữ qua các lần chuyển trang để chọn gom từ nhiều trang rồi đồng bộ một lần.
  const [selected, setSelected] = useState(() => new Map());
  const [syncing, setSyncing] = useState(false);
  const [syncMessage, setSyncMessage] = useState('');
  const [syncError, setSyncError] = useState('');

  const items = list.data?.items ?? [];
  const selectable = items.filter(story => story.link);
  const allOnPageSelected = selectable.length > 0 && selectable.every(story => selected.has(story.link));

  const toggle = story =>
    setSelected(prev => {
      const next = new Map(prev);
      if (next.has(story.link)) next.delete(story.link); else next.set(story.link, story);
      return next;
    });

  const togglePage = () =>
    setSelected(prev => {
      const next = new Map(prev);
      selectable.forEach(story => (allOnPageSelected ? next.delete(story.link) : next.set(story.link, story)));
      return next;
    });

  const sync = () => {
    setSyncing(true);
    setSyncMessage('');
    setSyncError('');

    const stories = [...selected.values()].map(story => new SyncCrawledStoryItem({
      link: story.link,
      title: story.title,
      author: story.author,
      coverUrl: story.coverUrl,
      status: story.status,
      chapterCount: story.chapterCount
    }));

    webSourcesClient.syncCrawledStories(webSourceId, new SyncCrawledStoriesCommand({ webSourceId, stories }))
      .then(result => {
        setSyncMessage(`Đã lưu ${stories.length} truyện (thêm mới ${result.created}, cập nhật ${result.updated}). `
          + `${result.queued} truyện đang được xếp hàng để lấy trang chi tiết lần lượt ở nền, cách nhau theo giãn cách của nguồn web; `
          + 'giới thiệu và thể loại sẽ có dần'
          + (result.created > 0 ? ', sau đó AI biên tập tên/giới thiệu để tạo truyện — theo dõi ở mục Nguồn truyện.' : '.'));
        setSelected(new Map());
        setReloadKey(key => key + 1);
      })
      .catch(err => setSyncError(getErrorMessage(err)))
      .finally(() => setSyncing(false));
  };

  const goToPage = next => {
    setParams({ page: String(next) });
    window.scrollTo({ top: 0 });
  };

  return (
    <>
      <PageHeader
        title={source.data?.title ? `Truyện trên ${source.data.title}` : 'Truyện trên nguồn web'}
        description="Dữ liệu lấy trực tiếp từ trang nguồn mỗi lần mở, không lưu vào hệ thống."
        actions={
          <>
            {list.data?.sourceUrl && (
              <a href={list.data.sourceUrl} target="_blank" rel="noreferrer" role="button" className="secondary">
                <ExternalLink size={16} aria-hidden="true" /> Trang gốc
              </a>
            )}
            <Link to="/web-sources" role="button" className="secondary">
              <ArrowLeft size={16} aria-hidden="true" /> Nguồn web
            </Link>
          </>
        }
      />

      <ErrorAlert message={source.error || list.error || syncError} />
      {syncMessage && <p className="success" role="status"><CheckCircle2 size={16} aria-hidden="true" /> {syncMessage}</p>}

      {selectable.length > 0 && (
        <div className="sync-bar">
          <label className="checkbox-item">
            <input type="checkbox" checked={allOnPageSelected} onChange={togglePage} disabled={list.loading} />
            Chọn cả trang ({selectable.length})
          </label>
          <span className="sync-bar-count">
            Đã chọn <strong>{selected.size}</strong> truyện
            {selected.size > 0 && (
              <button type="button" className="link-button" onClick={() => setSelected(new Map())}>Bỏ chọn</button>
            )}
          </span>
          <button type="button" onClick={sync} disabled={selected.size === 0 || syncing} aria-busy={syncing}>
            {!syncing && <RefreshCw size={16} aria-hidden="true" />}
            {syncing ? ' Đang lấy chi tiết từng truyện…' : ' Đồng bộ vào nguồn truyện'}
          </button>
        </div>
      )}

      {list.loading ? (
        <LoadingBlock label="Đang lấy danh sách truyện từ trang nguồn…" />
      ) : items.length === 0 ? (
        !list.error && (
          <EmptyState message="Không lấy được truyện nào. Kiểm tra cấu hình crawl, hoặc trang có thể cần cookie đăng nhập." />
        )
      ) : (
        <div className="crawl-grid">
          {items.map(story => (
            <div key={story.link} className={`crawl-card-wrap${selected.has(story.link) ? ' is-selected' : ''}`}>
              {story.link && (
                <input
                  type="checkbox"
                  className="crawl-card-check"
                  aria-label={`Chọn ${story.title}`}
                  checked={selected.has(story.link)}
                  onChange={() => toggle(story)}
                />
              )}
              <Link
                className="crawl-card"
                to={`/web-sources/${webSourceId}/story?url=${encodeURIComponent(story.link)}`}
              >
                {story.coverUrl ? (
                  <img src={story.coverUrl} alt="" loading="lazy" referrerPolicy="no-referrer"
                    onError={event => { event.currentTarget.style.visibility = 'hidden'; }} />
                ) : (
                  <span className="crawl-card-nocover" aria-hidden="true" />
                )}
                <span className="crawl-card-body">
                  <strong className="clamp-2">{story.title}</strong>
                  {story.author && <small>{story.author}</small>}
                  <span className="badge-row">
                    {story.status && <span className="chip">{story.status}</span>}
                    {story.chapterCount != null && <span className="chip">{story.chapterCount} chương</span>}
                    {story.isSynced && <span className="status-badge ok">Đã đồng bộ</span>}
                  </span>
                </span>
              </Link>
            </div>
          ))}
        </div>
      )}

      <CrawlPager
        page={page}
        totalPages={list.data?.totalPages}
        hasPreviousPage={page > 1}
        hasNextPage={Boolean(list.data?.hasNextPage)}
        onChange={goToPage}
        disabled={list.loading}
      />
    </>
  );
}
