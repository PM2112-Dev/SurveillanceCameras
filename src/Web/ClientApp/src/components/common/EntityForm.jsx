import { useEffect, useRef, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { getErrorMessage } from '../../api/clients';
import { PageHeader } from './PageHeader';
import { ErrorAlert, LoadingBlock } from './StateBlocks';

/**
 * Quản lý trạng thái form thêm/sửa dùng chung.
 *
 * - `initial`: giá trị form khi tạo mới.
 * - `loadEntity(id)`: tải bản ghi khi sửa; kết quả được đổi thành giá trị form bởi `toForm(entity)`.
 * - `save({ isEdit, id, form })`: gọi API tạo/cập nhật.
 */
export function useEntityForm({ initial, loadEntity, toForm, save, backTo }) {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEdit = id !== undefined;

  const [form, setForm] = useState(initial);
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  // Các hàm truyền vào thường là inline; giữ bản mới nhất để effect chỉ chạy theo `id`.
  const latest = useRef({ loadEntity, toForm });
  latest.current = { loadEntity, toForm };

  useEffect(() => {
    if (!isEdit) return;

    latest.current.loadEntity(Number(id))
      .then(entity => setForm(latest.current.toForm(entity)))
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setLoading(false));
  }, [id, isEdit]);

  const update = patch => setForm(prev => ({ ...prev, ...patch }));

  const submit = event => {
    event.preventDefault();
    setSaving(true);
    setError('');

    save({ isEdit, id: isEdit ? Number(id) : undefined, form })
      .then(() => navigate(backTo))
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setSaving(false));
  };

  return { form, update, isEdit, loading, saving, error, submit, cancel: () => navigate(backTo) };
}

/** Khung trang form: tiêu đề, thông báo lỗi và hai nút Huỷ / Lưu. */
export function EntityForm({ title, state, children }) {
  const { isEdit, loading, saving, error, submit, cancel } = state;

  if (loading) return <LoadingBlock />;

  return (
    <>
      <PageHeader title={title} />

      <form onSubmit={submit} className="form-card">
        <ErrorAlert message={error} />

        {children}

        <div className="form-actions">
          <button type="button" className="secondary" onClick={cancel}>
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
