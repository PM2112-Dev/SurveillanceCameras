import { useState } from 'react';
import { Pencil, Plus, Trash2 } from 'lucide-react';
import { SaveSubscriptionPlanCommand } from '../../web-api-client';
import { getErrorMessage, monetizationClient } from '../../api/clients';
import { useAsyncData } from '../../hooks/useAsyncData';
import { PageHeader } from '../../components/common/PageHeader';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';

const emptyPlan = { id: 0, code: '', title: '', description: '', appleProductId: '', googleProductId: '', periodDays: 30, trialDays: 0, isHighlighted: false, isActive: true, sortOrder: 0 };

function PlanForm({ plan, onDone }) {
  const [form, setForm] = useState({ ...emptyPlan, ...plan, description: plan.description ?? '', appleProductId: plan.appleProductId ?? '', googleProductId: plan.googleProductId ?? '' });
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const update = patch => setForm(current => ({ ...current, ...patch }));

  const submit = event => {
    event.preventDefault();
    setSaving(true);
    setError('');
    const body = new SaveSubscriptionPlanCommand({
      ...form,
      periodDays: Number(form.periodDays), trialDays: Number(form.trialDays) || 0, sortOrder: Number(form.sortOrder) || 0,
      description: form.description || undefined, appleProductId: form.appleProductId || undefined, googleProductId: form.googleProductId || undefined
    });
    (form.id ? monetizationClient.updateSubscriptionPlan(form.id, body) : monetizationClient.createSubscriptionPlan(body))
      .then(() => onDone(true))
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setSaving(false));
  };

  return (
    <section className="profile-card">
      <h2>{form.id ? `Sửa gói “${plan.title}”` : 'Thêm gói VIP'}</h2>
      <form onSubmit={submit}>
        <div className="grid">
          <label>Mã gói<input required pattern="[a-z0-9_\-]+" placeholder="month" value={form.code} onChange={e => update({ code: e.target.value })} /></label>
          <label>Tên hiện trong app<input required placeholder="Gói tháng" value={form.title} onChange={e => update({ title: e.target.value })} /></label>
        </div>
        <label>Mô tả ngắn<input placeholder="Tiết kiệm 40%" value={form.description} onChange={e => update({ description: e.target.value })} /></label>
        <div className="grid">
          <label>Product ID App Store<input placeholder="com.aica.movie.vip.month" value={form.appleProductId} onChange={e => update({ appleProductId: e.target.value })} /></label>
          <label>Product ID Google Play<input placeholder="vip_month" value={form.googleProductId} onChange={e => update({ googleProductId: e.target.value })} /></label>
        </div>
        <small>Phải trùng gói tự gia hạn đã tạo trên App Store Connect / Google Play Console. Giá do cửa hàng quyết định, app tự lấy giá theo tiền tệ của người xem.</small>
        <div className="grid">
          <label>Số ngày mỗi kỳ<input type="number" min="1" required value={form.periodDays} onChange={e => update({ periodDays: e.target.value })} /></label>
          <label>Số ngày dùng thử<input type="number" min="0" value={form.trialDays} onChange={e => update({ trialDays: e.target.value })} /></label>
          <label>Thứ tự<input type="number" value={form.sortOrder} onChange={e => update({ sortOrder: e.target.value })} /></label>
        </div>
        <fieldset>
          <label><input type="checkbox" checked={form.isHighlighted} onChange={e => update({ isHighlighted: e.target.checked })} /> Làm nổi bật và chọn sẵn</label>
          <label><input type="checkbox" checked={form.isActive} onChange={e => update({ isActive: e.target.checked })} /> Đang bán</label>
        </fieldset>
        <ErrorAlert message={error} />
        <div className="grid">
          <button type="submit" disabled={saving} aria-busy={saving}>Lưu gói</button>
          <button type="button" className="secondary" onClick={() => onDone(false)}>Huỷ</button>
        </div>
      </form>
    </section>
  );
}

export function SubscriptionPlans() {
  const [version, setVersion] = useState(0);
  const [editing, setEditing] = useState(null);
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);
  const { data, loading, error } = useAsyncData(() => monetizationClient.getSubscriptionPlans(), [version]);

  const done = changed => { setEditing(null); if (changed) setVersion(v => v + 1); };
  const remove = () => {
    setDeleting(true);
    monetizationClient.deleteSubscriptionPlan(deleteTarget.id)
      .then(() => { setDeleteTarget(null); setVersion(v => v + 1); })
      .finally(() => setDeleting(false));
  };

  return (
    <>
      <PageHeader title="Gói VIP" description="Các gói tự gia hạn bán trong app. Gói thiếu product ID của một nền tảng thì không hiện trên nền tảng đó."
        actions={!editing && <button type="button" onClick={() => setEditing(emptyPlan)}><Plus size={16} aria-hidden="true" /> Thêm gói</button>} />
      {editing && <PlanForm plan={editing} onDone={done} />}
      {loading ? <LoadingBlock /> : <ErrorAlert message={error} />}
      {data && data.length === 0 && <EmptyState message="Chưa có gói nào. App sẽ ẩn nút mua cho tới khi có gói đang bán." />}
      {data && data.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead><tr><th>Gói</th><th>App Store</th><th>Google Play</th><th>Kỳ</th><th>Trạng thái</th><th className="col-actions">Thao tác</th></tr></thead>
            <tbody>
              {data.map(plan => (
                <tr key={plan.id}>
                  <td>{plan.title} <code>{plan.code}</code>{plan.isHighlighted && <span className="status-badge warn"> Nổi bật</span>}<span className="cell-sub">{plan.description}</span></td>
                  <td>{plan.appleProductId ? <code>{plan.appleProductId}</code> : <em>—</em>}</td>
                  <td>{plan.googleProductId ? <code>{plan.googleProductId}</code> : <em>—</em>}</td>
                  <td>{plan.periodDays} ngày{plan.trialDays > 0 && <span className="cell-sub">Dùng thử {plan.trialDays} ngày</span>}</td>
                  <td><span className={`status-badge ${plan.isActive ? 'ok' : ''}`}>{plan.isActive ? 'Đang bán' : 'Ngừng bán'}</span></td>
                  <td className="col-actions">
                    <button type="button" className="icon-btn" title="Sửa" onClick={() => setEditing(plan)}><Pencil size={16} aria-hidden="true" /></button>
                    <button type="button" className="icon-btn danger" title="Xoá" onClick={() => setDeleteTarget(plan)}><Trash2 size={16} aria-hidden="true" /></button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <ConfirmDialog open={Boolean(deleteTarget)} title="Xoá gói VIP" busy={deleting}
        message={`Xoá gói “${deleteTarget?.title ?? ''}”? Người đã mua vẫn giữ VIP tới hết hạn; app thôi hiện gói này.`}
        onCancel={() => setDeleteTarget(null)} onConfirm={remove} />
    </>
  );
}
