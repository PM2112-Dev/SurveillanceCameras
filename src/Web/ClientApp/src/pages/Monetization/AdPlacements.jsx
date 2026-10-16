import { useState } from 'react';
import { UpdateAdPlacementCommand } from '../../web-api-client';
import { getErrorMessage, monetizationClient } from '../../api/clients';
import { useAsyncData } from '../../hooks/useAsyncData';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';

const formatLabel = { rewarded: 'Có thưởng', interstitial: 'Toàn màn hình', banner: 'Banner' };
const isTestUnit = id => id?.startsWith('ca-app-pub-3940256099942544/');

function PlacementForm({ placement }) {
  const [form, setForm] = useState({ ...placement, androidUnitId: placement.androidUnitId ?? '', iosUnitId: placement.iosUnitId ?? '' });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [saved, setSaved] = useState(false);
  const update = patch => { setForm(current => ({ ...current, ...patch })); setSaved(false); };

  const submit = event => {
    event.preventDefault();
    setSaving(true);
    setError('');
    monetizationClient.updateAdPlacement(form.id, new UpdateAdPlacementCommand({
      id: form.id, title: form.title, androidUnitId: form.androidUnitId || undefined, iosUnitId: form.iosUnitId || undefined,
      isEnabled: form.isEnabled, dailyLimit: Number(form.dailyLimit) || 0, cooldownSeconds: Number(form.cooldownSeconds) || 0,
      everyEpisodes: Number(form.everyEpisodes) || 0
    }))
      .then(() => setSaved(true))
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setSaving(false));
  };

  return (
    <section className="profile-card" style={{ marginBottom: 'var(--pico-spacing)' }}>
      <h2>{form.title} <small><code>{form.code}</code> · {formatLabel[form.format] ?? form.format}</small></h2>
      <form onSubmit={submit}>
        <label>
          <input type="checkbox" role="switch" checked={form.isEnabled} onChange={e => update({ isEnabled: e.target.checked })} /> Bật vị trí này
        </label>
        <div className="grid">
          <label>Ad unit Android<input value={form.androidUnitId} placeholder="ca-app-pub-…/…" onChange={e => update({ androidUnitId: e.target.value })} /></label>
          <label>Ad unit iOS<input value={form.iosUnitId} placeholder="ca-app-pub-…/…" onChange={e => update({ iosUnitId: e.target.value })} /></label>
        </div>
        {(isTestUnit(form.androidUnitId) || isTestUnit(form.iosUnitId)) && (
          <small className="cell-sub">Đang dùng ad unit thử nghiệm của Google (luôn hiện quảng cáo mẫu, không có doanh thu). Thay bằng ad unit thật trước khi phát hành.</small>
        )}
        <div className="grid">
          {form.format === 'rewarded' && (
            <label>Tối đa mỗi ngày<input type="number" min="0" value={form.dailyLimit} onChange={e => update({ dailyLimit: e.target.value })} /><small>Số tập mỗi người mở bằng quảng cáo trong một ngày; 0 là không giới hạn.</small></label>
          )}
          {form.format === 'interstitial' && (
            <>
              <label>Sau mỗi … tập<input type="number" min="0" value={form.everyEpisodes} onChange={e => update({ everyEpisodes: e.target.value })} /><small>0 là tắt.</small></label>
              <label>Nghỉ tối thiểu (giây)<input type="number" min="0" value={form.cooldownSeconds} onChange={e => update({ cooldownSeconds: e.target.value })} /></label>
              <label>Tối đa mỗi ngày<input type="number" min="0" value={form.dailyLimit} onChange={e => update({ dailyLimit: e.target.value })} /><small>0 là không giới hạn.</small></label>
            </>
          )}
        </div>
        <ErrorAlert message={error} />
        <button type="submit" disabled={saving} aria-busy={saving}>Lưu</button>
        {saved && <span className="cell-sub"> Đã lưu.</span>}
      </form>
    </section>
  );
}

export function AdPlacements() {
  const { data, loading, error } = useAsyncData(() => monetizationClient.getAdPlacements(), []);
  return (
    <>
      <PageHeader title="Quảng cáo AdMob" description="Vị trí hiện quảng cáo trong app. Người dùng VIP không thấy quảng cáo nào." />
      <p className="hint">
        App ID của AdMob được gắn lúc build app (Android: <code>-Pluma.admobAppId</code>, iOS: <code>GADApplicationIdentifier</code> trong Info.plist).
        Ở đây chỉ quản lý ad unit và giới hạn hiện.
      </p>
      {loading ? <LoadingBlock /> : <ErrorAlert message={error} />}
      {data?.map(placement => <PlacementForm key={placement.id} placement={placement} />)}
    </>
  );
}
