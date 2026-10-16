import { useState } from 'react';
import { SetAppViewerVipRequest } from '../../web-api-client';
import { appViewersClient, getErrorMessage, monetizationClient } from '../../api/clients';
import { useAsyncData } from '../../hooks/useAsyncData';
import { PageHeader } from '../../components/common/PageHeader';
import { Pagination } from '../../components/common/Pagination';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';

const sources = { 1: 'Cấp tay', 2: 'App Store', 3: 'Google Play' };
const sourceLabel = { Admin: 'Cấp tay', AppStore: 'App Store', GooglePlay: 'Google Play' };
const statusLabel = {
  Active: 'Đang hiệu lực', Canceled: 'Đã tắt gia hạn', GracePeriod: 'Ân hạn thanh toán', OnHold: 'Tạm khoá (lỗi thanh toán)',
  Expired: 'Hết hạn', Revoked: 'Hoàn tiền / thu hồi', Paused: 'Tạm dừng', Pending: 'Chờ thanh toán'
};
const formatTime = value => (value ? new Date(value).toLocaleString('vi-VN') : '—');
const toLocalInput = date => new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);

function GrantForm({ onSaved }) {
  const [email, setEmail] = useState('');
  const [plan, setPlan] = useState('gift');
  const [expires, setExpires] = useState(toLocalInput(new Date(Date.now() + 30 * 86400000)));
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const send = revoke => event => {
    event?.preventDefault();
    setBusy(true);
    setError('');
    setMessage('');
    appViewersClient.setAppViewerVip(new SetAppViewerVipRequest({
      email, plan: plan || undefined, expiresAt: revoke ? new Date() : new Date(expires)
    }))
      .then(() => { setMessage(revoke ? 'Đã thu hồi gói cấp tay.' : 'Đã cấp VIP.'); onSaved(); })
      .catch(err => setError(err?.status === 404 ? 'Không có người xem với email này.' : getErrorMessage(err)))
      .finally(() => setBusy(false));
  };

  return (
    <section className="profile-card" style={{ marginBottom: 'var(--pico-spacing)' }}>
      <h2>Cấp / thu hồi VIP tay</h2>
      <p className="cell-sub">Dùng để tặng hoặc hỗ trợ khách. Không ảnh hưởng gói người xem đã mua qua cửa hàng.</p>
      <form onSubmit={send(false)}>
        <div className="grid">
          <label>Email người xem<input type="email" required value={email} onChange={e => setEmail(e.target.value)} /></label>
          <label>Mã gói<input value={plan} onChange={e => setPlan(e.target.value)} /></label>
          <label>Hết hạn<input type="datetime-local" required value={expires} onChange={e => setExpires(e.target.value)} /></label>
        </div>
        <ErrorAlert message={error} />
        <div className="grid">
          <button type="submit" disabled={busy} aria-busy={busy}>Cấp / gia hạn</button>
          <button type="button" className="secondary" disabled={busy || !email} onClick={send(true)}>Thu hồi gói cấp tay</button>
        </div>
        {message && <span className="cell-sub">{message}</span>}
      </form>
    </section>
  );
}

export function AppSubscriptions() {
  const [filters, setFilters] = useState({ email: '', source: '', active: '' });
  const [applied, setApplied] = useState(filters);
  const [page, setPage] = useState(1);
  const [version, setVersion] = useState(0);
  const { data, loading, error } = useAsyncData(() => monetizationClient.getAppSubscriptions(
    applied.email || undefined, applied.source ? Number(applied.source) : undefined,
    applied.active === '' ? undefined : applied.active === 'true', page, 20), [applied, page, version]);

  const search = event => { event.preventDefault(); setPage(1); setApplied(filters); };

  return (
    <>
      <PageHeader title="Người đăng ký VIP" description="Gói VIP cấp tay và mua qua App Store / Google Play. Trạng thái gói mua được cửa hàng tự cập nhật (gia hạn, huỷ, hoàn tiền)." />
      <GrantForm onSaved={() => setVersion(v => v + 1)} />
      <form className="filter-bar" onSubmit={search}>
        <input type="search" placeholder="Tìm theo email…" value={filters.email} onChange={e => setFilters({ ...filters, email: e.target.value })} />
        <select value={filters.source} onChange={e => setFilters({ ...filters, source: e.target.value })}>
          <option value="">Mọi nguồn</option>
          {Object.entries(sources).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        <select value={filters.active} onChange={e => setFilters({ ...filters, active: e.target.value })}>
          <option value="">Mọi trạng thái</option>
          <option value="true">Còn hạn</option>
          <option value="false">Hết hạn</option>
        </select>
        <button type="submit">Lọc</button>
      </form>
      {loading ? <LoadingBlock /> : <ErrorAlert message={error} />}
      {data && data.items.length === 0 && <EmptyState message="Không có gói nào." />}
      {data && data.items.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Người xem</th><th>Gói</th><th>Nguồn</th><th>Trạng thái</th><th>Hết hạn</th><th>Đối chiếu gần nhất</th></tr></thead>
            <tbody>
              {data.items.map(s => (
                <tr key={s.id}>
                  <td>{s.email ?? <em>(đã xoá)</em>}<span className="cell-sub">{s.userId}</span></td>
                  <td>{s.plan ?? '—'}{s.productId && <span className="cell-sub"><code>{s.productId}</code></span>}</td>
                  <td>{sourceLabel[s.source] ?? s.source}{s.isSandbox && <span className="cell-sub">Sandbox / thử</span>}</td>
                  <td>
                    <span className={`status-badge ${s.isActive ? 'ok' : s.status === 'Revoked' ? 'danger' : ''}`}>{s.isActive ? 'Còn hạn' : 'Hết hạn'}</span>
                    <span className="cell-sub">{statusLabel[s.status] ?? s.status}{s.source !== 'Admin' && (s.autoRenew ? ' · tự gia hạn' : ' · không tự gia hạn')}</span>
                  </td>
                  <td>{formatTime(s.expiresAt)}</td>
                  <td>{formatTime(s.verifiedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <Pagination page={data?.pageNumber ?? page} totalPages={data?.totalPages ?? 0} totalCount={data?.totalCount ?? 0}
        hasPreviousPage={data?.hasPreviousPage ?? false} hasNextPage={data?.hasNextPage ?? false} onChange={setPage} />
    </>
  );
}
