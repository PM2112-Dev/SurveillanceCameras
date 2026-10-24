import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Check, CloudUpload, ExternalLink, Eye, EyeOff, Pencil, RefreshCw, RotateCcw, Search, Sparkles, Trash2, X } from 'lucide-react';
import {
  CleanupMovieEpisodesRequest,
  DeleteMovieEpisodesRequest,
  SetMovieEpisodeStreamRequest,
  SetMoviesPublishedRequest,
  SyncTikTokDramasCommand,
  UpdateMovieEpisodeRequest
} from '../../web-api-client';
import { getErrorMessage, moviesClient } from '../../api/clients';
import { getMovieEpisodesFiltered } from '../../api/movies';
import { retryMovieVideoStorage, storeMovieEpisodeVideo } from '../../api/storageAccounts';
import { PageHeader } from '../../components/common/PageHeader';
import { Pagination } from '../../components/common/Pagination';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { useAsyncData } from '../../hooks/useAsyncData';
import { MovieAccessForm, effectiveAccessRule, episodeAccess } from './MovieAccessForm';
import { dateTimeFormat, formatDuration, numberFormat } from './movieFormat';
import { formatBytes } from '../StorageAccounts/storageFormat';

const PAGE_SIZE = 50;

const EPISODE_FILTER_OPTIONS = [
  { value: '', label: 'Loại tập: tất cả' },
  { value: 'issues', label: 'Tập có vấn đề (lỗi / trùng / mất nguồn)' },
  { value: 'stored', label: 'Đã lưu video R2' },
  { value: 'unstored', label: 'Chưa lưu video R2' },
  { value: 'storageError', label: 'Đang lỗi lưu video R2' },
  { value: 'missingSource', label: 'Mất nguồn phát' },
  { value: 'duplicate', label: 'Trùng số tập' },
  { value: 'edited', label: 'Đã sửa tay' }
];

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
  const [episodeFilter, setEpisodeFilter] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [episodeSearch, setEpisodeSearch] = useState('');
  const [selected, setSelected] = useState(() => new Set());
  const [editing, setEditing] = useState(null);   // { id, episodeNumber, name }
  const [cleanup, setCleanup] = useState(null);   // { removeBroken, renumber, preview, busy }

  const movie = useAsyncData(() => moviesClient.getMovieById(movieId), [movieId, version]);
  // Mặc định hệ thống (số tập miễn phí / quảng cáo) — endpoint công khai của app.
  const appConfig = useAsyncData(() => fetch('/api/app/config').then(response => (response.ok ? response.json() : null)), []);
  const episodes = useAsyncData(
    () => getMovieEpisodesFiltered(movieId, {
      page,
      pageSize: PAGE_SIZE,
      filter: episodeFilter || undefined,
      search: episodeSearch || undefined
    }),
    [movieId, page, version, episodeFilter, episodeSearch]
  );
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

  const run = (action, successMessage) => {
    setMessage('');
    setError('');
    return action()
      .then(result => {
        if (successMessage) setMessage(typeof successMessage === 'function' ? successMessage(result) : successMessage);
        setVersion(v => v + 1);
        return result;
      })
      .catch(err => setError(getErrorMessage(err)));
  };

  const saveEdit = () => run(
    () => moviesClient.updateMovieEpisode(movieId, editing.id,
      new UpdateMovieEpisodeRequest({ episodeNumber: Number(editing.episodeNumber), name: editing.name })),
    'Đã lưu tập.'
  ).then(() => setEditing(null));

  const deleteEpisodes = (ids, label) => {
    if (!window.confirm(`Xoá ${label}? File video đã lưu ở kho cũng bị xoá, và đồng bộ lại sẽ không thêm lại.`)) return;
    run(() => moviesClient.deleteMovieEpisodes(movieId, new DeleteMovieEpisodesRequest({ episodeIds: ids })),
      count => `Đã xoá ${count} tập.`).then(() => setSelected(new Set()));
  };

  const toggleSelected = id => setSelected(current => {
    const next = new Set(current);
    if (next.has(id)) next.delete(id); else next.add(id);
    return next;
  });

  const previewCleanup = (options = {}) => {
    const next = { removeBroken: true, renumber: false, ...cleanup, ...options, busy: true };
    setCleanup(next);
    setError('');
    moviesClient.cleanupMovieEpisodes(movieId, new CleanupMovieEpisodesRequest({
      apply: false, removeBroken: next.removeBroken, renumber: next.renumber
    }))
      .then(preview => setCleanup(current => ({ ...current, preview, busy: false })))
      .catch(err => { setError(getErrorMessage(err)); setCleanup(null); });
  };

  const applyCleanup = () => {
    setCleanup(current => ({ ...current, busy: true }));
    moviesClient.cleanupMovieEpisodes(movieId, new CleanupMovieEpisodesRequest({
      apply: true, removeBroken: cleanup.removeBroken, renumber: cleanup.renumber
    }))
      .then(result => {
        setMessage(`Đã dọn: xoá ${result.duplicates} bản sao, ${result.broken} tập hỏng, cho ${result.storageRetried} tập lưu lại, đổi số ${result.renumbered} tập.`);
        setCleanup(null);
        setVersion(v => v + 1);
      })
      .catch(err => { setError(getErrorMessage(err)); setCleanup(current => ({ ...current, busy: false })); });
  };

  const handleEpisodeSearch = event => {
    event.preventDefault();
    setPage(1);
    setEpisodeSearch(searchInput.trim());
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
            <button type="button" className="secondary" onClick={() => (cleanup ? setCleanup(null) : previewCleanup())}>
              <Sparkles size={16} aria-hidden="true" /> Dọn tập lỗi/trùng
            </button>
            {((info?.failedStorageEpisodeCount ?? 0) > 0 || items.some(episode => episode.storageError)) && (
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
            <dt>Lưu trữ R2</dt>
            <dd>
              {info.storedEpisodeCount ?? 0} / {info.episodeCount ?? 0} tập
              {(info.episodeCount ?? 0) > 0 && info.storedEpisodeCount === info.episodeCount && (
                <span className="status-badge ok" style={{ marginLeft: 6 }}>Đủ R2</span>
              )}
              {(info.failedStorageEpisodeCount ?? 0) > 0 && (
                <span className="status-badge danger" style={{ marginLeft: 6 }}>{info.failedStorageEpisodeCount} lỗi R2</span>
              )}
              {(info.missingSourceEpisodeCount ?? 0) > 0 && (
                <span className="status-badge warn" style={{ marginLeft: 6 }}>{info.missingSourceEpisodeCount} mất nguồn</span>
              )}
            </dd>
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

      {cleanup && (
        <section className="profile-card" aria-label="Dọn tập lỗi hoặc trùng">
          <h3>Dọn tập lỗi / trùng</h3>
          {cleanup.busy && !cleanup.preview ? <LoadingBlock /> : cleanup.preview && (
            <>
              <p className="cell-sub">
                {cleanup.preview.duplicates} bản sao (cùng số tập, cùng nội dung) · {cleanup.preview.broken} tập không còn nguồn phát ·
                {' '}{cleanup.preview.conflicts} nhóm cùng số nhưng khác nội dung · {cleanup.preview.storageRetried} tập lưu video lỗi
              </p>
              {cleanup.preview.details?.length > 0
                ? <ul>{cleanup.preview.details.map((line, index) => <li key={index}>{line}</li>)}</ul>
                : <p>Không có gì cần dọn.</p>}
              <label className="checkbox-item">
                <input type="checkbox" checked={cleanup.removeBroken} disabled={cleanup.busy}
                  onChange={event => previewCleanup({ removeBroken: event.target.checked })} />
                Xoá tập không còn link TikTok lẫn file phát
              </label>
              <label className="checkbox-item">
                <input type="checkbox" checked={cleanup.renumber} disabled={cleanup.busy}
                  onChange={event => previewCleanup({ renumber: event.target.checked })} />
                Đánh số lại tập từ 1 theo thứ tự hiện tại (xử lý nhóm cùng số khác nội dung)
              </label>
              <div className="page-header-actions">
                <button type="button" onClick={applyCleanup} disabled={cleanup.busy} aria-busy={cleanup.busy}>Áp dụng</button>
                <button type="button" className="secondary" onClick={() => setCleanup(null)}>Đóng</button>
              </div>
            </>
          )}
        </section>
      )}

      <div className="chapter-toolbar">
        <h2>Danh sách tập</h2>
        <div className="page-header-actions">
          {selected.size > 0 && (
            <button type="button" className="secondary danger" onClick={() => deleteEpisodes([...selected], `${selected.size} tập đã chọn`)}>
              <Trash2 size={16} aria-hidden="true" /> Xoá {selected.size} tập
            </button>
          )}
        </div>
      </div>

      <form className="filter-bar" onSubmit={handleEpisodeSearch}>
        <select
          aria-label="Lọc tập phim"
          value={episodeFilter}
          onChange={event => { setEpisodeFilter(event.target.value); setPage(1); }}
        >
          {EPISODE_FILTER_OPTIONS.map(opt => (
            <option key={opt.value} value={opt.value}>{opt.label}</option>
          ))}
        </select>
        <input
          type="search"
          placeholder="Tìm theo số tập, tên tập hoặc ID video…"
          value={searchInput}
          onChange={event => setSearchInput(event.target.value)}
        />
        <button type="submit">
          <Search size={16} aria-hidden="true" /> Tìm
        </button>
      </form>

      {episodes.loading && !episodes.data ? (
        <LoadingBlock />
      ) : items.length === 0 ? (
        <EmptyState message={episodeFilter || episodeSearch ? 'Không có tập nào khớp bộ lọc.' : 'Phim chưa có tập nào.'} />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th aria-label="Chọn"></th>
                <th>Tập</th>
                <th>Ảnh</th>
                <th>Quyền xem</th>
                <th>Ngôn ngữ</th>
                <th>Thời lượng</th>
                <th>Video</th>
                <th>Video tự lưu trữ (app)</th>
                <th className="col-actions">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {items.map(episode => (
                <tr key={episode.id}>
                  <td>
                    <input type="checkbox" checked={selected.has(episode.id)} onChange={() => toggleSelected(episode.id)}
                      aria-label={`Chọn tập ${episode.episodeNumber}`} />
                  </td>
                  <td>
                    {editing?.id === episode.id ? (
                      <span className="inline-edit">
                        <input type="number" min={1} value={editing.episodeNumber} style={{ width: '6rem' }}
                          onChange={event => setEditing({ ...editing, episodeNumber: event.target.value })} aria-label="Số tập" />
                        <input type="text" maxLength={2000} value={editing.name} placeholder="Tên tập"
                          onChange={event => setEditing({ ...editing, name: event.target.value })} aria-label="Tên tập" />
                      </span>
                    ) : (
                      <>
                        <span className="nowrap">{episode.title || `Tập ${episode.episodeNumber}`}</span>
                        {episode.nameRaw && <span className="cell-sub clamp-2">{episode.nameRaw}</span>}
                        {episode.isDuplicate && <span className="status-badge danger" title="Có tập khác cùng số tập">Trùng số tập</span>}
                        {episode.isMissingSource && <span className="status-badge danger" title="Không còn link TikTok lẫn file phát">Thiếu nguồn</span>}
                        {episode.isEdited && <span className="status-badge" title="Đã sửa tay — đồng bộ lại không ghi đè">Đã sửa</span>}
                      </>
                    )}
                  </td>
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
                  <td className="col-actions">
                    {editing?.id === episode.id ? (
                      <>
                        <button type="button" className="icon-btn" title="Lưu" onClick={saveEdit}><Check size={16} aria-hidden="true" /></button>
                        <button type="button" className="icon-btn" title="Huỷ" onClick={() => setEditing(null)}><X size={16} aria-hidden="true" /></button>
                      </>
                    ) : (
                      <>
                        <button type="button" className="icon-btn" title="Sửa số tập và tên"
                          onClick={() => setEditing({ id: episode.id, episodeNumber: episode.episodeNumber, name: episode.nameRaw ?? '' })}>
                          <Pencil size={16} aria-hidden="true" />
                        </button>
                        <button type="button" className="icon-btn danger" title="Xoá tập"
                          onClick={() => deleteEpisodes([episode.id], `tập ${episode.episodeNumber}`)}>
                          <Trash2 size={16} aria-hidden="true" />
                        </button>
                      </>
                    )}
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
