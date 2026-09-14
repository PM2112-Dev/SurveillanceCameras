import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { CreateWebSourceCommand, UpdateWebSourceCommand } from '../../web-api-client';
import { webSourcesClient, getErrorMessage } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';

export function WebSourceForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEdit = id !== undefined;

  const [form, setForm] = useState({ title: '', baseUrl: '' });
  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!isEdit) return;

    webSourcesClient.getWebSourceById(Number(id))
      .then(entity => setForm({ title: entity.title ?? '', baseUrl: entity.baseUrl ?? '' }))
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setLoading(false));
  }, [id, isEdit]);

  const handleSubmit = event => {
    event.preventDefault();
    setSaving(true);
    setError('');

    const request = isEdit
      ? webSourcesClient.updateWebSource(
          Number(id),
          new UpdateWebSourceCommand({ id: Number(id), title: form.title, baseUrl: form.baseUrl })
        )
      : webSourcesClient.createWebSource(
          new CreateWebSourceCommand({ title: form.title, baseUrl: form.baseUrl })
        );

    request
      .then(() => navigate('/web-sources'))
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setSaving(false));
  };

  if (loading) return <LoadingBlock />;

  return (
    <>
      <PageHeader title={isEdit ? 'Sửa nguồn web' : 'Thêm nguồn web'} />

      <form onSubmit={handleSubmit} className="form-card">
        <ErrorAlert message={error} />

        <label>
          Tên nguồn <span className="required">*</span>
          <input
            type="text"
            required
            maxLength={500}
            value={form.title}
            onChange={event => setForm({ ...form, title: event.target.value })}
            placeholder="VD: Wikidich"
          />
        </label>

        <label>
          Base URL <span className="required">*</span>
          <input
            type="url"
            required
            maxLength={2000}
            value={form.baseUrl}
            onChange={event => setForm({ ...form, baseUrl: event.target.value })}
            placeholder="https://example.com"
          />
        </label>

        <div className="form-actions">
          <button type="button" className="secondary" onClick={() => navigate('/web-sources')}>
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
