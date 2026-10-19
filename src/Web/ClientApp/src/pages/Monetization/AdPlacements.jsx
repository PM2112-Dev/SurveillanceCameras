import { Fragment, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
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

/** Tóm tắt luật hiện quảng cáo của một vị trí, theo loại. */
export function placementRule(p) {
  const parts = [];
  if (p.format === 'native') {
    parts.push(p.everyItems > 0 ? `chèn sau mỗi ${p.everyItems} mục` : 'một lần ở đầu danh sách');
    if (p.displaySeconds > 0) parts.push(`tự tắt sau ${p.displaySeconds}s`);
  }
  if (p.format === 'interstitial' && p.everyEpisodes > 0) parts.push(`sau mỗi ${p.everyEpisodes} tập`);
  if (p.format === 'interstitial' && !(p.everyEpisodes > 0)) parts.push('tắt (chưa đặt số tập)');
  if ((p.format === 'interstitial' || p.format === 'app_open') && p.cooldownSeconds > 0) parts.push(`nghỉ ${p.cooldownSeconds}s`);
  if (p.dailyLimit > 0 && p.format !== 'banner' && p.format !== 'native') parts.push(`tối đa ${p.dailyLimit}/ngày`);
  return parts.length ? parts.join(' · ') : '—';
}

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
    <section className="profile-card" style={{ margin: 0 }}>
      <h2>
        {creating ? 'Thêm vị trí quảng cáo' : form.title}{' '}
        {!creating && <small><code>{form.code}</code> · {formatLabel[f] ?? f}</small>}
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
          <button type="button" className="secondary" onClick={onCancel}>{creating ? 'Huỷ' : 'Đóng'}</button>
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

const unitSummary = (id, label) => id
  ? <span title={id}>{label}{isTestUnit(id) && <span className="status-badge" title="Ad unit thử nghiệm của Google"> thử</span>}</span>
  : <span className="cell-sub">{label} —</span>;

export function AdPlacements() {
  const [version, setVersion] = useState(0);
  const [editing, setEditing] = useState(null); // 'new' | id
  const [params, setParams] = useSearchParams();
  const format = params.get('format') ?? '';
  const screen = params.get('screen') ?? '';
  const status = params.get('status') ?? '';
  const setFilter = (name, value) => setParams(current => {
    const next = new URLSearchParams(current);
    if (value) next.set(name, value); else next.delete(name);
    return next;
  }, { replace: true });
  const { data, loading, error } = useAsyncData(() => monetizationClient.getAdPlacements(), [version]);
  const screens = useAsyncData(() => monetizationClient.getAdScreens(), [version]);
  const screenList = screens.data ?? [];
  const screenOf = code => screenList.find(s => s.code === code);
  const saved = () => { setEditing(null); setVersion(v => v + 1); };

  const shown = (data ?? [])
    .filter(p => !format || p.format === format)
    .filter(p => !screen || (screen === '_all' ? !p.screenCode : p.screenCode === screen))
    .filter(p => !status || (status === 'on' ? p.isEnabled : !p.isEnabled))
    // Nhóm theo màn (theo thứ tự màn), trong màn theo loại rồi thứ tự thử.
    .sort((a, b) => {
      const order = code => { const i = screenList.findIndex(s => s.code === code); return code ? (i < 0 ? 999 : i) : -1; };
      return order(a.screenCode) - order(b.screenCode) || a.format.localeCompare(b.format) || a.priority - b.priority || a.id - b.id;
    });

  return (
    <>
      <PageHeader title="Quảng cáo AdMob" description="Vị trí hiện quảng cáo trong app. Mỗi loại có thể có nhiều vị trí; người dùng VIP không thấy quảng cáo nào." />
      <p className="hint">
        App ID của AdMob được gắn lúc build app (Android: <code>-Pluma.admobAppId</code>, iOS: <code>GADApplicationIdentifier</code> trong Info.plist).
        Ở đây quản lý ad unit, màn hiển thị và giới hạn. Màn hình khai báo ở trang <Link to="/monetization/ad-screens">Màn hình quảng cáo</Link>.
      </p>
      <div className="filter-bar">
        <select value={format} onChange={e => setFilter('format', e.target.value)} aria-label="Loại quảng cáo">
          <option value="">Mọi loại</option>
          {Object.entries(formatLabel).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
        </select>
        <select value={screen} onChange={e => setFilter('screen', e.target.value)} aria-label="Màn hiển thị">
          <option value="">Mọi màn</option>
          <option value="_all">Không gắn màn (hiện mọi màn)</option>
          {screenList.map(s => <option key={s.code} value={s.code}>{s.title} ({s.placementCount})</option>)}
        </select>
        <select value={status} onChange={e => setFilter('status', e.target.value)} aria-label="Trạng thái">
          <option value="">Bật và tắt</option>
          <option value="on">Đang bật</option>
          <option value="off">Đang tắt</option>
        </select>
        <button type="button" onClick={() => setEditing('new')} disabled={editing === 'new'}>Thêm vị trí</button>
      </div>
      {loading && !data ? <LoadingBlock /> : <ErrorAlert message={error || screens.error} />}
      {data && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr><th>Vị trí</th><th>Loại</th><th>Màn hiển thị</th><th>Luật hiển thị</th><th>Ad unit</th><th>Thứ tự</th><th>Trạng thái</th><th /></tr>
            </thead>
            <tbody>
              {editing === 'new' && (
                <tr><td colSpan={8}>
                  <PlacementForm placement={blank} screens={screenList} onCancel={() => setEditing(null)} onSaved={saved} />
                </td></tr>
              )}
              {shown.map(p => {
                const s = screenOf(p.screenCode);
                return (
                  <Fragment key={p.id}>
                    <tr>
                      <td><strong>{p.title || p.code}</strong><div className="cell-sub"><code>{p.code}</code></div></td>
                      <td>{formatLabel[p.format] ?? p.format}</td>
                      <td>
                        {!p.screenCode
                          ? <span className="cell-sub">{needsScreen(p.format) ? 'Chưa chọn màn' : 'Mọi màn'}</span>
                          : <>
                              <Link to={`/monetization/ad-screens?screen=${encodeURIComponent(p.screenCode)}`}>{s?.title ?? p.screenCode}</Link>
                              {!s && <span className="status-badge" title="Mã màn chưa được khai báo ở trang Màn hình quảng cáo"> chưa khai báo</span>}
                              {s && !s.isEnabled && <span className="status-badge" title="Màn đang tắt nên quảng cáo này không hiện"> màn tắt</span>}
                            </>}
                      </td>
                      <td>{placementRule(p)}</td>
                      <td style={{ whiteSpace: 'nowrap' }}>{unitSummary(p.androidUnitId, 'Android')}<br />{unitSummary(p.iosUnitId, 'iOS')}</td>
                      <td>{p.priority}</td>
                      <td><span className={`status-badge ${p.isEnabled ? 'ok' : ''}`}>{p.isEnabled ? 'Bật' : 'Tắt'}</span></td>
                      <td><button type="button" className="secondary" onClick={() => setEditing(editing === p.id ? null : p.id)}>{editing === p.id ? 'Đóng' : 'Sửa'}</button></td>
                    </tr>
                    {editing === p.id && (
                      <tr><td colSpan={8}>
                        <PlacementForm placement={p} screens={screenList} onCancel={() => setEditing(null)} onSaved={saved} onDeleted={saved} />
                      </td></tr>
                    )}
                  </Fragment>
                );
              })}
              {shown.length === 0 && <tr><td colSpan={8} className="cell-sub">Không có vị trí nào khớp bộ lọc.</td></tr>}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
