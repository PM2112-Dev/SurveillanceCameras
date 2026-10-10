import { useEffect, useRef, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, RefreshCw } from 'lucide-react';
import { SyncTikTokDramasCommand } from '../../web-api-client';
import { getErrorMessage, moviesClient, userProfilesClient } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { useAsyncData } from '../../hooks/useAsyncData';
import { formatDuration, numberFormat } from './movieFormat';

const PAGE_SIZE = 20;
const MAX_SYNC = 20;

const idOf = drama => drama.dramaId ?? drama.dramaID;
const coverOf = drama => drama.zoomCover?.['240'] ?? drama.cover?.urlList?.[0];

/** Drama của một kênh TikTok đã lưu (đọc thẳng từ TikTok); chọn drama để đồng bộ thành phim. */
export function ChannelDramas() {
  const { id } = useParams();
  const channelId = Number(id);
  const channel = useAsyncData(() => userProfilesClient.getUserProfileById(channelId), [channelId]);

  const [items, setItems] = useState([]);
  const [movieIds, setMovieIds] = useState({});
  const [cursor, setCursor] = useState('0');
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [selected, setSelected] = useState(() => new Set());
  const [syncing, setSyncing] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');
  const latest = useRef(0);

  const loadPage = (pageCursor, append) => {
    const key = append ? latest.current : ++latest.current;
    append ? setLoadingMore(true) : setLoading(true);
    setError('');

    return userProfilesClient.getChannelDramas(channelId, pageCursor, PAGE_SIZE)
      .then(page => {
        if (key !== latest.current) return;
        setItems(prev => {
          const base = append ? prev : [];
          const seen = new Set(base.map(idOf));
          return [...base, ...(page.items ?? []).filter(d => !seen.has(idOf(d)))];
        });
        setMovieIds(prev => ({ ...(append ? prev : {}), ...(page.movieIds ?? {}) }));
        setCursor(page.cursor ?? pageCursor);
        setHasMore(Boolean(page.hasMore));
      })
      .catch(err => { if (key === latest.current) setError(getErrorMessage(err)); })
      .finally(() => { if (key === latest.current) { setLoading(false); setLoadingMore(false); } });
  };

  useEffect(() => {
    setSelected(new Set());
    loadPage('0', false);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [channelId]);

  const toggle = dramaId => setSelected(prev => {
    const next = new Set(prev);
    next.has(dramaId) ? next.delete(dramaId) : next.size < MAX_SYNC && next.add(dramaId);
    return next;
  });

  const toggleAll = () => setSelected(prev => (prev.size > 0
    ? new Set()
    : new Set(items.map(idOf).filter(dramaId => !movieIds[dramaId]).slice(0, MAX_SYNC))));

  const sync = () => {
    setSyncing(true);
    setMessage('');
    setError('');
    moviesClient.syncTikTokDramas(new SyncTikTokDramasCommand({ userProfileId: channelId, dramaIds: [...selected] }))
      .then(result => {
        setMessage(`Đã tạo ${result.created} phim, cập nhật ${result.updated} phim, thêm ${result.episodesAdded} tập.`);
        if (result.errors?.length) setError(result.errors.join('\n'));
        setSelected(new Set());
        return loadPage('0', false);
      })
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setSyncing(false));
  };

  const info = channel.data;

  return (
    <>
      <PageHeader
        title={`Drama của ${info?.title || info?.uniqueId || 'kênh'}`}
        description={`Chọn drama rồi bấm Đồng bộ để lưu thành phim cùng các tập (tối đa ${MAX_SYNC} drama mỗi lần). Drama đã đồng bộ thì đồng bộ lại để thêm tập mới.`}
        actions={
          <>
            <button type="button" onClick={sync} disabled={selected.size === 0 || syncing} aria-busy={syncing}>
              <RefreshCw size={16} aria-hidden="true" /> Đồng bộ {selected.size > 0 ? `(${selected.size})` : ''}
            </button>
            <Link to="/user-profiles" role="button" className="secondary">
              <ArrowLeft size={16} aria-hidden="true" /> Kênh đã lưu
            </Link>
          </>
        }
      />

      <ErrorAlert message={channel.error || error} />
      {message && <p className="cell-sub">{message}</p>}

      {loading ? (
        <LoadingBlock label="Đang tải danh sách drama từ TikTok…" />
      ) : items.length === 0 ? (
        !error && <EmptyState message="Kênh này chưa có drama nào." />
      ) : (
        <>
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>
                    <input type="checkbox" aria-label="Chọn tất cả drama chưa đồng bộ"
                      checked={selected.size > 0} onChange={toggleAll} />
                  </th>
                  <th>Bìa</th>
                  <th>Drama</th>
                  <th>Số tập</th>
                  <th>Lượt xem</th>
                  <th>Thời lượng</th>
                  <th>Phim</th>
                </tr>
              </thead>
              <tbody>
                {items.map(drama => {
                  const dramaId = idOf(drama);
                  const cover = coverOf(drama);
                  const movieId = movieIds[dramaId];

                  return (
                    <tr key={dramaId}>
                      <td>
                        <input type="checkbox" aria-label={`Chọn ${drama.dramaName}`}
                          checked={selected.has(dramaId)} onChange={() => toggle(dramaId)} />
                      </td>
                      <td>
                        {cover && (
                          <img className="drama-cover" src={cover} alt="" loading="lazy" referrerPolicy="no-referrer"
                            onError={event => { event.currentTarget.style.visibility = 'hidden'; }} />
                        )}
                      </td>
                      <td>
                        <strong>{drama.dramaName}</strong>
                        <span className="cell-sub clamp-2">{drama.description}</span>
                        <span className="chip-row">
                          {(drama.themes ?? []).map(theme => (
                            <span key={theme.tagID ?? theme.tagId} className="chip">{theme.tagVal ?? theme.tagValue}</span>
                          ))}
                        </span>
                      </td>
                      <td className="nowrap">{numberFormat.format(drama.numVideos ?? 0)}</td>
                      <td className="nowrap">{numberFormat.format(drama.numWatched ?? 0)}</td>
                      <td className="nowrap">{formatDuration(drama.totalDuration)}</td>
                      <td className="nowrap">
                        {movieId ? <Link to={`/movies/${movieId}`}>Đã đồng bộ</Link> : '—'}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {hasMore && (
            <div className="load-more">
              <button type="button" className="secondary" onClick={() => loadPage(cursor, true)}
                disabled={loadingMore} aria-busy={loadingMore}>
                Tải thêm
              </button>
            </div>
          )}
        </>
      )}
    </>
  );
}
