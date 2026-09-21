import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { LogIn, Pencil, Plus, Search, Square, Trash2, UserSearch } from 'lucide-react';
import { accountsClient, accountTypesClient, getErrorMessage } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { Pagination } from '../../components/common/Pagination';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { SessionState, formatDateTime, getSessionInfo } from './tiktokSession';

const PAGE_SIZE = 20;
const STATUS_POLL_MS = 5000;

export function AccountList() {
  const [result, setResult] = useState(null);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);
  const [accountTypes, setAccountTypes] = useState({});
  const [sessions, setSessions] = useState({});
  const [busyIds, setBusyIds] = useState(() => new Set());

  const load = useCallback(() => {
    setLoading(true);
    setError('');

    accountsClient.getAccounts(search || undefined, undefined, page, PAGE_SIZE)
      .then(setResult)
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setLoading(false));
  }, [page, search]);

  useEffect(load, [load]);

  // Tên loại tài khoản chỉ để hiển thị; lỗi ở đây không được chặn danh sách.
  useEffect(() => {
    accountTypesClient.getAccountTypes(undefined, 1, 100)
      .then(types => setAccountTypes(Object.fromEntries((types.items ?? []).map(t => [t.id, t.title]))))
      .catch(() => {});
  }, []);

  const items = useMemo(() => result?.items ?? [], [result]);

  // Trạng thái phiên TikTok thay đổi ở phía server (đăng nhập xong, bị đăng xuất, người dùng tắt Chrome…)
  // nên phải hỏi lại định kỳ.
  useEffect(() => {
    if (items.length === 0) return undefined;

    let cancelled = false;
    const poll = () => {
      Promise.allSettled(items.map(item => accountsClient.getTikTokSessionStatus(item.id)))
        .then(results => {
          if (cancelled) return;
          setSessions(prev => {
            const next = { ...prev };
            results.forEach((r, index) => {
              if (r.status === 'fulfilled') next[items[index].id] = r.value;
            });
            return next;
          });
        });
    };

    poll();
    const timer = setInterval(poll, STATUS_POLL_MS);

    return () => {
      cancelled = true;
      clearInterval(timer);
    };
  }, [items]);

  const handleSearch = event => {
    event.preventDefault();
    setPage(1);
    setSearch(searchInput.trim());
  };

  const handleDelete = () => {
    setDeleting(true);

    accountsClient.deleteAccount(deleteTarget.id)
      .then(() => {
        setDeleteTarget(null);
        load();
      })
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setDeleting(false));
  };

  const setBusy = (id, busy) =>
    setBusyIds(prev => {
      const next = new Set(prev);
      if (busy) next.add(id); else next.delete(id);
      return next;
    });

  const runSessionAction = (item, action) => {
    setBusy(item.id, true);
    setError('');

    action(item.id)
      .then(() => accountsClient.getTikTokSessionStatus(item.id))
      .then(status => setSessions(prev => ({ ...prev, [item.id]: status })))
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setBusy(item.id, false));
  };

  return (
    <>
      <PageHeader
        title="Tài khoản"
        description="Tài khoản nền tảng (TikTok…) và cookie đăng nhập dùng để đăng nội dung."
        actions={
          <Link to="/accounts/new" role="button">
            <Plus size={16} aria-hidden="true" /> Thêm tài khoản
          </Link>
        }
      />

      <p className="hint">
        Bấm <strong>Mở Chrome</strong> để máy chủ mở một cửa sổ Chrome trên TikTok, sau đó tự đăng nhập trong
        cửa sổ đó. Chrome được giữ mở và cookie mới tự động được cập nhật vào tài khoản mỗi khi thay đổi.
      </p>

      <form className="filter-bar" onSubmit={handleSearch}>
        <input
          type="search"
          placeholder="Tìm theo tên…"
          value={searchInput}
          onChange={event => setSearchInput(event.target.value)}
        />
        <button type="submit">
          <Search size={16} aria-hidden="true" /> Tìm
        </button>
      </form>

      <ErrorAlert message={error} />

      {loading ? (
        <LoadingBlock />
      ) : items.length === 0 ? (
        <EmptyState message="Chưa có tài khoản nào. Bấm “Thêm tài khoản” để tạo mới." />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tên</th>
                <th>Loại</th>
                <th>Phiên TikTok</th>
                <th className="col-actions">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => {
                const status = sessions[item.id];
                const info = getSessionInfo(status);
                const running = (status?.state ?? SessionState.Stopped) !== SessionState.Stopped;
                const busy = busyIds.has(item.id);
                const updatedAt = formatDateTime(status?.cookieUpdatedAt);

                return (
                  <tr key={item.id}>
                    <td>{item.title || <em>(không tên)</em>}</td>
                    <td>{accountTypes[item.accountTypeId] ?? (item.accountTypeId || '—')}</td>
                    <td>
                      <span className={`status-badge ${info.tone}`}>{info.label}</span>
                      <span className="cell-sub">
                        {updatedAt
                          ? `Cookie cập nhật: ${updatedAt}`
                          : item.cookie ? 'Đã có cookie' : 'Chưa có cookie'}
                      </span>
                    </td>
                    <td className="col-actions">
                      {running ? (
                        <button
                          type="button"
                          className="icon-btn"
                          title="Đóng Chrome"
                          disabled={busy}
                          aria-busy={busy}
                          onClick={() => runSessionAction(item, id => accountsClient.stopTikTokSession(id))}
                        >
                          {!busy && <Square size={16} aria-hidden="true" />}
                        </button>
                      ) : (
                        <button
                          type="button"
                          className="icon-btn"
                          title="Mở Chrome để đăng nhập TikTok"
                          disabled={busy}
                          aria-busy={busy}
                          onClick={() => runSessionAction(item, id => accountsClient.startTikTokSession(id))}
                        >
                          {!busy && <LogIn size={16} aria-hidden="true" />}
                        </button>
                      )}
                      <Link to={`/accounts/${item.id}/tiktok`} className="icon-btn" title="Tra cứu kênh TikTok">
                        <UserSearch size={16} aria-hidden="true" />
                      </Link>
                      <Link to={`/accounts/${item.id}`} className="icon-btn" title="Sửa">
                        <Pencil size={16} aria-hidden="true" />
                      </Link>
                      <button
                        type="button"
                        className="icon-btn danger"
                        title="Xoá"
                        onClick={() => setDeleteTarget(item)}
                      >
                        <Trash2 size={16} aria-hidden="true" />
                      </button>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <Pagination
        page={result?.pageNumber ?? page}
        totalPages={result?.totalPages ?? 0}
        totalCount={result?.totalCount ?? 0}
        hasPreviousPage={result?.hasPreviousPage ?? false}
        hasNextPage={result?.hasNextPage ?? false}
        onChange={setPage}
      />

      <ConfirmDialog
        open={Boolean(deleteTarget)}
        title="Xoá tài khoản"
        message={`Xoá “${deleteTarget?.title ?? ''}”? Thao tác này không thể hoàn tác.`}
        busy={deleting}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
      />
    </>
  );
}
