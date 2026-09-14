import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { CreateCategoryCommand, UpdateCategoryCommand } from '../../web-api-client';
import { categoriesClient, getErrorMessage } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';

export function CategoryForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEdit = id !== undefined;

  const [title, setTitle] = useState('');
  const [storySourceIds, setStorySourceIds] = useState([]);
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!isEdit) return;

    categoriesClient.getCategoryById(Number(id))
      .then(entity => {
        setTitle(entity.title ?? '');
        // Giữ nguyên liên kết nguồn truyện hiện có — màn hình này chỉ sửa tên,
        // việc gán thể loại cho nguồn truyện làm ở form Nguồn truyện.
        setStorySourceIds((entity.storySources ?? []).map(x => x.id));
      })
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setLoading(false));
  }, [id, isEdit]);

  const handleSubmit = event => {
    event.preventDefault();
    setSaving(true);
    setError('');

    const request = isEdit
      ? categoriesClient.updateCategory(
          Number(id),
          new UpdateCategoryCommand({ id: Number(id), title, storySourceIds })
        )
      : categoriesClient.createCategory(new CreateCategoryCommand({ title }));

    request
      .then(() => navigate('/categories'))
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setSaving(false));
  };

  if (loading) return <LoadingBlock />;

  return (
    <>
      <PageHeader title={isEdit ? 'Sửa thể loại' : 'Thêm thể loại'} />

      <form onSubmit={handleSubmit} className="form-card">
        <ErrorAlert message={error} />

        <label>
          Tên thể loại <span className="required">*</span>
          <input
            type="text"
            required
            maxLength={500}
            value={title}
            onChange={event => setTitle(event.target.value)}
            placeholder="VD: Tiên hiệp"
          />
        </label>

        {isEdit && (
          <small>Đang liên kết với {storySourceIds.length} nguồn truyện.</small>
        )}

        <div className="form-actions">
          <button type="button" className="secondary" onClick={() => navigate('/categories')}>
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
