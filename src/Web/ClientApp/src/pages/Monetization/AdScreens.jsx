import { useState } from 'react';
import { SaveAdScreenCommand } from '../../web-api-client';
import { getErrorMessage, monetizationClient } from '../../api/clients';
import { useAsyncData } from '../../hooks/useAsyncData';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';

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

export function AdScreens() {
  const [version, setVersion] = useState(0);
  const [editing, setEditing] = useState(null);
  const { data, loading, error } = useAsyncData(() => monetizationClient.getAdScreens(), [version]);
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
            <thead><tr><th>Mã</th><th>Tên</th><th>Trạng thái</th><th>Số vị trí</th><th /></tr></thead>
            <tbody>
              {editing === 'new' && <ScreenForm screen={blank} onSaved={saved} onCancel={() => setEditing(null)} />}
              {data.map(screen => editing === screen.id
                ? <ScreenForm key={screen.id} screen={screen} onSaved={saved} />
                : (
                  <tr key={screen.id}>
                    <td><code>{screen.code}</code></td>
                    <td>{screen.title}</td>
                    <td><span className={`status-badge ${screen.isEnabled ? 'ok' : ''}`}>{screen.isEnabled ? 'Bật' : 'Tắt'}</span></td>
                    <td>{screen.placementCount}</td>
                    <td><button type="button" className="secondary" onClick={() => setEditing(screen.id)}>Sửa</button></td>
                  </tr>
                ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
