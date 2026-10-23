import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Calculator, Eraser, Pencil, Plus, PlugZap, SlidersHorizontal, Trash2 } from 'lucide-react';
import {
  checkStorageAccount,
  deleteStorageAccount,
  getStorageAccounts,
  trimStorageAccount,
  updateStorageAccountMonthlyStat
} from '../../api/storageAccounts';
import { PageHeader } from '../../components/common/PageHeader';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { dateTimeFormat } from '../Movies/movieFormat';
import {
  CLASS_A_PRICE_PER_MILLION_USD,
  CLASS_B_PRICE_PER_MILLION_USD,
  estimateR2Cost,
  formatBytes,
  formatCount,
  formatMonth,
  formatUsd,
  FREE_CLASS_A_REQUESTS,
  FREE_CLASS_B_REQUESTS,
  FREE_STORAGE_BYTES,
  MB,
  STORAGE_PRICE_PER_GB_USD
} from './storageFormat';

/** Mức đầy: xanh khi còn rộng, vàng khi ≥ 90%, đỏ khi chạm trần. */
function ratioLevel(used, limit) {
  if (!limit || limit <= 0) return 'ok';
  const ratio = used / limit;
  if (ratio >= 1) return 'danger';
  return ratio >= 0.9 ? 'warn' : 'ok';
}

export function StorageAccountList() {
  const [accounts, setAccounts] = useState(null);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [checking, setChecking] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);
  // Kế hoạch dọn bucket đang chờ xác nhận: { account, plan, deleteOrphans }.
  const [trim, setTrim] = useState(null);
  const [trimBusy, setTrimBusy] = useState(false);
  // Hộp thoại đối soát số liệu tháng: { account, form, busy }.
  const [editStat, setEditStat] = useState(null);

  const load = useCallback(() => {
    getStorageAccounts()
      .then(setAccounts)
      .catch(err => setError(err.message));
  }, []);

  useEffect(load, [load]);

  const account = accounts?.[0] ?? null;

  const check = target => {
    setChecking(true);
    setError('');
    setMessage('');
    checkStorageAccount(target.id)
      .then(result => {
        if (result.succeeded) {
          setMessage(`“${target.title}”: kết nối thành công — bucket đang có ${formatCount(result.objects)} file, ${formatBytes(result.usedBytes)}.`);
        } else {
          setError(`“${target.title}”: ${result.error}`);
        }
        load();
      })
      .catch(err => setError(err.message))
      .finally(() => setChecking(false));
  };

  const planTrim = (target, deleteOrphans = false) => {
    setTrimBusy(true);
    setError('');
    setMessage('');
    trimStorageAccount(target.id, false, deleteOrphans)
      .then(plan => setTrim({ account: target, plan, deleteOrphans }))
      .catch(err => { setError(`“${target.title}”: ${err.message}`); setTrim(null); })
      .finally(() => setTrimBusy(false));
  };

  const applyTrim = () => {
    const { account: target, deleteOrphans } = trim;
    setTrimBusy(true);
    trimStorageAccount(target.id, true, deleteOrphans)
      .then(result => {
        const orphanText = result.orphansDeleted && result.orphanObjects > 0 ? `, xoá ${result.orphanObjects} file mồ côi` : '';
        setMessage(`“${target.title}”: đã gỡ video của ${result.episodesReleased} tập${orphanText} — bucket còn ${formatBytes(result.usedBytesAfter)} / ${formatBytes(result.quotaBytes)}.`);
        if (result.errors.length > 0) setError(`Không xoá được ${result.errors.length} file: ${result.errors[0]}`);
        else if (!result.withinQuota) setError(`“${target.title}” vẫn vượt hạn mức: bucket còn file không do hệ thống quản lý.`);
        load();
      })
      .catch(err => setError(`“${target.title}”: ${err.message}`))
      .finally(() => { setTrimBusy(false); setTrim(null); });
  };

  const openStatEditor = (target, stat) => {
    const now = new Date();
    const row = stat ?? target.currentMonth ?? {
      year: now.getUTCFullYear(),
      month: now.getUTCMonth() + 1,
      videosUploaded: 0,
      bytesUploaded: 0,
      videosDeleted: 0,
      bytesDeleted: 0,
      putObjectRequests: 0,
      listObjectsRequests: 0,
      otherClassARequests: 0,
      getObjectRequests: 0,
      HeadObjectRequests: 0,
      otherClassBRequests: 0,
      deleteObjectRequests: 0
    };
    setEditStat({
      account: target,
      busy: false,
      form: {
        year: String(row.year),
        month: String(row.month),
        videosUploaded: String(row.videosUploaded ?? 0),
        bytesUploadedMb: String(Math.round(((row.bytesUploaded ?? 0) / MB) * 100) / 100),
        videosDeleted: String(row.videosDeleted ?? 0),
        bytesDeletedMb: String(Math.round(((row.bytesDeleted ?? 0) / MB) * 100) / 100),
        putObjectRequests: String(row.putObjectRequests ?? 0),
        listObjectsRequests: String(row.listObjectsRequests ?? 0),
        otherClassARequests: String(row.otherClassARequests ?? 0),
        getObjectRequests: String(row.getObjectRequests ?? 0),
        headObjectRequests: String(row.headObjectRequests ?? 0),
        otherClassBRequests: String(row.otherClassBRequests ?? 0),
        deleteObjectRequests: String(row.deleteObjectRequests ?? 0)
      }
    });
  };

  const saveStatEditor = event => {
    event.preventDefault();
    if (!editStat) return;
    const { account: target, form } = editStat;
    setEditStat(current => ({ ...current, busy: true }));
    setError('');
    setMessage('');

    const year = Number(form.year);
    const month = Number(form.month);
    const payload = {
      videosUploaded: Math.max(0, Math.floor(Number(form.videosUploaded) || 0)),
      bytesUploaded: Math.max(0, Math.round((Number(form.bytesUploadedMb) || 0) * MB)),
      videosDeleted: Math.max(0, Math.floor(Number(form.videosDeleted) || 0)),
      bytesDeleted: Math.max(0, Math.round((Number(form.bytesDeletedMb) || 0) * MB)),
      putObjectRequests: Math.max(0, Math.floor(Number(form.putObjectRequests) || 0)),
      listObjectsRequests: Math.max(0, Math.floor(Number(form.listObjectsRequests) || 0)),
      otherClassARequests: Math.max(0, Math.floor(Number(form.otherClassARequests) || 0)),
      getObjectRequests: Math.max(0, Math.floor(Number(form.getObjectRequests) || 0)),
      headObjectRequests: Math.max(0, Math.floor(Number(form.headObjectRequests) || 0)),
      otherClassBRequests: Math.max(0, Math.floor(Number(form.otherClassBRequests) || 0)),
      deleteObjectRequests: Math.max(0, Math.floor(Number(form.deleteObjectRequests) || 0))
    };

    updateStorageAccountMonthlyStat(target.id, year, month, payload)
      .then(() => {
        setMessage(`Đã cập nhật số liệu đối soát tháng ${formatMonth(year, month)}.`);
        setEditStat(null);
        load();
      })
      .catch(err => {
        setError(err.message);
        setEditStat(current => (current ? { ...current, busy: false } : null));
      });
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

  const currentMonth = account?.currentMonth;
  const storageLevel = account ? ratioLevel(account.usedBytes, account.quotaBytes) : 'ok';
  const storagePercent = account && account.quotaBytes > 0
    ? Math.min(100, (account.usedBytes / account.quotaBytes) * 100)
    : 0;
  const monthlyVideoReached = Boolean(
    account && account.monthlyVideoLimit > 0 && (currentMonth?.videosUploaded ?? 0) >= account.monthlyVideoLimit
  );
  const monthlyVideoPercent = account && account.monthlyVideoLimit > 0
    ? Math.min(100, ((currentMonth?.videosUploaded ?? 0) / account.monthlyVideoLimit) * 100)
    : 0;
  const classAPercent = currentMonth
    ? Math.min(100, (currentMonth.totalClassARequests / FREE_CLASS_A_REQUESTS) * 100)
    : 0;
  const classBPercent = currentMonth
    ? Math.min(100, (currentMonth.totalClassBRequests / FREE_CLASS_B_REQUESTS) * 100)
    : 0;

  const previewCost = editStat && estimateR2Cost(
    editStat.account.usedBytes,
    (Number(editStat.form.putObjectRequests) || 0) +
      (Number(editStat.form.listObjectsRequests) || 0) +
      (Number(editStat.form.otherClassARequests) || 0),
    (Number(editStat.form.getObjectRequests) || 0) +
      (Number(editStat.form.headObjectRequests) || 0) +
      (Number(editStat.form.otherClassBRequests) || 0)
  );

  return (
    <>
      <PageHeader
        title="Tài khoản lưu trữ R2"
        description="Quản lý tài khoản Cloudflare R2 duy nhất, số lượng video tải lên mỗi tháng và theo dõi số request theo loại để tính tiền."
        actions={
          account ? (
            <>
              <button type="button" className="secondary" onClick={() => check(account)} disabled={checking || trimBusy}
                aria-busy={checking} title="Kết nối thử tới bucket và đối soát dung lượng">
                <PlugZap size={16} aria-hidden="true" /> Kiểm tra &amp; đối soát
              </button>
              <button type="button" className="secondary" onClick={() => planTrim(account)} disabled={trimBusy || checking}
                title="Dọn bucket về dưới hạn mức">
                <Eraser size={16} aria-hidden="true" /> Dọn bucket
              </button>
              <button type="button" className="secondary" onClick={() => openStatEditor(account, currentMonth)}>
                <SlidersHorizontal size={16} aria-hidden="true" /> Đối soát request tháng
              </button>
              <Link to={`/storage-accounts/${account.id}`} role="button">
                <Pencil size={16} aria-hidden="true" /> Cấu hình R2
              </Link>
              <button type="button" className="secondary danger" onClick={() => setDeleteTarget(account)}>
                <Trash2 size={16} aria-hidden="true" /> Xoá
              </button>
            </>
          ) : (
            <Link to="/storage-accounts/new" role="button">
              <Plus size={16} aria-hidden="true" /> Thiết lập tài khoản R2
            </Link>
          )
        }
      />

      <p className="hint">
        Hệ thống sử dụng <strong>1 tài khoản Cloudflare R2 duy nhất</strong> để lưu trữ video các tập phim. Mức miễn phí
        hằng tháng của R2: <strong>{formatBytes(FREE_STORAGE_BYTES)}</strong> lưu trữ (vượt mức ${STORAGE_PRICE_PER_GB_USD}/GB-tháng),{' '}
        <strong>{formatCount(FREE_CLASS_A_REQUESTS)}</strong> request Class A (PutObject, ListObjects — vượt mức ${CLASS_A_PRICE_PER_MILLION_USD}/triệu),{' '}
        <strong>{formatCount(FREE_CLASS_B_REQUESTS)}</strong> request Class B (GetObject, HeadObject — vượt mức ${CLASS_B_PRICE_PER_MILLION_USD}/triệu),
        xoá file (DeleteObject) và băng thông tải ra (Egress) <strong>miễn phí ($0)</strong>.
      </p>

      <ErrorAlert message={error} />
      {message && <p className="cell-sub">{message}</p>}

      {accounts === null ? (
        !error && <LoadingBlock />
      ) : !account ? (
        <EmptyState message="Chưa thiết lập tài khoản lưu trữ Cloudflare R2. Bấm “Thiết lập tài khoản R2” để bắt đầu." />
      ) : (
        <>
          <section className="profile-card">
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '0.5rem' }}>
              <h3 style={{ margin: 0 }}>{account.title}</h3>
              <div>
                {!account.isEnabled ? (
                  <span className="status-badge">Đang tắt</span>
                ) : account.lastError ? (
                  <span className="status-badge danger">Lỗi kết nối</span>
                ) : storageLevel === 'danger' ? (
                  <span className="status-badge warn">Đầy dung lượng</span>
                ) : monthlyVideoReached ? (
                  <span className="status-badge warn">Đạt trần video tháng</span>
                ) : (
                  <span className="status-badge ok">Đang hoạt động</span>
                )}
              </div>
            </div>
            <dl className="crawl-story-meta" style={{ marginTop: '0.75rem' }}>
              <dt>Cloudflare Account ID</dt><dd><code>{account.cloudflareAccountId}</code></dd>
              <dt>Bucket</dt><dd><code>{account.bucket}</code></dd>
              <dt>Địa chỉ công khai (CDN)</dt>
              <dd><a href={account.publicBaseUrl} target="_blank" rel="noreferrer">{account.publicBaseUrl}</a></dd>
              <dt>Access Key ID</dt><dd><code>{account.accessKeyId}</code></dd>
              <dt>Tổng số tập đang lưu</dt><dd>{formatCount(account.episodeCount)} tập</dd>
              <dt>Giới hạn upload/tháng</dt>
              <dd>{account.monthlyVideoLimit > 0 ? `${formatCount(account.monthlyVideoLimit)} video / tháng` : 'Không giới hạn'}</dd>
              <dt>Kiểm tra gần nhất</dt>
              <dd>{account.lastCheckedAt ? dateTimeFormat.format(new Date(account.lastCheckedAt)) : 'Chưa kiểm tra'}</dd>
            </dl>
            {account.lastError && <p className="error">Lỗi gần nhất: {account.lastError}</p>}
          </section>

          {currentMonth && (
            <div className="stat-grid" style={{ marginTop: 'var(--pico-spacing)' }}>
              <div className="stat-card stat-card--static">
                <span className="stat-value">{formatBytes(account.usedBytes)}</span>
                <span className="stat-label">Dung lượng lưu trữ / {formatBytes(account.quotaBytes)} ({Math.round(storagePercent)}%)</span>
                <div className="progress-bar" style={{ marginTop: '0.35rem' }}>
                  <span className={storageLevel} style={{ width: `${storagePercent}%` }} />
                </div>
                <span className="cell-sub">
                  Miễn phí 10 GB · Ước tính phí lưu trữ: <strong>{formatUsd(currentMonth.storageCostUsd)}</strong>/tháng
                </span>
              </div>

              <div className="stat-card stat-card--static">
                <span className="stat-value">
                  {formatCount(currentMonth.videosUploaded)}
                  {account.monthlyVideoLimit > 0 ? ` / ${formatCount(account.monthlyVideoLimit)}` : ''} video
                </span>
                <span className="stat-label">Video upload tháng {formatMonth(currentMonth.year, currentMonth.month)}</span>
                {account.monthlyVideoLimit > 0 && (
                  <div className="progress-bar" style={{ marginTop: '0.35rem' }}>
                    <span className={ratioLevel(currentMonth.videosUploaded, account.monthlyVideoLimit)}
                      style={{ width: `${monthlyVideoPercent}%` }} />
                  </div>
                )}
                <span className="cell-sub">
                  Đã tải lên {formatBytes(currentMonth.bytesUploaded)} · Đã xoá {formatCount(currentMonth.videosDeleted)} video ({formatBytes(currentMonth.bytesDeleted)})
                </span>
              </div>

              <div className="stat-card stat-card--static">
                <span className="stat-value">{formatCount(currentMonth.totalClassARequests)}</span>
                <span className="stat-label">Request Class A (Ghi / Liệt kê)</span>
                <div className="progress-bar" style={{ marginTop: '0.35rem' }}>
                  <span className={ratioLevel(currentMonth.totalClassARequests, FREE_CLASS_A_REQUESTS)}
                    style={{ width: `${classAPercent}%` }} />
                </div>
                <span className="cell-sub">
                  PutObject: {formatCount(currentMonth.putObjectRequests)} · ListObjects: {formatCount(currentMonth.listObjectsRequests)}
                  {currentMonth.otherClassARequests > 0 ? ` · Khác: ${formatCount(currentMonth.otherClassARequests)}` : ''}
                </span>
                <span className="cell-sub">
                  Miễn phí 1M request · Phí Class A: <strong>{formatUsd(currentMonth.classACostUsd)}</strong>
                </span>
              </div>

              <div className="stat-card stat-card--static">
                <span className="stat-value">{formatCount(currentMonth.totalClassBRequests)}</span>
                <span className="stat-label">Request Class B (Đọc / Phát video)</span>
                <div className="progress-bar" style={{ marginTop: '0.35rem' }}>
                  <span className={ratioLevel(currentMonth.totalClassBRequests, FREE_CLASS_B_REQUESTS)}
                    style={{ width: `${classBPercent}%` }} />
                </div>
                <span className="cell-sub">
                  GetObject: {formatCount(currentMonth.getObjectRequests)} · HeadObject: {formatCount(currentMonth.headObjectRequests)}
                  {currentMonth.otherClassBRequests > 0 ? ` · Khác: ${formatCount(currentMonth.otherClassBRequests)}` : ''}
                </span>
                <span className="cell-sub">
                  Miễn phí 10M request · Phí Class B: <strong>{formatUsd(currentMonth.classBCostUsd)}</strong>
                </span>
              </div>

              <div className="stat-card stat-card--static">
                <span className="stat-value">{formatCount(currentMonth.deleteObjectRequests)}</span>
                <span className="stat-label">Request DeleteObject (Xoá file)</span>
                <span className="cell-sub">Tổng mọi request tháng này: {formatCount(currentMonth.totalRequests)}</span>
                <span className="cell-sub">Phí xoá file &amp; băng thông: <strong>$0.00 (Miễn phí)</strong></span>
              </div>

              <div className="stat-card stat-card--static">
                <span className="stat-value">{formatUsd(currentMonth.totalEstimatedCostUsd)}</span>
                <span className="stat-label">
                  <Calculator size={15} aria-hidden="true" /> Tổng chi phí R2 ước tính tháng {formatMonth(currentMonth.year, currentMonth.month)}
                </span>
                <span className="cell-sub">
                  Lưu trữ {formatUsd(currentMonth.storageCostUsd)} + Class A {formatUsd(currentMonth.classACostUsd)} + Class B {formatUsd(currentMonth.classBCostUsd)}
                </span>
              </div>
            </div>
          )}

          <section style={{ marginTop: 'var(--pico-spacing)' }}>
            <h3>Thống kê số video upload &amp; request theo từng tháng (UTC)</h3>
            <div className="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Tháng</th>
                    <th>Video tải lên</th>
                    <th>Video đã xoá</th>
                    <th>Request Class A ($4.50/1M)</th>
                    <th>Request Class B ($0.36/1M)</th>
                    <th>Request Delete ($0)</th>
                    <th>Chi phí ước tính</th>
                    <th className="col-actions">Đối soát</th>
                  </tr>
                </thead>
                <tbody>
                  {(account.monthlyStats ?? []).map(stat => (
                    <tr key={`${stat.year}-${stat.month}`}>
                      <td className="nowrap"><strong>{formatMonth(stat.year, stat.month)}</strong></td>
                      <td>
                        <strong>{formatCount(stat.videosUploaded)}</strong>
                        {account.monthlyVideoLimit > 0 ? ` / ${formatCount(account.monthlyVideoLimit)}` : ''} video
                        <span className="cell-sub">{formatBytes(stat.bytesUploaded)}</span>
                      </td>
                      <td>
                        {formatCount(stat.videosDeleted)} video
                        <span className="cell-sub">{formatBytes(stat.bytesDeleted)}</span>
                      </td>
                      <td>
                        <strong>{formatCount(stat.totalClassARequests)}</strong> / {formatCount(FREE_CLASS_A_REQUESTS)}
                        <span className="cell-sub">
                          Put: {formatCount(stat.putObjectRequests)} · List: {formatCount(stat.listObjectsRequests)}
                          {stat.otherClassARequests > 0 ? ` · Khác: ${formatCount(stat.otherClassARequests)}` : ''}
                        </span>
                      </td>
                      <td>
                        <strong>{formatCount(stat.totalClassBRequests)}</strong> / {formatCount(FREE_CLASS_B_REQUESTS)}
                        <span className="cell-sub">
                          Get: {formatCount(stat.getObjectRequests)} · Head: {formatCount(stat.headObjectRequests)}
                          {stat.otherClassBRequests > 0 ? ` · Khác: ${formatCount(stat.otherClassBRequests)}` : ''}
                        </span>
                      </td>
                      <td>
                        {formatCount(stat.deleteObjectRequests)}
                        <span className="cell-sub">DeleteObject</span>
                      </td>
                      <td>
                        <strong>{formatUsd(stat.totalEstimatedCostUsd)}</strong>
                        <span className="cell-sub">
                          Lưu trữ: {formatUsd(stat.storageCostUsd)} · A: {formatUsd(stat.classACostUsd)} · B: {formatUsd(stat.classBCostUsd)}
                        </span>
                      </td>
                      <td className="col-actions">
                        <button type="button" className="icon-btn" title="Đối soát / chỉnh số liệu tháng này"
                          onClick={() => openStatEditor(account, stat)}>
                          <SlidersHorizontal size={16} aria-hidden="true" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        </>
      )}

      {editStat && (
        <dialog open>
          <article style={{ maxWidth: '680px', width: '100%' }}>
            <header>
              <strong>Đối soát số liệu R2 theo tháng — “{editStat.account.title}”</strong>
            </header>
            <form onSubmit={saveStatEditor}>
              <p className="cell-sub">
                Hệ thống tự cộng dồn mỗi khi tải video lên (PutObject), kiểm tra/dọn bucket (ListObjects), xoá video
                (DeleteObject) và khi người dùng phát tập phim trên app (GetObject). Bạn có thể đối chiếu với trang
                Cloudflare R2 → Metrics và cập nhật lại tại đây để khớp số tính tiền thực tế.
              </p>
              <div className="grid">
                <label>
                  Năm (UTC)
                  <input type="number" required min="2020" max="2100" value={editStat.form.year}
                    onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, year: e.target.value } }))} />
                </label>
                <label>
                  Tháng (1–12)
                  <input type="number" required min="1" max="12" value={editStat.form.month}
                    onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, month: e.target.value } }))} />
                </label>
              </div>

              <fieldset>
                <legend><strong>Quản lý video trong tháng</strong></legend>
                <div className="grid">
                  <label>
                    Số video đã tải lên
                    <input type="number" min="0" step="1" value={editStat.form.videosUploaded}
                      onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, videosUploaded: e.target.value } }))} />
                  </label>
                  <label>
                    Dung lượng tải lên (MB)
                    <input type="number" min="0" step="0.01" value={editStat.form.bytesUploadedMb}
                      onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, bytesUploadedMb: e.target.value } }))} />
                  </label>
                  <label>
                    Số video đã xoá
                    <input type="number" min="0" step="1" value={editStat.form.videosDeleted}
                      onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, videosDeleted: e.target.value } }))} />
                  </label>
                  <label>
                    Dung lượng đã xoá (MB)
                    <input type="number" min="0" step="0.01" value={editStat.form.bytesDeletedMb}
                      onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, bytesDeletedMb: e.target.value } }))} />
                  </label>
                </div>
              </fieldset>

              <fieldset>
                <legend><strong>Request Class A (Miễn phí 1.000.000/tháng · vượt mức $4.50/triệu)</strong></legend>
                <div className="grid">
                  <label>
                    PutObject (Tải file lên)
                    <input type="number" min="0" step="1" value={editStat.form.putObjectRequests}
                      onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, putObjectRequests: e.target.value } }))} />
                  </label>
                  <label>
                    ListObjects (Đọc danh sách bucket)
                    <input type="number" min="0" step="1" value={editStat.form.listObjectsRequests}
                      onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, listObjectsRequests: e.target.value } }))} />
                  </label>
                  <label>
                    Class A khác (Multipart/Copy...)
                    <input type="number" min="0" step="1" value={editStat.form.otherClassARequests}
                      onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, otherClassARequests: e.target.value } }))} />
                  </label>
                </div>
              </fieldset>

              <fieldset>
                <legend><strong>Request Class B (Miễn phí 10.000.000/tháng · vượt mức $0.36/triệu) &amp; Delete ($0)</strong></legend>
                <div className="grid">
                  <label>
                    GetObject (Đọc / phát video)
                    <input type="number" min="0" step="1" value={editStat.form.getObjectRequests}
                      onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, getObjectRequests: e.target.value } }))} />
                  </label>
                  <label>
                    HeadObject (Kiểm tra file)
                    <input type="number" min="0" step="1" value={editStat.form.headObjectRequests}
                      onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, headObjectRequests: e.target.value } }))} />
                  </label>
                  <label>
                    Class B khác (Đối soát CDN/R2)
                    <input type="number" min="0" step="1" value={editStat.form.otherClassBRequests}
                      onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, otherClassBRequests: e.target.value } }))} />
                  </label>
                  <label>
                    DeleteObject (Xoá file — miễn phí)
                    <input type="number" min="0" step="1" value={editStat.form.deleteObjectRequests}
                      onChange={e => setEditStat(s => ({ ...s, form: { ...s.form, deleteObjectRequests: e.target.value } }))} />
                  </label>
                </div>
              </fieldset>

              {previewCost && (
                <p className="cell-sub">
                  Chi phí ước tính theo số liệu trên: <strong>{formatUsd(previewCost.totalEstimatedCostUsd)}</strong>{' '}
                  (Lưu trữ: {formatUsd(previewCost.storageCostUsd)} · Class A: {formatUsd(previewCost.classACostUsd)} · Class B: {formatUsd(previewCost.classBCostUsd)})
                </p>
              )}

              <footer>
                <button type="button" className="secondary" onClick={() => setEditStat(null)} disabled={editStat.busy}>Huỷ</button>
                <button type="submit" disabled={editStat.busy} aria-busy={editStat.busy}>Lưu đối soát</button>
              </footer>
            </form>
          </article>
        </dialog>
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
