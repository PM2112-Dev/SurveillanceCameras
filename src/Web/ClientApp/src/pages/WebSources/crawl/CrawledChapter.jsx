import { Link, useParams, useSearchParams } from 'react-router-dom';
import { ArrowLeft, ExternalLink } from 'lucide-react';
import { webSourcesClient } from '../../../api/clients';
import { PageHeader } from '../../../components/common/PageHeader';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../../components/common/StateBlocks';
import { useAsyncData } from '../../../hooks/useAsyncData';

/** Nội dung một chương đọc thẳng từ trang nguồn — dùng để kiểm tra cấu hình crawl lấy đúng nội dung. */
export function CrawledChapter() {
  const { id } = useParams();
  const webSourceId = Number(id);
  const [params] = useSearchParams();
  const url = params.get('url') ?? '';
  const storyUrl = params.get('story');

  const chapter = useAsyncData(() => webSourcesClient.getCrawledChapter(webSourceId, url), [webSourceId, url]);
  const paragraphs = chapter.data?.paragraphs ?? [];

  return (
    <>
      <PageHeader
        title={chapter.data?.title || 'Nội dung chương'}
        description={chapter.data ? `${paragraphs.length} đoạn — lấy trực tiếp từ trang nguồn.` : undefined}
        actions={
          <>
            <a href={url} target="_blank" rel="noreferrer" role="button" className="secondary">
              <ExternalLink size={16} aria-hidden="true" /> Trang gốc
            </a>
            <Link
              to={storyUrl ? `/web-sources/${webSourceId}/story?url=${encodeURIComponent(storyUrl)}` : `/web-sources/${webSourceId}/stories`}
              role="button"
              className="secondary"
            >
              <ArrowLeft size={16} aria-hidden="true" /> Về truyện
            </Link>
          </>
        }
      />

      <ErrorAlert message={chapter.error} />

      {chapter.loading ? (
        <LoadingBlock label="Đang lấy nội dung chương…" />
      ) : paragraphs.length === 0 ? (
        !chapter.error && <EmptyState message="Chương không có nội dung." />
      ) : (
        <article className="crawl-reader">
          {paragraphs.map((text, index) => <p key={index}>{text}</p>)}
        </article>
      )}
    </>
  );
}
