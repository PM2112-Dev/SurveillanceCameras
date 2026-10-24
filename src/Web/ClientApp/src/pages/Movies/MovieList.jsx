import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Eye, EyeOff, RotateCcw } from 'lucide-react';
import { SetMoviesPublishedRequest } from '../../web-api-client';
import { getErrorMessage, languagesClient, moviesClient, userProfilesClient } from '../../api/clients';
import { getMoviesFiltered } from '../../api/movies';
import { retryMovieVideoStorage } from '../../api/storageAccounts';
import { useOptions } from '../../hooks/useOptions';
import { CrudList } from '../../components/common/CrudList';
import { ErrorAlert } from '../../components/common/StateBlocks';
import { formatDuration, numberFormat } from './movieFormat';

const PUBLISHED_OPTIONS = [
  { value: 'true', label: 'Đang hiện trên app' },
  { value: 'false', label: 'Chưa xuất bản / Đã gỡ' }
];

const COMPLETION_OPTIONS = [
  { value: 'full', label: 'Đủ tập (Full)' },
  { value: 'ongoing', label: 'Đang ra (Chưa đủ tập)' },
  { value: 'empty', label: 'Chưa có tập nào (0 tập)' }
];

const ACCESS_OPTIONS = [
  { value: 'vipOnly', label: 'Phim chỉ dành cho VIP' },
  { value: 'nonVipOnly', label: 'Phim thường (có miễn phí/QC)' },
  { value: 'custom', label: 'Có cấu hình quyền xem riêng' },
  { value: 'default', label: 'Quyền xem mặc định hệ thống' }
];

const STORAGE_OPTIONS = [
  { value: 'allStored', label: 'Đã lưu R2 đủ 100% tập' },
  { value: 'partialStored', label: 'Đã lưu R2 một phần' },
  { value: 'noneStored', label: 'Chưa lưu tập nào lên R2' },
  { value: 'hasUnstored', label: 'Còn tập chưa lưu R2' },
  { value: 'storageError', label: 'Có tập lỗi lưu R2' }
];

const ISSUE_OPTIONS = [
  { value: 'any', label: 'Có vấn đề cần kiểm tra' },
  { value: 'syncError', label: 'Lỗi đồng bộ phim' },
  { value: 'storageError', label: 'Có tập lỗi lưu R2' },
  { value: 'missingSource', label: 'Có tập mất nguồn phát' },
  { value: 'duplicate', label: 'Có tập trùng số tập' },
  { value: 'healthy', label: 'Không có lỗi' }
];

// Giá trị của enum MovieSort ở API; rỗng = mới nhất (0).
const SORT_OPTIONS = [
  { value: '1', label: 'Lượt xem: cao → thấp' },
  { value: '2', label: 'Lượt xem: thấp → cao' },
  { value: '3', label: 'Số tập: nhiều → ít' },
  { value: '4', label: 'Số tập: ít → nhiều' },
  { value: '5', label: 'Thời lượng: dài → ngắn' },
  { value: '6', label: 'Mới đồng bộ tập gần nhất' },
  { value: '7', label: 'Tên phim: A → Z' },
  { value: '8', label: 'Cũ nhất trước' }
];

export function MovieList() {
  const languages = useOptions(() => languagesClient.getLanguages(undefined, 1, 100));
  const channels = useOptions(
    () => userProfilesClient.getUserProfiles(undefined, undefined, undefined, 1, 200),
    item => item.title || (item.uniqueId ? `@${item.uniqueId}` : `#${item.id}`)
  );
  const [reloadKey, setReloadKey] = useState(0);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const channelOptions = [
    ...channels,
    { value: '-1', label: '(Chưa gắn kênh)' }
  ];

  const languageOptions = [
    ...languages,
    { value: '-1', label: '(Chưa gán ngôn ngữ)' }
  ];

  const togglePublished = item => {
    setError('');
    setMessage('');
    moviesClient.setMoviesPublished(new SetMoviesPublishedRequest({ isPublished: !item.isPublished, ids: [item.id] }))
      .then(() => {
        setMessage(item.isPublished
          ? `Đã gỡ “${item.title || `#${item.id}`}” khỏi app.`
          : `Đã xuất bản “${item.title || `#${item.id}`}” lên app.`);
        setReloadKey(k => k + 1);
      })
      .catch(err => setError(getErrorMessage(err)));
  };

  const retryStorage = item => {
    setError('');
    setMessage('');
    retryMovieVideoStorage(item.id)
      .then(count => {
        setMessage(count > 0
          ? `Đã đưa ${count} tập lỗi của “${item.title || `#${item.id}`}” vào hàng chờ lưu R2 lại.`
          : 'Không có tập nào đang lỗi lưu R2.');
        setReloadKey(k => k + 1);
      })
      .catch(err => setError(err.message));
  };

  return (
    <>
      <ErrorAlert message={error} />
      {message && <p className="cell-sub">{message}</p>}
      <CrudList
        key={reloadKey}
        title="Phim"
        description="Phim video nhiều tập (không phải truyện chữ), đồng bộ từ drama của kênh TikTok đã lưu. Dùng các bộ lọc bên dưới để lọc theo kênh, ngôn ngữ, trạng thái xuất bản, VIP, tiến độ lưu R2 hoặc các phim có lỗi cần xử lý."
        basePath="/movies"
        editable={false}
        searchPlaceholder="Tìm theo tên phim, ID drama hoặc kênh…"
        emptyMessage="Không có phim nào khớp bộ lọc hiện tại."
        deleteTitle="Xoá phim"
        filters={[
          { name: 'userProfileId', label: 'Kênh', options: channelOptions },
          { name: 'languageId', label: 'Ngôn ngữ', options: languageOptions },
          { name: 'isPublished', label: 'Trên app', options: PUBLISHED_OPTIONS },
          { name: 'completion', label: 'Bộ tập', options: COMPLETION_OPTIONS },
          { name: 'access', label: 'Quyền xem', options: ACCESS_OPTIONS },
          { name: 'storage', label: 'Lưu trữ R2', options: STORAGE_OPTIONS },
          { name: 'issue', label: 'Tình trạng', options: ISSUE_OPTIONS },
          { name: 'sort', label: 'Sắp xếp', allLabel: 'Mới đồng bộ trước', options: SORT_OPTIONS }
        ]}
        columns={[
          {
            header: 'Phim',
            render: item => (
              <span className="profile-inline">
                {item.coverUrl && <img src={item.coverUrl} alt="" referrerPolicy="no-referrer" />}
                <span>
                  <Link to={`/movies/${item.id}`}>{item.title || <em>(không tên)</em>}</Link>
                  {item.isFull && <small className="status-badge ok">Đủ tập</small>}
                  {item.isPublished && <small className="status-badge ok">Trên app</small>}
                  {item.isVipOnly && <small className="status-badge warn">Phim VIP</small>}
                  {item.hasCustomAccess && !item.isVipOnly && <small className="status-badge">Quyền riêng</small>}
                  {item.episodeSyncError && (
                    <small className="status-badge danger" title={item.episodeSyncError}>Lỗi đồng bộ</small>
                  )}
                </span>
              </span>
            )
          },
          {
            header: 'Kênh',
            render: item => item.userProfileId
              ? <Link to={`/user-profiles/${item.userProfileId}/dramas`}>{item.userProfileTitle || `#${item.userProfileId}`}</Link>
              : '—'
          },
          { header: 'Ngôn ngữ', render: item => item.languageTitle || item.languageCode || '—' },
          {
            header: 'Lượt xem',
            render: item => (item.viewCount != null ? numberFormat.format(item.viewCount) : '—')
          },
          { header: 'Số tập', render: item => item.episodeCount ?? 0 },
          {
            header: 'Lưu trữ R2',
            render: item => {
              const total = item.episodeCount ?? 0;
              const stored = item.storedEpisodeCount ?? 0;
              const failed = item.failedStorageEpisodeCount ?? 0;
              const missing = item.missingSourceEpisodeCount ?? 0;
              if (total === 0) return '—';
              return (
                <span>
                  <span>{stored}/{total} tập</span>
                  {stored === total && <small className="status-badge ok" style={{ marginLeft: 6 }}>Đủ R2</small>}
                  {failed > 0 && <small className="status-badge danger" style={{ marginLeft: 6 }}>{failed} lỗi R2</small>}
                  {missing > 0 && <small className="status-badge warn" style={{ marginLeft: 6 }}>{missing} mất nguồn</small>}
                </span>
              );
            }
          },
          { header: 'Thời lượng', render: item => formatDuration(item.totalDurationSeconds) }
        ]}
        rowActions={item => (
          <>
            {item.failedStorageEpisodeCount > 0 && (
              <button
                type="button"
                className="icon-btn"
                title={`Thử lưu lại ${item.failedStorageEpisodeCount} tập lỗi R2`}
                onClick={() => retryStorage(item)}
              >
                <RotateCcw size={16} aria-hidden="true" />
              </button>
            )}
            <button
              type="button"
              className="icon-btn"
              title={item.isPublished ? 'Gỡ khỏi app' : 'Xuất bản lên app'}
              onClick={() => togglePublished(item)}
            >
              {item.isPublished ? <EyeOff size={16} aria-hidden="true" /> : <Eye size={16} aria-hidden="true" />}
            </button>
          </>
        )}
        fetchPage={({ search, page, pageSize, filters }) =>
          getMoviesFiltered({
            title: search,
            userProfileId: filters.userProfileId ? Number(filters.userProfileId) : undefined,
            languageId: filters.languageId ? Number(filters.languageId) : undefined,
            isPublished: filters.isPublished !== undefined && filters.isPublished !== '' ? filters.isPublished : undefined,
            completion: filters.completion || undefined,
            access: filters.access || undefined,
            storage: filters.storage || undefined,
            issue: filters.issue || undefined,
            sort: Number(filters.sort || 0),
            page,
            pageSize
          })}
        deleteItem={item => moviesClient.deleteMovie(item.id)}
      />
    </>
  );
}
