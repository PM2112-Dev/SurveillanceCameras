import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { SaveAdScreenCommand } from '../../web-api-client';
import { getErrorMessage, monetizationClient } from '../../api/clients';
import { useAsyncData } from '../../hooks/useAsyncData';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { formatLabel, placementRule } from './AdPlacements';

const builtIn = ['splash', 'home', 'search', 'recommended', 'watch', 'library', 'profile', 'downloads'];
const blank = { id: 0, code: '', title: '', isEnabled: true, sortOrder: 0, placementCount: 0 };

function ScreenForm({ screen, onSaved, onCancel }) {
  const creating = screen.id === 0;
  const [form, setForm] = useState({ ...screen, title: screen.title ?? '' });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [confirmDelete, setConfirmDelete] = useState(false);
  const update = patch => setForm(current => ({ ...current, ...patch }));

  const run = promise => {
    setSaving(true);
    setError('');
    promise.then(onSaved).catch(err => { setError(getErrorMessage(err)); setConfirmDelete(false); }).finally(() => setSaving(false));
  };
  const submit = event => {
    event.preventDefault();
    const command = new SaveAdScreenCommand({ id: form.id, code: form.code, title: form.title, isEnabled: form.isEnabled, sortOrder: Number(form.sortOrder) || 0 });
    run(creating ? monetizationClient.createAdScreen(command) : monetizationClient.updateAdScreen(form.id, command));
  };

  return (
    <tr>
      <td colSpan={5}>
        <form onSubmit={submit} className="filter-bar" style={{ alignItems: 'end', margin: 0 }}>
          <label>Mã{' '}
            <input required value={form.code} disabled={!creating} pattern="[a-z0-9_]+" placeholder="vd. player_end" onChange={e => update({ code: e.target.value })} />
          </label>
          <label>Tên màn<input required value={form.title} onChange={e => update({ title: e.target.value })} /></label>
          <label>Thứ tự<input type="number" min="0" value={form.sortOrder} onChange={e => update({ sortOrder: e.target.value })} /></label>
          <label><input type="checkbox" role="switch" checked={form.isEnabled} onChange={e => update({ isEnabled: e.target.checked })} /> Bật</label>
          <button type="submit" disabled={saving} aria-busy={saving}>{creating ? 'Thêm' : 'Lưu'}</button>
          {!creating && !builtIn.includes(form.code) && form.placementCount === 0 && (
            <button type="button" className="danger" disabled={saving} onClick={() => setConfirmDelete(true)}>Xoá</button>
          )}
          {creating && <button type="button" className="secondary" onClick={onCancel}>Huỷ</button>}
        </form>
        <ErrorAlert message={error} />
        <ConfirmDialog open={confirmDelete} title="Xoá màn hình" busy={saving} onCancel={() => setConfirmDelete(false)}
          onConfirm={() => run(monetizationClient.deleteAdScreen(form.id))} message={`Xoá màn "${form.title}"?`} />
      </td>
    </tr>
  );
}

/** Các quảng cáo gắn với một màn: mỗi dòng dẫn sang trang Quảng cáo đã lọc theo màn. */
function ScreenPlacements({ code, placements }) {
  if (placements.length === 0) return <span className="cell-sub">Chưa có quảng cáo</span>;
  return (
    <ul className="plain-list" style={{ margin: 0, paddingLeft: '1.1em' }}>
      {placements.map(p => (
        <li key={p.id} style={{ opacity: p.isEnabled ? 1 : 0.55 }}>
          <Link to={`/monetization/ads?screen=${encodeURIComponent(code)}&format=${p.format}`}>{p.title || p.code}</Link>
          {' '}<span className="cell-sub">{formatLabel[p.format] ?? p.format} · {placementRule(p)}{p.isEnabled ? '' : ' · đang tắt'}</span>
        </li>
      ))}
    </ul>
  );
}

export function AdScreens() {
  const [version, setVersion] = useState(0);
  const [editing, setEditing] = useState(null);
  const [params] = useSearchParams();
  const focus = params.get('screen');
  const { data, loading, error } = useAsyncData(() => monetizationClient.getAdScreens(), [version]);
  const placements = useAsyncData(() => monetizationClient.getAdPlacements(), [version]);
  const all = placements.data ?? [];
  const forScreen = code => all.filter(p => p.screenCode === code);
  const everywhere = all.filter(p => !p.screenCode);
  const orphaned = all.filter(p => p.screenCode && !data?.some(s => s.code === p.screenCode));
  const saved = () => { setEditing(null); setVersion(v => v + 1); };

  return (
    <>
      <PageHeader title="Màn hình quảng cáo" description="Khai báo màn hình của app để gắn quảng cáo và xem thống kê theo màn. Tắt một màn thì mọi quảng cáo của màn đó ngừng hiện." />
      <p className="hint">
        Mã màn là tên app dùng khi báo thống kê. Các màn có sẵn đã được app gắn khung hiển thị; màn tự thêm chỉ có tác dụng khi bản app có
        màn cùng mã.
      </p>
      <div className="filter-bar"><button type="button" onClick={() => setEditing('new')} disabled={editing === 'new'}>Thêm màn</button></div>
      {loading && !data ? <LoadingBlock /> : <ErrorAlert message={error} />}
      {data && (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Mã</th><th>Tên</th><th>Trạng thái</th><th>Quảng cáo của màn</th><th /></tr></thead>
            <tbody>
              {editing === 'new' && <ScreenForm screen={blank} onSaved={saved} onCancel={() => setEditing(null)} />}
              {data.map(screen => editing === screen.id
                ? <ScreenForm key={screen.id} screen={screen} onSaved={saved} />
                : (
                  <tr key={screen.id} style={focus === screen.code ? { boxShadow: 'inset 4px 0 0 #7442be', background: 'rgba(116, 66, 190, 0.08)' } : undefined}>
                    <td><code>{screen.code}</code></td>
                    <td>{screen.title}</td>
                    <td><span className={`status-badge ${screen.isEnabled ? 'ok' : ''}`}>{screen.isEnabled ? 'Bật' : 'Tắt'}</span></td>
                    <td>
                      <ScreenPlacements code={screen.code} placements={forScreen(screen.code)} />
                      {!screen.isEnabled && forScreen(screen.code).length > 0 && <div className="cell-sub">Màn đang tắt nên các quảng cáo trên không hiện.</div>}
                    </td>
                    <td>
                      <button type="button" className="secondary" onClick={() => setEditing(screen.id)}>Sửa</button>{' '}
                      <Link role="button" className="secondary" to={`/monetization/ads?screen=${encodeURIComponent(screen.code)}`}>Quảng cáo</Link>
                    </td>
                  </tr>
                ))}
              <tr>
                <td colSpan={2}><em>Không gắn màn</em></td><td />
                <td>
                  {everywhere.length === 0 ? <span className="cell-sub">Không có</span> : (
                    <ul style={{ margin: 0, paddingLeft: '1.1em' }}>
                      {everywhere.map(p => <li key={p.id}><Link to={`/monetization/ads?screen=_all&format=${p.format}`}>{p.title || p.code}</Link> <span className="cell-sub">{formatLabel[p.format] ?? p.format} · hiện mọi màn</span></li>)}
                    </ul>
                  )}
                </td>
                <td />
              </tr>
            </tbody>
          </table>
        </div>
      )}
      {orphaned.length > 0 && (
        <p className="hint">
          Có quảng cáo gắn mã màn chưa khai báo ở đây: {orphaned.map(p => <span key={p.id}><Link to={`/monetization/ads?screen=${encodeURIComponent(p.screenCode)}`}>{p.title || p.code}</Link> (<code>{p.screenCode}</code>) </span>)}
          — thêm màn có mã đó hoặc đổi màn của quảng cáo.
        </p>
      )}
    </>
  );
}
