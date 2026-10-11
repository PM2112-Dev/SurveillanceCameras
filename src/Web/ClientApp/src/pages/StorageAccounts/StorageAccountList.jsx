import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Pencil, Plus, PlugZap, Trash2 } from 'lucide-react';
import { checkStorageAccount, deleteStorageAccount, getStorageAccounts } from '../../api/storageAccounts';
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
        kết nối thử và đối soát lại theo dung lượng thực có trong bucket.
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
