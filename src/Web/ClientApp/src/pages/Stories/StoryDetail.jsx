import { Link, useParams } from 'react-router-dom';
import { useState } from 'react';
import { ArrowLeft, ExternalLink, Pencil, Upload } from 'lucide-react';
import { accountsClient, accountTypesClient, getErrorMessage, worksClient } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { useAsyncData } from '../../hooks/useAsyncData';
import { StoryChapters } from './StoryChapters';

/** Chi tiết truyện: thông tin, và các chương của truyện (chương không có menu riêng). */
export function StoryDetail() {
  const { id } = useParams();
  const storyId = Number(id);

  const story = useAsyncData(() => worksClient.getWorkById(storyId), [storyId]);
  const info = story.data;
  const account = useAsyncData(
    () => (info?.accountId ? accountsClient.getAccountById(info.accountId) : Promise.resolve(null)),
    [info?.accountId]
  );

  const accountType = useAsyncData(
    () => (account.data?.accountTypeId ? accountTypesClient.getAccountTypeById(account.data.accountTypeId) : Promise.resolve(null)),
    [account.data?.accountTypeId]
  );
  const [tytBusy, setTytBusy] = useState(false);
  const [tytMessage, setTytMessage] = useState('');

  const createOnTyt = () => {
    setTytBusy(true);
    setTytMessage('');
    worksClient.requestTytStoryCreation(storyId)
      .then(() => setTytMessage('Đã đưa vào hàng đợi — tải lại trang sau ít phút để xem kết quả.'))
      .catch(err => setTytMessage(getErrorMessage(err)))
      .finally(() => setTytBusy(false));
  };

  // Domain TYT lấy theo loại tài khoản đăng truyện, không hard-code.
  const tytOrigin = accountType.data?.baseUrl;

  return (
    <>
      <PageHeader
        title={info?.title || 'Chi tiết truyện'}
        actions={
          <>
            {info?.storySource?.linkRaw && (
              <a href={info.storySource.linkRaw} target="_blank" rel="noreferrer" role="button" className="secondary">
                <ExternalLink size={16} aria-hidden="true" /> Trang gốc
              </a>
            )}
            <Link to={`/stories/${storyId}/edit`} role="button" className="secondary">
              <Pencil size={16} aria-hidden="true" /> Sửa
            </Link>
            <Link to="/stories" role="button" className="secondary">
              <ArrowLeft size={16} aria-hidden="true" /> Truyện
            </Link>
          </>
        }
      />

      <ErrorAlert message={story.error} />

      {story.loading && !info ? (
        <LoadingBlock />
      ) : info && (
        <section className="profile-card crawl-story">
          <dl className="crawl-story-meta">
            <dt>Tác giả</dt><dd>{info.author || '—'}</dd>
            <dt>Nguồn truyện</dt>
            <dd>
              {info.storySourceId
                ? <Link to={`/story-sources/${info.storySourceId}`}>{info.storySource?.title || `#${info.storySourceId}`}</Link>
                : '—'}
            </dd>
            <dt>Tài khoản đăng</dt>
            <dd>{account.data?.title || (info.accountId ? `#${info.accountId}` : <em>Chưa gắn — bấm Sửa để chọn</em>)}</dd>
            <dt>Loại video</dt><dd>{info.storyVideoType?.title || '—'}</dd>
            <dt>ID trên TYT</dt>
            <dd>
              {info.tytStoryId
                ? (tytOrigin
                  ? <a href={new URL(`/truyen/${info.tytStoryId}`, tytOrigin).toString()} target="_blank" rel="noreferrer">{info.tytStoryId}</a>
                  : info.tytStoryId)
                : '—'}
              {info.tytUpdatedAt && (
                <small className="cell-sub">Cập nhật thông tin lên TYT lúc {info.tytUpdatedAt.toLocaleString('vi-VN')}</small>
              )}
              {info.tytError && <small className="cell-sub error">Lỗi: {info.tytError}</small>}
              {info.accountId && (
                <button type="button" className="secondary" disabled={tytBusy} onClick={createOnTyt}
                  title={info.tytStoryId ? 'Gửi lại tên, tác giả, giới thiệu, trạng thái lên TYT' : 'Tạo truyện trên TYT rồi cập nhật thông tin'}>
                  <Upload size={14} aria-hidden="true" /> {info.tytStoryId ? 'Cập nhật lên TYT' : 'Tạo trên TYT'}
                </button>
              )}
              {tytMessage && <small className="cell-sub">{tytMessage}</small>}
            </dd>
          </dl>
          <div className="badge-row">
            {info.isFull && <span className="status-badge ok">Hoàn thành</span>}
            {info.isPublished && <span className="status-badge ok">Đã đăng</span>}
            {info.isEarning && <span className="status-badge ok">Kiếm tiền</span>}
          </div>
          {info.description && (
            <details className="crawl-story-desc">
              <summary>Giới thiệu</summary>
              {info.description.split('\n').map((line, index) => <p key={index}>{line}</p>)}
            </details>
          )}
        </section>
      )}

      {info && <StoryChapters storyId={storyId} />}
    </>
  );
}
