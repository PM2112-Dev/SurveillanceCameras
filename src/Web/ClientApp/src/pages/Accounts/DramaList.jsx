import { useEffect, useMemo, useRef, useState } from 'react';
import { Check, Copy } from 'lucide-react';
import { accountsClient, getErrorMessage } from '../../api/clients';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';

const PAGE_SIZE = 20;

const numberFormat = new Intl.NumberFormat('vi-VN');

/** Tổng thời lượng TikTok trả về tính bằng giây. */
function formatDuration(totalSeconds) {
  const seconds = Number(totalSeconds) || 0;
  const hours = Math.floor(seconds / 3600);
  const minutes = Math.floor((seconds % 3600) / 60);

  return hours > 0 ? `${hours} giờ ${minutes} phút` : `${minutes} phút`;
}

const coverOf = drama => drama.zoomCover?.['240'] ?? drama.cover?.urlList?.[0];

/** Danh sách drama của một kênh (theo secUid), tải từng trang bằng cursor. */
export function DramaList({ accountId, secUid }) {
  const [items, setItems] = useState([]);
  const [cursor, setCursor] = useState('0');
  const [hasMore, setHasMore] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState('');
  const [copiedId, setCopiedId] = useState(null);

  // Bỏ kết quả của secUid cũ khi người dùng tra cứu kênh khác giữa chừng.
  const latestKey = useRef(0);

  useEffect(() => {
    const key = ++latestKey.current;
    setItems([]);
    setCursor('0');
    setHasMore(false);
    setFilter('');
    setError('');
    setLoading(true);

    accountsClient.getTikTokDramaList(accountId, secUid, PAGE_SIZE, '0')
      .then(page => {
        if (key !== latestKey.current) return;
        setItems(page.dramaList ?? []);
        setCursor(page.cursor ?? '0');
        setHasMore(Boolean(page.hasMore));
      })
      .catch(err => {
        if (key === latestKey.current) setError(getErrorMessage(err));
      })
      .finally(() => {
        if (key === latestKey.current) setLoading(false);
      });
  }, [accountId, secUid]);

  const loadMore = () => {
    const key = latestKey.current;
    setLoadingMore(true);
    setError('');

    accountsClient.getTikTokDramaList(accountId, secUid, PAGE_SIZE, cursor)
      .then(page => {
        if (key !== latestKey.current) return;
        // Chống trùng nếu TikTok trả lại drama đã có ở trang trước.
        setItems(prev => {
          const seen = new Set(prev.map(d => d.dramaID ?? d.dramaId));
          return [...prev, ...(page.dramaList ?? []).filter(d => !seen.has(d.dramaID ?? d.dramaId))];
        });
        setCursor(page.cursor ?? cursor);
        setHasMore(Boolean(page.hasMore));
      })
      .catch(err => {
        if (key === latestKey.current) setError(getErrorMessage(err));
      })
      .finally(() => {
        if (key === latestKey.current) setLoadingMore(false);
      });
  };

  const copyId = drama => {
    const id = drama.dramaId ?? drama.dramaID;
    navigator.clipboard.writeText(id ?? '')
      .then(() => {
        setCopiedId(id);
        setTimeout(() => setCopiedId(null), 1500);
      })
      .catch(() => {});
  };

  const visible = useMemo(() => {
    const term = filter.trim().toLowerCase();
    return term ? items.filter(d => (d.dramaName ?? '').toLowerCase().includes(term)) : items;
  }, [items, filter]);

  return (
    <section className="drama-section" aria-label="Danh sách drama">
      <div className="drama-section-head">
        <h2>Danh sách drama</h2>
        {!loading && items.length > 0 && (
          <small>
            Đã tải {items.length} drama{hasMore ? ' (còn nữa)' : ''}
          </small>
        )}
      </div>

      <ErrorAlert message={error} />

      {loading ? (
        <LoadingBlock label="Đang tải danh sách drama…" />
      ) : items.length === 0 ? (
        !error && <EmptyState message="Kênh này chưa có drama nào." />
      ) : (
        <>
          <div className="filter-bar">
            <input
              type="search"
              placeholder="Lọc theo tên drama trong danh sách đã tải…"
              value={filter}
              onChange={event => setFilter(event.target.value)}
            />
          </div>

          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Bìa</th>
                  <th>Drama</th>
                  <th>Số tập</th>
                  <th>Lượt xem</th>
                  <th>Thời lượng</th>
                </tr>
              </thead>
              <tbody>
                {visible.map(drama => {
                  const id = drama.dramaId ?? drama.dramaID;
                  const cover = coverOf(drama);

                  return (
                    <tr key={id}>
                      <td>
                        {cover && (
                          <img
                            className="drama-cover"
                            src={cover}
                            alt=""
                            loading="lazy"
                            referrerPolicy="no-referrer"
                            onError={event => { event.currentTarget.style.visibility = 'hidden'; }}
                          />
                        )}
                      </td>
                      <td>
                        <strong>{drama.dramaName}</strong>
                        {drama.isLimitedFree && <span className="status-badge ok drama-free">Miễn phí</span>}
                        <span className="cell-sub clamp-2">{drama.description}</span>
                        <span className="chip-row">
                          {(drama.themes ?? []).map(theme => (
                            <span key={theme.tagID ?? theme.tagId} className="chip">{theme.tagVal ?? theme.tagValue}</span>
                          ))}
                        </span>
                        <span className="cell-sub">
                          ID: <code>{id}</code>
                          <button
                            type="button"
                            className="icon-btn"
                            title="Sao chép ID drama"
                            onClick={() => copyId(drama)}
                          >
                            {copiedId === id ? <Check size={14} aria-hidden="true" /> : <Copy size={14} aria-hidden="true" />}
                          </button>
                        </span>
                      </td>
                      <td className="nowrap">{numberFormat.format(drama.numVideos ?? 0)}</td>
                      <td className="nowrap">{numberFormat.format(drama.numWatched ?? 0)}</td>
                      <td className="nowrap">{formatDuration(drama.totalDuration)}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {visible.length === 0 && <EmptyState message="Không có drama nào khớp bộ lọc." />}

          {hasMore && (
            <div className="load-more">
              <button type="button" className="secondary" onClick={loadMore} disabled={loadingMore} aria-busy={loadingMore}>
                Tải thêm
              </button>
            </div>
          )}
        </>
      )}
    </section>
  );
}
