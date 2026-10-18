import { useEffect, useState } from 'react';
import { UpdateMonetizationSettingsCommand } from '../../web-api-client';
import { getErrorMessage, monetizationClient } from '../../api/clients';
import { useAsyncData } from '../../hooks/useAsyncData';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';

const formatDay = day => new Date(`${day}T00:00:00Z`).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', timeZone: 'UTC' });

function SettingsForm({ settings, onSaved }) {
  const [form, setForm] = useState(settings);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [saved, setSaved] = useState(false);
  useEffect(() => setForm(settings), [settings]);

  const update = patch => { setForm(current => ({ ...current, ...patch })); setSaved(false); };

  const submit = event => {
    event.preventDefault();
    setSaving(true);
    setError('');
    monetizationClient.updateMonetizationSettings(new UpdateMonetizationSettingsCommand({
      ...form,
      paywallAfterRewardedAds: Number(form.paywallAfterRewardedAds) || 0,
      minAppVersion: form.minAppVersion || undefined
    }))
      .then(() => { setSaved(true); onSaved(); })
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setSaving(false));
  };

  return (
    <section className="profile-card">
      <h2>Cấu hình điều khiển từ xa</h2>
      <p className="cell-sub">App tải lại cấu hình mỗi lần mở (và khi quay lại sau 30 phút), không cần phát hành bản mới.</p>
      <form onSubmit={submit}>
        <fieldset>
          <label>
            <input type="checkbox" role="switch" checked={form.adsEnabled} onChange={e => update({ adsEnabled: e.target.checked })} />
            Bật quảng cáo (tắt thì app không tải SDK và không hiện quảng cáo nào)
          </label>
          <label>
            <input type="checkbox" role="switch" checked={form.purchasesEnabled} onChange={e => update({ purchasesEnabled: e.target.checked })} />
            Bật mua gói VIP trong app
          </label>
          <label>
            <input type="checkbox" role="switch" checked={form.requireAdVerification}
              onChange={e => update({ requireAdVerification: e.target.checked })} />
            Bắt buộc AdMob xác nhận lượt xem (SSV) trước khi mở tập
          </label>
          <small>
            Chỉ bật sau khi đã khai báo URL callback <code>/api/app/store/admob/ssv</code> cho ad unit thật trên AdMob; nếu không,
            người xem sẽ không mở được tập bằng quảng cáo.
          </small>
          <label>
            <input type="checkbox" role="switch" checked={form.adStatsEnabled ?? true} onChange={e => update({ adStatsEnabled: e.target.checked })} />
            Thu thập thống kê quảng cáo (request, hiển thị, bấm, doanh thu)
          </label>
        </fieldset>
        <div className="grid">
          <label>
            Mẫu màn mua gói
            <select value={form.paywallVariant} onChange={e => update({ paywallVariant: e.target.value })}>
              {['A', 'B', 'C', 'D'].map(x => <option key={x} value={x}>Mẫu {x}</option>)}
            </select>
          </label>
          <label>
            Mời mua gói sau mỗi … lần xem quảng cáo
            <input type="number" min="0" max="100" value={form.paywallAfterRewardedAds}
              onChange={e => update({ paywallAfterRewardedAds: e.target.value })} />
            <small>0 là không mời.</small>
          </label>
          <label>
            Phiên bản app tối thiểu
            <input placeholder="vd. 1.2.0" value={form.minAppVersion ?? ''} onChange={e => update({ minAppVersion: e.target.value })} />
            <small>Bản cũ hơn được nhắc cập nhật.</small>
          </label>
        </div>
        <ErrorAlert message={error} />
        <button type="submit" disabled={saving} aria-busy={saving}>Lưu cấu hình</button>
        {saved && <span className="cell-sub"> Đã lưu.</span>}
      </form>
    </section>
  );
}

export function MonetizationOverview() {
  const [version, setVersion] = useState(0);
  const settings = useAsyncData(() => monetizationClient.getMonetizationSettings(), [version]);
  const stats = useAsyncData(() => monetizationClient.getMonetizationStats(30), []);
  const days = [...(stats.data?.days ?? [])].reverse();
  const totals = days.reduce((sum, d) => ({
    rewarded: sum.rewarded + d.rewardedVerified, unlocked: sum.unlocked + d.episodesUnlocked, subs: sum.subs + d.newSubscriptions
  }), { rewarded: 0, unlocked: 0, subs: 0 });

  return (
    <>
      <PageHeader title="Kiếm tiền" description="Quảng cáo AdMob và gói VIP của app xem phim: số liệu 30 ngày và cấu hình điều khiển từ xa." />

      {stats.loading ? <LoadingBlock /> : <ErrorAlert message={stats.error} />}
      {stats.data && (
        <>
          <div className="stat-grid">
            <div className="stat-card stat-card--static"><span className="stat-value">{stats.data.activeVip}</span><span className="stat-label">Người đang VIP</span></div>
            <div className="stat-card stat-card--static"><span className="stat-value">{stats.data.activeStoreSubscriptions}</span><span className="stat-label">Gói mua qua cửa hàng còn hạn</span></div>
            <div className="stat-card stat-card--static"><span className="stat-value">{stats.data.activeAdminGrants}</span><span className="stat-label">Gói cấp tay còn hạn</span></div>
            <div className="stat-card stat-card--static"><span className="stat-value">{totals.unlocked}</span><span className="stat-label">Tập mở bằng quảng cáo (30 ngày)</span></div>
          </div>
          <div className="table-wrap" style={{ marginTop: 'var(--pico-spacing)' }}>
            <table>
              <thead><tr><th>Ngày (UTC)</th><th>Tập mở bằng quảng cáo</th><th>Lượt AdMob xác nhận</th><th>Gói mới</th></tr></thead>
              <tbody>
                {days.filter(d => d.rewardedVerified || d.episodesUnlocked || d.newSubscriptions).map(d => (
                  <tr key={d.day}><td>{formatDay(d.day)}</td><td>{d.episodesUnlocked}</td><td>{d.rewardedVerified}</td><td>{d.newSubscriptions}</td></tr>
                ))}
                <tr><th>Tổng</th><th>{totals.unlocked}</th><th>{totals.rewarded}</th><th>{totals.subs}</th></tr>
              </tbody>
            </table>
          </div>
        </>
      )}

      {settings.loading && !settings.data ? <LoadingBlock /> : <ErrorAlert message={settings.error} />}
      {settings.data && <SettingsForm settings={settings.data} onSaved={() => setVersion(v => v + 1)} />}
    </>
  );
}
