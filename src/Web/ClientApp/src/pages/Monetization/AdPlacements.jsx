import { useState } from 'react';
import { SaveAdPlacementCommand } from '../../web-api-client';
import { getErrorMessage, monetizationClient } from '../../api/clients';
import { useAsyncData } from '../../hooks/useAsyncData';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';

export const formatLabel = {
  rewarded: 'Có thưởng', interstitial: 'Toàn màn hình', banner: 'Banner', native: 'Native', app_open: 'Mở app'
};
const formatHint = {
  rewarded: 'Xem hết để mở một tập.',
  interstitial: 'Chen giữa các tập.',
  banner: 'Hiện ở cuối màn được chọn.',
  native: 'Chèn vào danh sách của màn được chọn, tự tắt sau số giây đặt bên dưới.',
  app_open: 'Hiện khi mở app và khi quay lại app.'
};
const builtIn = ['rewarded_unlock', 'interstitial_episode', 'banner_home'];
const needsScreen = format => format === 'banner' || format === 'native';
const isTestUnit = id => id?.startsWith('ca-app-pub-3940256099942544/');

const blank = {
  id: 0, code: '', title: '', format: 'native', androidUnitId: '', iosUnitId: '', isEnabled: true, dailyLimit: 0, cooldownSeconds: 0,
  everyEpisodes: 0, screenCode: '', priority: 0, displaySeconds: 0, everyItems: 0
};

function PlacementForm({ placement, screens, onSaved, onDeleted, onCancel }) {
  const creating = placement.id === 0;
  const [form, setForm] = useState({
    ...placement, title: placement.title ?? '', androidUnitId: placement.androidUnitId ?? '', iosUnitId: placement.iosUnitId ?? '',
    screenCode: placement.screenCode ?? ''
  });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [saved, setSaved] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);
  const update = patch => { setForm(current => ({ ...current, ...patch })); setSaved(false); };
  const f = form.format;

  const submit = event => {
    event.preventDefault();
    setSaving(true);
    setError('');
    const command = new SaveAdPlacementCommand({
      id: form.id, code: form.code, title: form.title, format: form.format,
      androidUnitId: form.androidUnitId || undefined, iosUnitId: form.iosUnitId || undefined, isEnabled: form.isEnabled,
      dailyLimit: Number(form.dailyLimit) || 0, cooldownSeconds: Number(form.cooldownSeconds) || 0,
      everyEpisodes: Number(form.everyEpisodes) || 0, screenCode: form.screenCode || undefined,
      priority: Number(form.priority) || 0, displaySeconds: Number(form.displaySeconds) || 0, everyItems: Number(form.everyItems) || 0
    });
    (creating ? monetizationClient.createAdPlacement(command) : monetizationClient.updateAdPlacement(form.id, command))
      .then(() => { setSaved(true); onSaved(); })
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setSaving(false));
  };

  const remove = () => {
    setSaving(true);
    monetizationClient.deleteAdPlacement(form.id)
      .then(onDeleted)
      .catch(err => { setError(getErrorMessage(err)); setConfirmDelete(false); })
      .finally(() => setSaving(false));
  };

  return (
    <section className="profile-card" style={{ marginBottom: 'var(--pico-spacing)' }}>
      <h2>
        {creating ? 'Thêm vị trí quảng cáo' : form.title}{' '}
        {!creating && <small><code>{form.code}</code> · {formatLabel[f] ?? f}{form.screenCode && <> · màn <code>{form.screenCode}</code></>}</small>}
      </h2>
      <form onSubmit={submit}>
        {creating && (
          <div className="grid">
            <label>Loại quảng cáo
              <select value={f} onChange={e => update({ format: e.target.value })}>
                {Object.entries(formatLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
              </select>
              <small>{formatHint[f]}</small>
            </label>
            <label>Mã vị trí
              <input required value={form.code} placeholder="vd. native_search_2" pattern="[a-z0-9_]+" onChange={e => update({ code: e.target.value })} />
              <small>Chữ thường, số, _. Không đổi được sau khi tạo.</small>
            </label>
          </div>
        )}
        <label>Tên gọi<input required value={form.title} onChange={e => update({ title: e.target.value })} /></label>
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
          {f !== 'rewarded' && f !== 'app_open' && (
            <label>Màn hiển thị
              <select value={form.screenCode} onChange={e => update({ screenCode: e.target.value })} required={needsScreen(f)}>
                <option value="">{needsScreen(f) ? '— chọn màn —' : 'Mọi màn'}</option>
                {screens.map(s => <option key={s.code} value={s.code}>{s.title} ({s.code}){s.isEnabled ? '' : ' — đang tắt'}</option>)}
              </select>
            </label>
          )}
          <label>Thứ tự thử
            <input type="number" min="0" value={form.priority} onChange={e => update({ priority: e.target.value })} />
            <small>Nhiều vị trí cùng loại, cùng màn: app thử số nhỏ trước, không có quảng cáo thì thử vị trí kế.</small>
          </label>
        </div>
        <div className="grid">
          {f === 'native' && (
            <>
              <label>Tự tắt sau (giây)<input type="number" min="0" value={form.displaySeconds} onChange={e => update({ displaySeconds: e.target.value })} /><small>0 là không tự tắt.</small></label>
              <label>Chèn sau mỗi … mục<input type="number" min="0" value={form.everyItems} onChange={e => update({ everyItems: e.target.value })} /><small>0 là chỉ chèn một lần ở đầu danh sách.</small></label>
            </>
          )}
          {f === 'interstitial' && (
            <label>Sau mỗi … tập<input type="number" min="0" value={form.everyEpisodes} onChange={e => update({ everyEpisodes: e.target.value })} /><small>0 là tắt.</small></label>
          )}
          {(f === 'interstitial' || f === 'app_open') && (
            <label>Nghỉ tối thiểu (giây)<input type="number" min="0" value={form.cooldownSeconds} onChange={e => update({ cooldownSeconds: e.target.value })} /></label>
          )}
          {(f === 'rewarded' || f === 'interstitial' || f === 'app_open') && (
            <label>Tối đa mỗi ngày<input type="number" min="0" value={form.dailyLimit} onChange={e => update({ dailyLimit: e.target.value })} />
              <small>{f === 'rewarded' ? 'Số tập mỗi người mở bằng quảng cáo trong ngày; ' : ''}0 là không giới hạn.</small></label>
          )}
        </div>
        <ErrorAlert message={error} />
        <div className="actions">
          <button type="submit" disabled={saving} aria-busy={saving}>{creating ? 'Thêm vị trí' : 'Lưu'}</button>
          {creating && <button type="button" className="secondary" onClick={onCancel}>Huỷ</button>}
          {!creating && !builtIn.includes(form.code) && (
            <button type="button" className="danger" disabled={saving} onClick={() => setConfirmDelete(true)}>Xoá</button>
          )}
          {saved && <span className="cell-sub"> Đã lưu.</span>}
        </div>
      </form>
      <ConfirmDialog open={confirmDelete} title="Xoá vị trí quảng cáo" busy={saving} onCancel={() => setConfirmDelete(false)} onConfirm={remove}
        message={`Xoá "${form.title}"? App ngừng hiện quảng cáo này ở lần tải cấu hình tiếp theo; thống kê cũ vẫn giữ.`} />
    </section>
  );
}

export function AdPlacements() {
  const [version, setVersion] = useState(0);
  const [adding, setAdding] = useState(false);
  const [filter, setFilter] = useState('');
  const { data, loading, error } = useAsyncData(() => monetizationClient.getAdPlacements(), [version]);
  const screens = useAsyncData(() => monetizationClient.getAdScreens(), [version]);
  const reload = () => setVersion(v => v + 1);
  const shown = data?.filter(p => !filter || p.format === filter);

  return (
    <>
      <PageHeader title="Quảng cáo AdMob" description="Vị trí hiện quảng cáo trong app. Mỗi loại có thể có nhiều vị trí; người dùng VIP không thấy quảng cáo nào." />
      <p className="hint">
        App ID của AdMob được gắn lúc build app (Android: <code>-Pluma.admobAppId</code>, iOS: <code>GADApplicationIdentifier</code> trong Info.plist).
        Ở đây quản lý ad unit, màn hiển thị và giới hạn. Màn hình khai báo ở trang <a href="/monetization/ad-screens">Màn hình quảng cáo</a>.
      </p>
      <div className="filter-bar">
        <select value={filter} onChange={e => setFilter(e.target.value)}>
          <option value="">Mọi loại</option>
          {Object.entries(formatLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        <button type="button" onClick={() => setAdding(true)} disabled={adding}>Thêm vị trí</button>
      </div>
      {adding && <PlacementForm placement={blank} screens={screens.data ?? []} onCancel={() => setAdding(false)}
        onSaved={() => { setAdding(false); reload(); }} />}
      {loading && !data ? <LoadingBlock /> : <ErrorAlert message={error || screens.error} />}
      {shown?.map(placement => (
        <PlacementForm key={`${placement.id}-${version}`} placement={placement} screens={screens.data ?? []} onSaved={reload} onDeleted={reload} />
      ))}
    </>
  );
}
