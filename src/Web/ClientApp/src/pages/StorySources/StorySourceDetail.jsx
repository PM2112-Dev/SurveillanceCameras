import { useEffect, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import { ArrowLeft, CheckCircle2, ExternalLink, Pencil, RefreshCw, Sparkles } from 'lucide-react';
import { getErrorMessage, storySourcesClient, webSourcesClient } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { useAsyncData } from '../../hooks/useAsyncData';
import { CrawledChapterList } from '../WebSources/crawl/CrawledChapterList';
import { pageFromParams } from '../WebSources/crawl/CrawlPager';

/**
 * Chi tiết nguồn truyện: thông tin đã lưu trong hệ thống, kèm mục lục đọc thẳng từ trang gốc.
 * "Cập nhật từ trang nguồn" lấy lại mô tả, ảnh bìa… (đồng bộ từ danh sách không có các trường này).
 */
export function StorySourceDetail() {
  const { id } = useParams();
  const storySourceId = Number(id);
  const [params, setParams] = useSearchParams();
  const chapterPage = pageFromParams(params, 'chapterPage');

  const [reloadKey, setReloadKey] = useState(0);
  const story = useAsyncData(() => storySourcesClient.getStorySourceById(storySourceId), [storySourceId, reloadKey]);
  const info = story.data;

  const webSource = useAsyncData(
    () => (info?.webSourceId ? webSourcesClient.getWebSourceById(info.webSourceId) : Promise.resolve(null)),
    [info?.webSourceId]
  );

  const [refreshing, setRefreshing] = useState(false);
  const [refreshMessage, setRefreshMessage] = useState('');
  const [refreshError, setRefreshError] = useState('');

  const refresh = () => {
    setRefreshing(true);
    setRefreshMessage('');
    setRefreshError('');

    storySourcesClient.refreshStorySource(storySourceId)
      .then(() => {
        setRefreshMessage('Đã cập nhật thông tin từ trang nguồn.');
        setReloadKey(key => key + 1);
      })
      .catch(err => setRefreshError(getErrorMessage(err)))
      .finally(() => setRefreshing(false));
  };

  // Tạo truyện bằng AI chạy nền (Kafka): sau khi gửi yêu cầu, tải lại định kỳ tới khi có truyện hoặc báo lỗi.
  const [waitingForStory, setWaitingForStory] = useState(false);
  const [requesting, setRequesting] = useState(false);

  useEffect(() => {
    if (!waitingForStory) return undefined;
    if (info?.workId || info?.storyCreationError) {
      setWaitingForStory(false);
      return undefined;
    }
    const timer = setTimeout(() => setReloadKey(key => key + 1), 4000);
    return () => clearTimeout(timer);
  }, [waitingForStory, info]);

  const requestStory = () => {
    setRequesting(true);
    setRefreshMessage('');
    setRefreshError('');

    storySourcesClient.requestStoryCreation(storySourceId)
      .then(() => {
        setWaitingForStory(true);
        setReloadKey(key => key + 1);
      })
      .catch(err => setRefreshError(getErrorMessage(err)))
      .finally(() => setRequesting(false));
  };

  // Mục lục chỉ đọc được khi có link gốc và nguồn web đã khai báo cấu hình crawl.
  const canCrawl = Boolean(info?.linkRaw && webSource.data?.crawlConfig);

  return (
    <>
      <PageHeader
        title={info?.title || 'Chi tiết nguồn truyện'}
        description={webSource.data?.title ? `Nguồn web: ${webSource.data.title}` : undefined}
        actions={
          <>
            {canCrawl && (
              <button type="button" className="secondary" onClick={refresh} disabled={refreshing} aria-busy={refreshing}>
                {!refreshing && <RefreshCw size={16} aria-hidden="true" />} Cập nhật từ trang nguồn
              </button>
            )}
            {info?.linkRaw && (
              <a href={info.linkRaw} target="_blank" rel="noreferrer" role="button" className="secondary">
                <ExternalLink size={16} aria-hidden="true" /> Trang gốc
              </a>
            )}
            <Link to={`/story-sources/${storySourceId}/edit`} role="button" className="secondary">
              <Pencil size={16} aria-hidden="true" /> Sửa
            </Link>
            <Link to="/story-sources" role="button" className="secondary">
              <ArrowLeft size={16} aria-hidden="true" /> Nguồn truyện
            </Link>
          </>
        }
      />

      <ErrorAlert message={story.error || refreshError} />
      {refreshMessage && <p className="success" role="status"><CheckCircle2 size={16} aria-hidden="true" /> {refreshMessage}</p>}

      {info && (
        <div className="story-link-bar">
          {info.workId ? (
            <span>
              <CheckCircle2 size={16} aria-hidden="true" /> Đã tạo truyện —{' '}
              <Link to={`/stories/${info.workId}`}>mở truyện</Link>
            </span>
          ) : waitingForStory ? (
            <span aria-busy="true">AI đang biên tập tên và giới thiệu để tạo truyện…</span>
          ) : (
            <>
              <span>
                {info.storyCreationError
                  ? <span className="error">Tạo truyện bằng AI lỗi: {info.storyCreationError}</span>
                  : 'Chưa tạo truyện từ nguồn này.'}
              </span>
              <button type="button" onClick={requestStory} disabled={requesting} aria-busy={requesting}>
                {!requesting && <Sparkles size={16} aria-hidden="true" />}
                {info.storyCreationError ? ' Thử lại' : ' Tạo truyện bằng AI'}
              </button>
            </>
          )}
        </div>
      )}

      {story.loading && !info ? (
        <LoadingBlock />
      ) : info && (
        <section className="profile-card crawl-story">
          <div className="profile-head">
            {info.imageUrl && (
              <img className="crawl-story-cover" src={info.imageUrl} alt="" referrerPolicy="no-referrer"
                onError={event => { event.currentTarget.style.visibility = 'hidden'; }} />
            )}
            <div>
              <h2 className="profile-name">{info.title || <em>(không tên)</em>}</h2>
              <dl className="crawl-story-meta">
                {info.sinoVietnamese && <><dt>Hán Việt</dt><dd>{info.sinoVietnamese}</dd></>}
                <dt>Tác giả</dt><dd>{info.author || '—'}</dd>
                <dt>Tình trạng</dt><dd>{info.status || '—'}</dd>
                <dt>Số chương</dt><dd>{info.totalChapters ?? '—'}</dd>
                {info.lastUpdate && <><dt>Cập nhật</dt><dd>{info.lastUpdate}</dd></>}
              </dl>
              {info.categories?.length > 0 && (
                <div className="chip-row">
                  {info.categories.map(category => <span key={category.id} className="chip">{category.title}</span>)}
                </div>
              )}
            </div>
          </div>

          {info.description ? (
            <details className="crawl-story-desc" open>
              <summary>Giới thiệu</summary>
              {info.description.split('\n').map((line, index) => <p key={index}>{line}</p>)}
            </details>
          ) : (
            <p className="hint">
              Chưa có giới thiệu{canCrawl ? ' — bấm “Cập nhật từ trang nguồn” để lấy về.' : '.'}
            </p>
          )}
        </section>
      )}

      {canCrawl && (
        <CrawledChapterList
          webSourceId={info.webSourceId}
          storyUrl={info.linkRaw}
          page={chapterPage}
          onPageChange={next => setParams({ chapterPage: String(next) })}
        />
      )}
      {info && !webSource.loading && !canCrawl && (
        <p className="hint">
          Không xem được mục lục: {info.linkRaw ? 'nguồn web chưa có cấu hình crawl.' : 'nguồn truyện chưa có link trang gốc.'}
        </p>
      )}
    </>
  );
}
