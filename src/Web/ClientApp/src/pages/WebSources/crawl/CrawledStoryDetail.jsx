import { Link, useParams, useSearchParams } from 'react-router-dom';
import { ArrowLeft, ExternalLink } from 'lucide-react';
import { webSourcesClient } from '../../../api/clients';
import { PageHeader } from '../../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../../components/common/StateBlocks';
import { useAsyncData } from '../../../hooks/useAsyncData';
import { pageFromParams } from './CrawlPager';
import { CrawledChapterList } from './CrawledChapterList';

/** Thông tin truyện và mục lục chương đọc thẳng từ trang nguồn. */
export function CrawledStoryDetail() {
  const { id } = useParams();
  const webSourceId = Number(id);
  const [params, setParams] = useSearchParams();
  const url = params.get('url') ?? '';
  const chapterPage = pageFromParams(params, 'chapterPage');

  const story = useAsyncData(() => webSourcesClient.getCrawledStory(webSourceId, url), [webSourceId, url]);

  const info = story.data;

  return (
    <>
      <PageHeader
        title={info?.title || 'Thông tin truyện'}
        description="Dữ liệu lấy trực tiếp từ trang nguồn, không lưu vào hệ thống."
        actions={
          <>
            <a href={url} target="_blank" rel="noreferrer" role="button" className="secondary">
              <ExternalLink size={16} aria-hidden="true" /> Trang gốc
            </a>
            <Link to={`/web-sources/${webSourceId}/stories`} role="button" className="secondary">
              <ArrowLeft size={16} aria-hidden="true" /> Danh sách truyện
            </Link>
          </>
        }
      />

      <ErrorAlert message={story.error} />

      {story.loading ? (
        <LoadingBlock label="Đang lấy thông tin truyện…" />
      ) : info && (
        <section className="profile-card crawl-story">
          <div className="profile-head">
            {info.coverUrl && (
              <img className="crawl-story-cover" src={info.coverUrl} alt="" referrerPolicy="no-referrer"
                onError={event => { event.currentTarget.style.visibility = 'hidden'; }} />
            )}
            <div>
              <h2 className="profile-name">{info.title}</h2>
              <dl className="crawl-story-meta">
                {info.author && <><dt>Tác giả</dt><dd>{info.author}</dd></>}
                {info.status && <><dt>Tình trạng</dt><dd>{info.status}</dd></>}
                {info.chapterCount != null && <><dt>Số chương</dt><dd>{info.chapterCount}</dd></>}
              </dl>
              {info.genres?.length > 0 && (
                <div className="chip-row">
                  {info.genres.map(genre => <span key={genre} className="chip">{genre}</span>)}
                </div>
              )}
            </div>
          </div>

          {info.description && (
            <details className="crawl-story-desc" open>
              <summary>Giới thiệu</summary>
              {info.description.split('\n').map((line, index) => <p key={index}>{line}</p>)}
            </details>
          )}
        </section>
      )}

      <CrawledChapterList
        webSourceId={webSourceId}
        storyUrl={url}
        page={chapterPage}
        onPageChange={next => setParams({ url, chapterPage: String(next) })}
      />
    </>
  );
}
