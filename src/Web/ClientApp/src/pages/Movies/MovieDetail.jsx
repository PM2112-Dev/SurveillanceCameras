import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, CloudUpload, ExternalLink, Eye, EyeOff, Pencil, RefreshCw, RotateCcw } from 'lucide-react';
import { SetMovieEpisodeStreamRequest, SetMoviesPublishedRequest, SyncTikTokDramasCommand } from '../../web-api-client';
import { getErrorMessage, moviesClient } from '../../api/clients';
import { retryMovieVideoStorage, storeMovieEpisodeVideo } from '../../api/storageAccounts';
import { PageHeader } from '../../components/common/PageHeader';
import { Pagination } from '../../components/common/Pagination';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { useAsyncData } from '../../hooks/useAsyncData';
import { MovieAccessForm, effectiveAccessRule, episodeAccess } from './MovieAccessForm';
import { dateTimeFormat, formatDuration, numberFormat } from './movieFormat';
import { formatBytes } from '../StorageAccounts/storageFormat';

const PAGE_SIZE = 50;

/** Chi tiết phim: thông tin và danh sách tập (tập nằm trong phim, không có menu riêng). */
export function MovieDetail() {
  const { id } = useParams();
  const movieId = Number(id);
  const [page, setPage] = useState(1);
  const [version, setVersion] = useState(0);
  const [syncing, setSyncing] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const [storingId, setStoringId] = useState(null);

  const movie = useAsyncData(() => moviesClient.getMovieById(movieId), [movieId, version]);
  // Mặc định hệ thống (số tập miễn phí / quảng cáo) — endpoint công khai của app.
  const appConfig = useAsyncData(() => fetch('/api/app/config').then(response => (response.ok ? response.json() : null)), []);
  const episodes = useAsyncData(() => moviesClient.getMovieEpisodes(movieId, page, PAGE_SIZE), [movieId, page, version]);
  const info = movie.data;

  const resync = () => {
    setSyncing(true);
    setMessage('');
    setError('');
    moviesClient.syncTikTokDramas(new SyncTikTokDramasCommand({ userProfileId: info.userProfileId, dramaIds: [info.externalId] }))
      .then(result => {
        if (result.errors?.length) setError(result.errors.join('\n'));
        else setMessage(`Đã đồng bộ lại — thêm ${result.episodesAdded} tập mới.`);
        setVersion(v => v + 1);
      })
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setSyncing(false));
  };

  const togglePublished = () => {
    setMessage('');
    setError('');
    moviesClient.setMoviesPublished(new SetMoviesPublishedRequest({ isPublished: !info.isPublished, ids: [movieId] }))
      .then(() => {
        setMessage(info.isPublished ? 'Đã gỡ phim khỏi app.' : 'Phim đã hiện trên app.');
        setVersion(v => v + 1);
      })
      .catch(err => setError(getErrorMessage(err)));
  };

  const editStream = episode => {
    const url = window.prompt(
      `Link file video (https, mp4/HLS) của ${episode.title || `tập ${episode.episodeNumber}`}. Để trống để gỡ.`,
      episode.streamUrl ?? ''
    );
    if (url === null) return;
    setMessage('');
    setError('');
    moviesClient.setMovieEpisodeStream(movieId, episode.id, new SetMovieEpisodeStreamRequest({ streamUrl: url.trim() }))
      .then(() => setVersion(v => v + 1))
      .catch(err => setError(getErrorMessage(err)));
  };

  const retryStorage = () => {
    setMessage('');
    setError('');
    retryMovieVideoStorage(movieId)
      .then(count => {
        setMessage(count > 0
          ? `Đã đưa ${count} tập vào hàng chờ lưu video lại.`
          : 'Không có tập nào đang lỗi lưu video.');
        setVersion(v => v + 1);
      })
      .catch(err => setError(err.message));
  };

  const storeNow = episode => {
    setStoringId(episode.id);
    setMessage('');
    setError('');
    storeMovieEpisodeVideo(movieId, episode.id)
      .then(result => {
        if (result.succeeded) setMessage(`Đã lưu video tập ${episode.episodeNumber} vào kho (${formatBytes(result.sizeBytes)}).`);
        else setError(`Tập ${episode.episodeNumber}: ${result.error}`);
        setVersion(v => v + 1);
      })
      .catch(err => setError(err.message))
      .finally(() => setStoringId(null));
  };

  const items = episodes.data?.items ?? [];
  const accessRule = effectiveAccessRule(info, appConfig.data);
  const accessLabels = { free: 'Miễn phí', ads: 'Quảng cáo', vip: 'VIP' };

  return (
    <>
      <PageHeader
        title={info?.title || 'Chi tiết phim'}
        actions={
          <>
            {info?.userProfileId && info?.externalId && (
              <button type="button" className="secondary" onClick={resync} disabled={syncing} aria-busy={syncing}>
                <RefreshCw size={16} aria-hidden="true" /> Đồng bộ lại
              </button>
            )}
            {items.some(episode => episode.storageError) && (
              <button type="button" className="secondary" onClick={retryStorage}>
                <RotateCcw size={16} aria-hidden="true" /> Lưu video lại
              </button>
            )}
            {info && (
              <button type="button" className="secondary" onClick={togglePublished}>
                {info.isPublished
                  ? <><EyeOff size={16} aria-hidden="true" /> Gỡ khỏi app</>
                  : <><Eye size={16} aria-hidden="true" /> Xuất bản lên app</>}
              </button>
            )}
            <Link to="/movies" role="button" className="secondary">
              <ArrowLeft size={16} aria-hidden="true" /> Phim
            </Link>
          </>
        }
      />

      <ErrorAlert message={movie.error || episodes.error || error} />
      {message && <p className="cell-sub">{message}</p>}

      {movie.loading && !info ? (
        <LoadingBlock />
      ) : info && (
        <section className="profile-card crawl-story">
          {info.coverUrl && <img className="crawl-story-cover" src={info.coverUrl} alt="" referrerPolicy="no-referrer" />}
          <dl className="crawl-story-meta">
            <dt>Kênh nguồn</dt>
            <dd>
              {info.userProfileId
                ? <Link to={`/user-profiles/${info.userProfileId}/dramas`}>{info.userProfileTitle || `#${info.userProfileId}`}</Link>
                : '—'}
            </dd>
            <dt>ID drama</dt><dd>{info.externalId || '—'}</dd>
            <dt>Ngôn ngữ</dt><dd>{info.languageTitle || info.languageCode || '—'}</dd>
            <dt>Lượt xem</dt><dd>{info.viewCount != null ? numberFormat.format(info.viewCount) : '—'}</dd>
            <dt>Số tập</dt><dd>{info.episodeCount ?? 0}{info.isFull && <span className="status-badge ok">Đủ tập</span>}</dd>
            <dt>Tổng thời lượng</dt><dd>{formatDuration(info.totalDurationSeconds)}</dd>
            <dt>Trên app</dt>
            <dd>{info.isPublished ? <span className="status-badge ok">Đang hiện</span> : 'Chưa xuất bản'}</dd>
            <dt>Đồng bộ lúc</dt><dd>{info.episodesSyncedAt ? dateTimeFormat.format(info.episodesSyncedAt) : '—'}</dd>
          </dl>
          {info.episodeSyncError && <p className="error">Lỗi đồng bộ gần nhất: {info.episodeSyncError}</p>}
          {info.description && (
            <details className="crawl-story-desc" open>
              <summary>Giới thiệu</summary>
              {info.description.split('\n').map((line, index) => <p key={index}>{line}</p>)}
            </details>
          )}
        </section>
      )}

      {info && (
        <MovieAccessForm
          key={`${info.id}:${info.hasCustomAccess}:${info.isVipOnly}:${info.adFromEpisode}:${info.adToEpisode}:${info.vipFromEpisode}:${info.vipToEpisode}`}
          movie={info}
          config={appConfig.data}
          onSaved={() => setVersion(v => v + 1)}
        />
      )}

      <h2>Danh sách tập</h2>
      {episodes.loading && !episodes.data ? (
        <LoadingBlock />
      ) : items.length === 0 ? (
        <EmptyState message="Phim chưa có tập nào." />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tập</th>
                <th>Ảnh</th>
                <th>Quyền xem</th>
                <th>Ngôn ngữ</th>
                <th>Thời lượng</th>
                <th>Video</th>
                <th>Video tự lưu trữ (app)</th>
              </tr>
            </thead>
            <tbody>
              {items.map(episode => (
                <tr key={episode.id}>
                  <td className="nowrap">{episode.title || `Tập ${episode.episodeNumber}`}</td>
                  <td>
                    {episode.thumbnailUrl && (
                      <img className="drama-cover" src={episode.thumbnailUrl} alt="" loading="lazy" referrerPolicy="no-referrer"
                        onError={event => { event.currentTarget.style.visibility = 'hidden'; }} />
                    )}
                  </td>
                  <td>{accessLabels[episodeAccess(accessRule, episode.episodeNumber)] ?? '—'}</td>
                  <td>{episode.languageCode || '—'}</td>
                  <td className="nowrap">{episode.durationSeconds ? formatDuration(episode.durationSeconds) : '—'}</td>
                  <td>
                    {episode.videoUrl ? (
                      <a href={episode.videoUrl} target="_blank" rel="noreferrer">
                        <ExternalLink size={14} aria-hidden="true" /> Xem trên TikTok
                      </a>
                    ) : '—'}
                  </td>
                  <td>
                    {episode.streamUrl
                      ? <a href={episode.streamUrl} target="_blank" rel="noreferrer">Link video</a>
                      : <span className="cell-sub">Chưa có</span>}
                    {' '}
                    <button type="button" className="secondary outline" onClick={() => editStream(episode)}
                      aria-label={`Sửa link video tập ${episode.episodeNumber}`}>
                      <Pencil size={14} aria-hidden="true" />
                    </button>
                    {!episode.streamUrl && episode.videoUrl && (
                      <>
                        {' '}
                        <button type="button" className="secondary outline" onClick={() => storeNow(episode)}
                          disabled={storingId !== null} aria-busy={storingId === episode.id}
                          title="Tải video từ TikTok và lưu vào kho ngay"
                          aria-label={`Lưu video tập ${episode.episodeNumber} vào kho`}>
                          <CloudUpload size={14} aria-hidden="true" />
                        </button>
                      </>
                    )}
                    {episode.storageAccountId != null && (
                      <span className="cell-sub">Đã lưu vào kho — {formatBytes(episode.storageSizeBytes)}</span>
                    )}
                    {episode.storageError && <span className="cell-sub">Lưu video lỗi: {episode.storageError}</span>}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Pagination
        page={episodes.data?.pageNumber ?? page}
        totalPages={episodes.data?.totalPages ?? 0}
        totalCount={episodes.data?.totalCount ?? 0}
        hasPreviousPage={episodes.data?.hasPreviousPage ?? false}
        hasNextPage={episodes.data?.hasNextPage ?? false}
        onChange={setPage}
      />
    </>
  );
}
