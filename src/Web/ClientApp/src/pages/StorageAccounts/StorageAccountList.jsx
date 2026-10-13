import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Eraser, Pencil, Plus, PlugZap, Trash2 } from 'lucide-react';
import { checkStorageAccount, deleteStorageAccount, getStorageAccounts, trimStorageAccount } from '../../api/storageAccounts';
import { PageHeader } from '../../components/common/PageHeader';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { dateTimeFormat } from '../Movies/movieFormat';
import { formatBytes } from './storageFormat';

/** Mức đầy của tài khoản: xanh khi còn rộng, vàng khi sắp đầy, đỏ khi hết chỗ. */
function usageLevel(account) {
  const ratio = account.quotaBytes > 0 ? account.usedBytes / account.quotaBytes : 1;
  if (ratio >= 1) return 'danger';
  return ratio >= 0.9 ? 'warn' : 'ok';
}

export function StorageAccountList() {
  const [accounts, setAccounts] = useState(null);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [checkingId, setCheckingId] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);
  // Kế hoạch dọn bucket đang chờ xác nhận: { account, plan, deleteOrphans }.
  const [trim, setTrim] = useState(null);
  const [trimBusy, setTrimBusy] = useState(false);

  const load = useCallback(() => {
    getStorageAccounts()
      .then(setAccounts)
      .catch(err => setError(err.message));
  }, []);

  useEffect(load, [load]);

  const check = account => {
    setCheckingId(account.id);
    setError('');
    setMessage('');
    checkStorageAccount(account.id)
      .then(result => {
        if (result.succeeded) {
          setMessage(`“${account.title}”: kết nối được — bucket đang có ${result.objects} file, ${formatBytes(result.usedBytes)}.`);
        } else {
          setError(`“${account.title}”: ${result.error}`);
        }
        load();
      })
      .catch(err => setError(err.message))
      .finally(() => setCheckingId(null));
  };

  const planTrim = (account, deleteOrphans = false) => {
    setTrimBusy(true);
    setError('');
    setMessage('');
    trimStorageAccount(account.id, false, deleteOrphans)
      .then(plan => setTrim({ account, plan, deleteOrphans }))
      .catch(err => { setError(`“${account.title}”: ${err.message}`); setTrim(null); })
      .finally(() => setTrimBusy(false));
  };

  const applyTrim = () => {
    const { account, deleteOrphans } = trim;
    setTrimBusy(true);
    trimStorageAccount(account.id, true, deleteOrphans)
      .then(result => {
        const orphanText = result.orphansDeleted && result.orphanObjects > 0 ? `, xoá ${result.orphanObjects} file mồ côi` : '';
        setMessage(`“${account.title}”: đã gỡ video của ${result.episodesReleased} tập${orphanText} — bucket còn ${formatBytes(result.usedBytesAfter)} / ${formatBytes(result.quotaBytes)}.`);
        if (result.errors.length > 0) setError(`Không xoá được ${result.errors.length} file: ${result.errors[0]}`);
        else if (!result.withinQuota) setError(`“${account.title}” vẫn vượt hạn mức: bucket còn file không do hệ thống quản lý.`);
        load();
      })
      .catch(err => setError(`“${account.title}”: ${err.message}`))
      .finally(() => { setTrimBusy(false); setTrim(null); });
  };

  const handleDelete = () => {
    setDeleting(true);
    setError('');
    deleteStorageAccount(deleteTarget.id)
      .then(load)
      .catch(err => setError(err.message))
      .finally(() => {
        setDeleting(false);
        setDeleteTarget(null);
      });
  };

  return (
    <>
      <PageHeader
        title="Tài khoản lưu trữ"
        description="Các tài khoản Cloudflare R2 dùng để lưu file video của tập phim."
        actions={
          <Link to="/storage-accounts/new" role="button">
            <Plus size={16} aria-hidden="true" /> Thêm tài khoản lưu trữ
          </Link>
        }
      />

      <p className="hint">
        Video được lưu vào tài khoản đang bật có <strong>thứ tự</strong> nhỏ nhất còn đủ chỗ; tài khoản đầy thì tự chuyển
        sang tài khoản kế tiếp. Dung lượng đã dùng do hệ thống cộng dồn khi tải lên — bấm <strong>Kiểm tra</strong> để
        kết nối thử và đối soát lại theo dung lượng thực có trong bucket. Dung lượng tính như Cloudflare (1 GB = 1 tỷ byte).
        Tài khoản vượt hạn mức thì bấm biểu tượng <strong>cục tẩy</strong> để xem và gỡ bớt video.
      </p>

      <ErrorAlert message={error} />
      {message && <p className="cell-sub">{message}</p>}

      {accounts === null ? (
        !error && <LoadingBlock />
      ) : accounts.length === 0 ? (
        <EmptyState message="Chưa có tài khoản lưu trữ nào. Bấm “Thêm tài khoản lưu trữ” để tạo mới." />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Thứ tự</th>
                <th>Tên</th>
                <th>Bucket</th>
                <th>Dung lượng</th>
                <th>Số tập</th>
                <th>Trạng thái</th>
                <th className="col-actions">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {accounts.map(account => {
                const level = usageLevel(account);
                const percent = Math.min(100, account.quotaBytes > 0 ? (account.usedBytes / account.quotaBytes) * 100 : 100);

                return (
                  <tr key={account.id}>
                    <td>{account.priority}</td>
                    <td>
                      {account.title}
                      <span className="cell-sub">Access Key: {account.accessKeyId}</span>
                    </td>
                    <td>
                      {account.bucket}
                      <span className="cell-sub">{account.publicBaseUrl}</span>
                    </td>
                    <td>
                      <div className="progress-bar" role="img"
                        aria-label={`Đã dùng ${formatBytes(account.usedBytes)} trên ${formatBytes(account.quotaBytes)}`}>
                        <span className={level} style={{ width: `${percent}%` }} />
                      </div>
                      <span className="cell-sub">
                        {formatBytes(account.usedBytes)} / {formatBytes(account.quotaBytes)} ({Math.round(percent)}%)
                      </span>
                    </td>
                    <td>{account.episodeCount}</td>
                    <td>
                      {!account.isEnabled
                        ? <span className="status-badge">Đang tắt</span>
                        : account.lastError
                          ? <span className="status-badge danger">Lỗi</span>
                          : level === 'danger'
                            ? <span className="status-badge warn">Đã đầy</span>
                            : <span className="status-badge ok">Đang dùng</span>}
                      {account.lastError && <span className="cell-sub">{account.lastError}</span>}
                      {account.lastCheckedAt && (
                        <span className="cell-sub">Kiểm tra lúc {dateTimeFormat.format(new Date(account.lastCheckedAt))}</span>
                      )}
                    </td>
                    <td className="col-actions">
                      <button type="button" className="icon-btn" title="Kiểm tra kết nối và đối soát dung lượng"
                        onClick={() => check(account)} disabled={checkingId !== null} aria-busy={checkingId === account.id}>
                        <PlugZap size={16} aria-hidden="true" />
                      </button>
                      <button type="button" className="icon-btn" title="Dọn bucket về dưới hạn mức"
                        onClick={() => planTrim(account)} disabled={trimBusy || checkingId !== null}>
                        <Eraser size={16} aria-hidden="true" />
                      </button>
                      <Link to={`/storage-accounts/${account.id}`} className="icon-btn" title="Sửa">
                        <Pencil size={16} aria-hidden="true" />
                      </Link>
                      <button type="button" className="icon-btn danger" title="Xoá" onClick={() => setDeleteTarget(account)}>
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

      {trim && (
        <dialog open>
          <article>
            <header><strong>Dọn bucket “{trim.account.title}”</strong></header>
            <p>
              Bucket đang có {trim.plan.objects} file, {formatBytes(trim.plan.usedBytesBefore)} trên hạn mức{' '}
              {formatBytes(trim.plan.quotaBytes)}.
            </p>
            {trim.plan.orphanObjects > 0 && (
              <label>
                <input type="checkbox" checked={trim.deleteOrphans} disabled={trimBusy}
                  onChange={event => planTrim(trim.account, event.target.checked)} />
                Xoá {trim.plan.orphanObjects} file mồ côi ({formatBytes(trim.plan.orphanBytes)}) — file trong thư mục movies/
                mà không tập nào trỏ tới. Đừng bật nếu bucket dùng chung với máy chủ khác.
              </label>
            )}
            {trim.plan.episodesReleased === 0 && !(trim.deleteOrphans && trim.plan.orphanObjects > 0) ? (
              <p>{trim.plan.withinQuota ? 'Bucket đang trong hạn mức, không cần xoá gì.' : 'Không còn video nào của hệ thống để gỡ.'}</p>
            ) : (
              <>
                {trim.plan.episodesReleased > 0 && (
                  <>
                    <p>
                      Sẽ gỡ video của <strong>{trim.plan.episodesReleased} tập</strong> ({formatBytes(trim.plan.episodeBytesReleased)}).
                      Các tập này mất link phát trên app cho tới khi được lưu lại:
                    </p>
                    <ul>
                      {trim.plan.movies.map(movie => (
                        <li key={movie.movieId}>
                          {movie.title || `Phim #${movie.movieId}`}: {movie.episodes} tập (tập {movie.fromEpisode}–{movie.toEpisode}),{' '}
                          {formatBytes(movie.bytes)}
                        </li>
                      ))}
                    </ul>
                  </>
                )}
                <p>Sau khi dọn còn khoảng {formatBytes(trim.plan.usedBytesAfter)}. File đã xoá trên R2 không khôi phục được.</p>
              </>
            )}
            <footer>
              <button type="button" className="secondary" onClick={() => setTrim(null)} disabled={trimBusy}>Đóng</button>
              {(trim.plan.episodesReleased > 0 || (trim.deleteOrphans && trim.plan.orphanObjects > 0)) && (
                <button type="button" className="danger" onClick={applyTrim} disabled={trimBusy} aria-busy={trimBusy}>
                  Xoá và dọn
                </button>
              )}
            </footer>
          </article>
        </dialog>
      )}

      <ConfirmDialog
        open={Boolean(deleteTarget)}
        title="Xoá tài khoản lưu trữ"
        message={`Xoá “${deleteTarget?.title ?? ''}” khỏi hệ thống? File trong bucket không bị xoá.`}
        busy={deleting}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
      />
    </>
  );
}
