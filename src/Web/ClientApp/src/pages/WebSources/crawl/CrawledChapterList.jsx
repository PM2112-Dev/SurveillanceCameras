import { Link } from 'react-router-dom';
import { webSourcesClient } from '../../../api/clients';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../../components/common/StateBlocks';
import { useAsyncData } from '../../../hooks/useAsyncData';
import { CrawlPager } from './CrawlPager';

/** Mục lục chương của một truyện, đọc thẳng từ trang nguồn (có phân trang). */
export function CrawledChapterList({ webSourceId, storyUrl, page, onPageChange }) {
  const chapters = useAsyncData(
    () => webSourcesClient.getCrawledChapters(webSourceId, storyUrl, page),
    [webSourceId, storyUrl, page]
  );
  const items = chapters.data?.items ?? [];

  return (
    <section aria-label="Mục lục">
      <h2 className="crawl-section-title">Mục lục</h2>

      <ErrorAlert message={chapters.error} />

      {chapters.loading ? (
        <LoadingBlock label="Đang lấy mục lục từ trang nguồn…" />
      ) : items.length === 0 ? (
        !chapters.error && <EmptyState message="Không lấy được chương nào." />
      ) : (
        <ol className="crawl-chapters">
          {items.map(chapter => (
            <li key={chapter.link}>
              <Link
                to={`/web-sources/${webSourceId}/chapter?url=${encodeURIComponent(chapter.link)}&story=${encodeURIComponent(storyUrl)}`}
              >
                {chapter.title || chapter.link}
              </Link>
            </li>
          ))}
        </ol>
      )}

      <CrawlPager
        page={page}
        totalPages={chapters.data?.totalPages}
        hasPreviousPage={page > 1}
        hasNextPage={Boolean(chapters.data?.hasNextPage)}
        onChange={onPageChange}
        disabled={chapters.loading}
      />
    </section>
  );
}
