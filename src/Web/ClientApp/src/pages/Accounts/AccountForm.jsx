import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { CreateAccountCommand, UpdateAccountCommand } from '../../web-api-client';
import { accountsClient, accountTypesClient, getErrorMessage } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';

export function AccountForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEdit = id !== undefined;

  // cookie không có ô nhập: nó do phiên TikTok tự cập nhật. Giữ nguyên giá trị đã tải để
  // UpdateAccountCommand (ghi đè toàn bộ trường) không xoá mất cookie khi chỉ sửa tên/loại.
  const [form, setForm] = useState({ title: '', accountTypeId: '', cookie: undefined });
  const [accountTypes, setAccountTypes] = useState([]);
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    accountTypesClient.getAccountTypes(undefined, 1, 100)
      .then(types => setAccountTypes(types.items ?? []))
      .catch(() => {});
  }, []);

  useEffect(() => {
    if (!isEdit) return;

    accountsClient.getAccountById(Number(id))
      .then(entity => setForm({
        title: entity.title ?? '',
        accountTypeId: entity.accountTypeId ? String(entity.accountTypeId) : '',
        cookie: entity.cookie
      }))
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setLoading(false));
  }, [id, isEdit]);

  const handleSubmit = event => {
    event.preventDefault();
    setSaving(true);
    setError('');

    const accountTypeId = form.accountTypeId ? Number(form.accountTypeId) : 0;

    const request = isEdit
      ? accountsClient.updateAccount(
          Number(id),
          new UpdateAccountCommand({ id: Number(id), title: form.title, accountTypeId, cookie: form.cookie })
        )
      : accountsClient.createAccount(
          new CreateAccountCommand({ title: form.title, accountTypeId })
        );

    request
      .then(() => navigate('/accounts'))
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setSaving(false));
  };

  if (loading) return <LoadingBlock />;

  return (
    <>
      <PageHeader title={isEdit ? 'Sửa tài khoản' : 'Thêm tài khoản'} />

      <form onSubmit={handleSubmit} className="form-card">
        <ErrorAlert message={error} />

        <label>
          Tên tài khoản <span className="required">*</span>
          <input
            type="text"
            required
            maxLength={500}
            value={form.title}
            onChange={event => setForm({ ...form, title: event.target.value })}
            placeholder="VD: Kênh TikTok chính"
          />
        </label>

        <label>
          Loại tài khoản
          <select
            value={form.accountTypeId}
            onChange={event => setForm({ ...form, accountTypeId: event.target.value })}
          >
            <option value="">— Không chọn —</option>
            {accountTypes.map(type => (
              <option key={type.id} value={type.id}>{type.title}</option>
            ))}
          </select>
        </label>

        <p className="hint">
          Cookie không nhập tay ở đây. Sau khi lưu, vào danh sách tài khoản và bấm “Mở Chrome” để đăng nhập TikTok;
          cookie sẽ được lấy và cập nhật tự động.
        </p>

        <div className="form-actions">
          <button type="button" className="secondary" onClick={() => navigate('/accounts')}>
            Huỷ
          </button>
          <button type="submit" disabled={saving} aria-busy={saving}>
            {isEdit ? 'Lưu thay đổi' : 'Tạo mới'}
          </button>
        </div>
      </form>
    </>
  );
}
