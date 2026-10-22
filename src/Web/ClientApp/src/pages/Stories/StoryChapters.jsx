import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { Plus, RefreshCw, RotateCcw, Upload } from 'lucide-react';
import { episodesClient, getErrorMessage, worksClient } from '../../api/clients';
import { Pagination } from '../../components/common/Pagination';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { useAsyncData } from '../../hooks/useAsyncData';
import { CHAPTER_STATUS_INFO, ChapterStatus, ChapterStatusBadge } from '../Chapters/chapterStatus';

const PAGE_SIZE = 50;
const POLL_MS = 5000;

const numberFormat = new Intl.NumberFormat('vi-VN');

/** Tiến độ xử lý chương (chạy nền qua Kafka) và danh sách chương của truyện, phân trang ở server. */
export function StoryChapters({ storyId }) {
  const [params, setParams] = useSearchParams();
  const page = Math.max(1, Number(params.get('page')) || 1);
  const status = params.get('status') ?? '';

  const [reloadKey, setReloadKey] = useState(0);
  const [actionBusy, setActionBusy] = useState('');
  const [message, setMessage] = useState('');
  const [actionError, setActionError] = useState('');

  const stats = useAsyncData(() => worksClient.getWorkEpisodeStats(storyId), [storyId, reloadKey]);
  const list = useAsyncData(
    () => episodesClient.getEpisodes(storyId, undefined, status ? Number(status) : undefined, page, PAGE_SIZE),
    [storyId, page, status, reloadKey]
  );

  const s = stats.data;
  // Còn chương đang chờ tải/biên tập thì tự làm mới để thấy tiến độ.
  const inProgress = Boolean(s && (s.listed > 0 || s.fetched - s.waitingForPrompt > 0));

  useEffect(() => {
    if (!inProgress) return undefined;
    const timer = setTimeout(() => setReloadKey(key => key + 1), POLL_MS);
    return () => clearTimeout(timer);
  }, [inProgress, reloadKey]);

  const runAction = (name, action, successMessage) => {
    setActionBusy(name);
    setMessage('');
    setActionError('');
    action()
      .then(result => {
        setMessage(typeof successMessage === 'function' ? successMessage(result) : successMessage);
        setReloadKey(key => key + 1);
      })
      .catch(err => setActionError(getErrorMessage(err)))
      .finally(() => setActionBusy(''));
  };

  const setFilter = value => setParams(value ? { status: value } : {});
  const items = list.data?.items ?? [];
  const percent = count => (s?.total ? Math.round((count / s.total) * 100) : 0);
  const failed = (s?.fetchFailed ?? 0) + (s?.editFailed ?? 0);

  return (
    <section aria-label="Chương">
      <div className="chapter-toolbar">
        <h2 className="crawl-section-title">Chương</h2>
        <div className="page-header-actions">
          <button
            type="button"
            className="secondary"
            disabled={Boolean(actionBusy)}
            aria-busy={actionBusy === 'sync'}
            onClick={() => runAction('sync', () => worksClient.requestWorkEpisodeSync(storyId),
              'Đã gửi yêu cầu đồng bộ mục lục — chương mới sẽ được tải và biên tập ở nền.')}
          >
            {actionBusy !== 'sync' && <RefreshCw size={16} aria-hidden="true" />} Đồng bộ chương mới
          </button>
          <button
            type="button"
            className="secondary"
            disabled={Boolean(actionBusy) || !s || (failed === 0 && s.waitingForPrompt === 0 && s.listed === 0)}
            aria-busy={actionBusy === 'retry'}
            title="Tải lại chương lỗi/chưa tải, biên tập lại chương lỗi/chưa biên tập"
            onClick={() => runAction('retry', () => worksClient.retryWorkEpisodes(storyId),
              count => `Đã đưa ${count} chương vào hàng đợi xử lý lại.`)}
          >
            {actionBusy !== 'retry' && <RotateCcw size={16} aria-hidden="true" />} Xử lý lại chương dở
          </button>
          <button
            type="button"
            className="secondary"
            disabled={Boolean(actionBusy) || !s || s.edited === 0}
            aria-busy={actionBusy === 'upload'}
            title="Đăng các chương đã biên tập xong lên TYT theo thứ tự (cách nhau một khoảng)"
            onClick={() => runAction('upload', () => worksClient.requestTytChapterUploads(storyId, undefined),
              count => {
                // Kết quả đăng nằm ở nền: tải lại sau ít giây để thấy "Đã đăng" hoặc lỗi của từng chương.
                [6000, 20000, 60000].forEach(ms => setTimeout(() => setReloadKey(key => key + 1), ms));
                return count > 0
                  ? `Đã đưa ${count} chương vào hàng đợi đăng lên TYT — kết quả hiện ở cột TYT (tự làm mới).`
                  : 'Không có chương nào để đăng: chương phải ở trạng thái “Đã biên tập”, đứng liền sau các chương đã đăng, và chưa có lỗi đăng.';
              })}
          >
            {actionBusy !== 'upload' && <Upload size={16} aria-hidden="true" />} Đăng chương lên TYT
          </button>
          <Link to={`/stories/${storyId}/chapters/new`} role="button" className="secondary">
            <Plus size={16} aria-hidden="true" /> Thêm chương
          </Link>
        </div>
      </div>

      <ErrorAlert message={actionError || stats.error} />
      {message && <p className="success" role="status">{message}</p>}
      {s?.episodeSyncError && <p className="error">Đồng bộ mục lục lỗi: {s.episodeSyncError}</p>}

      {s && s.total > 0 && (
        <div className="chapter-progress">
          <div className="progress-bar" role="img" aria-label={`Đã biên tập ${s.edited}/${s.total} chương`}>
            <span className="ok" style={{ width: `${percent(s.edited)}%` }} />
            <span className="warn" style={{ width: `${percent(s.fetched)}%` }} />
            <span className="danger" style={{ width: `${percent(failed)}%` }} />
          </div>
          <small>
            {numberFormat.format(s.total)} chương · {numberFormat.format(s.edited)} đã biên tập ·{' '}
            {numberFormat.format(s.fetched)} đã tải chờ biên tập · {numberFormat.format(s.listed)} chờ tải
            {failed > 0 && ` · ${numberFormat.format(failed)} lỗi`}
            {inProgress && ' — đang xử lý ở nền…'}
          </small>
          {s.waitingForPrompt > 0 && (
            <small className="hint">
              {numberFormat.format(s.waitingForPrompt)} chương chưa biên tập vì chưa có prompt loại
              {' '}<code>edit-chapter-content</code> — thêm prompt rồi bấm “Xử lý lại chương dở”.
            </small>
          )}
        </div>
      )}

      <div className="filter-bar">
        <select aria-label="Trạng thái" value={status} onChange={event => setFilter(event.target.value)}>
          <option value="">Trạng thái: tất cả</option>
          {Object.values(ChapterStatus).map(value => (
            <option key={value} value={value}>{CHAPTER_STATUS_INFO[value].label}</option>
          ))}
        </select>
      </div>

      <ErrorAlert message={list.error} />

      {list.loading && !list.data ? (
        <LoadingBlock />
      ) : items.length === 0 ? (
        <EmptyState message={status ? 'Không có chương nào ở trạng thái này.' : 'Chưa có chương. Chương được lấy tự động sau khi tạo truyện, hoặc bấm “Đồng bộ chương mới”.'} />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>#</th>
                <th>Tên chương</th>
                <th>Trạng thái</th>
                <th>Độ dài</th>
                <th>TYT</th>
              </tr>
            </thead>
            <tbody>
              {items.map(chapter => (
                <tr key={chapter.id}>
                  <td>{chapter.episodeNumber}</td>
                  <td>
                    <Link to={`/stories/${storyId}/chapters/${chapter.id}`}>
                      {chapter.nameEdit || chapter.nameRaw || chapter.title || <em>(không tên)</em>}
                    </Link>
                    {chapter.error && <span className="cell-sub clamp-2">{chapter.error}</span>}
                  </td>
                  <td><ChapterStatusBadge status={chapter.status} error={chapter.error} /></td>
                  <td className="nowrap">
                    {chapter.rawLength ? numberFormat.format(chapter.rawLength) : '—'}
                    {chapter.editLength ? ` → ${numberFormat.format(chapter.editLength)}` : ''}
                  </td>
                  <td>
                    {chapter.isUploaded
                      ? <span className="status-badge ok" title={chapter.tytUploadedAt?.toLocaleString('vi-VN')}>Đã đăng</span>
                      : chapter.tytUploadError
                        ? <span className="status-badge danger" title={chapter.tytUploadError}>Lỗi đăng</span>
                        : '—'}
                    {chapter.tytUploadError && !chapter.isUploaded && (
                      <span className="cell-sub clamp-2 error">{chapter.tytUploadError}</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Pagination
        page={list.data?.pageNumber ?? page}
        totalPages={list.data?.totalPages ?? 0}
        totalCount={list.data?.totalCount ?? 0}
        hasPreviousPage={list.data?.hasPreviousPage ?? false}
        hasNextPage={list.data?.hasNextPage ?? false}
        onChange={next => setParams({ ...(status ? { status } : {}), page: String(next) })}
      />
    </section>
  );
}
