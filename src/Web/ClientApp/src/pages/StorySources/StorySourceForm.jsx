import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { CreateStorySourceCommand, UpdateStorySourceCommand } from '../../web-api-client';
import { categoriesClient, storySourcesClient, webSourcesClient, getErrorMessage } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';

const emptyForm = {
  title: '',
  webSourceId: '',
  storySourceType: 0,
  sinoVietnamese: '',
  author: '',
  description: '',
  linkRaw: '',
  status: '',
  imageUrl: '',
  totalChapters: ''
};

export function StorySourceForm() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEdit = id !== undefined;

  const [form, setForm] = useState(emptyForm);
  const [categoryIds, setCategoryIds] = useState([]);
  const [webSources, setWebSources] = useState([]);
  const [categories, setCategories] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    const loadLookups = Promise.all([
      webSourcesClient.getWebSource(undefined, 1, 100),
      categoriesClient.getCategories(undefined, 1, 100)
    ]).then(([ws, cats]) => {
      setWebSources(ws.items ?? []);
      setCategories(cats.items ?? []);
    });

    const loadEntity = isEdit
      ? storySourcesClient.getStorySourceById(Number(id)).then(entity => {
          setForm({
            title: entity.title ?? '',
            webSourceId: entity.webSourceId ?? '',
            storySourceType: entity.storySourceType ?? 0,
            sinoVietnamese: entity.sinoVietnamese ?? '',
            author: entity.author ?? '',
            description: entity.description ?? '',
            linkRaw: entity.linkRaw ?? '',
            status: entity.status ?? '',
            imageUrl: entity.imageUrl ?? '',
            totalChapters: entity.totalChapters ?? ''
          });
          setCategoryIds((entity.categories ?? []).map(c => c.id));
        })
      : Promise.resolve();

    Promise.all([loadLookups, loadEntity])
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setLoading(false));
  }, [id, isEdit]);

  const setField = (name, value) => setForm(prev => ({ ...prev, [name]: value }));

  const toggleCategory = catId =>
    setCategoryIds(prev =>
      prev.includes(catId) ? prev.filter(x => x !== catId) : [...prev, catId]
    );

  const handleSubmit = event => {
    event.preventDefault();
    setSaving(true);
    setError('');

    const payload = {
      title: form.title,
      webSourceId: Number(form.webSourceId),
      storySourceType: Number(form.storySourceType) || 0,
      sinoVietnamese: form.sinoVietnamese || undefined,
      author: form.author || undefined,
      description: form.description || undefined,
      linkRaw: form.linkRaw || undefined,
      status: form.status || undefined,
      lastUpdate: new Date(),
      imageUrl: form.imageUrl || undefined,
      totalChapters: form.totalChapters === '' ? undefined : Number(form.totalChapters),
      categoryIds
    };

    const request = isEdit
      ? storySourcesClient.updateStorySource(
          Number(id),
          new UpdateStorySourceCommand({ id: Number(id), ...payload })
        )
      : storySourcesClient.createStorySource(new CreateStorySourceCommand(payload));

    request
      .then(() => navigate('/story-sources'))
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setSaving(false));
  };

  if (loading) return <LoadingBlock />;

  return (
    <>
      <PageHeader title={isEdit ? 'Sửa nguồn truyện' : 'Thêm nguồn truyện'} />

      <form onSubmit={handleSubmit} className="form-card">
        <ErrorAlert message={error} />

        <div className="form-grid">
          <label>
            Tên truyện <span className="required">*</span>
            <input
              type="text"
              required
              maxLength={500}
              value={form.title}
              onChange={event => setField('title', event.target.value)}
            />
          </label>

          <label>
            Nguồn web <span className="required">*</span>
            <select
              required
              value={form.webSourceId}
              onChange={event => setField('webSourceId', event.target.value)}
            >
              <option value="">— Chọn nguồn web —</option>
              {webSources.map(ws => (
                <option key={ws.id} value={ws.id}>{ws.title}</option>
              ))}
            </select>
          </label>

          <label>
            Tác giả
            <input
              type="text"
              value={form.author}
              onChange={event => setField('author', event.target.value)}
            />
          </label>

          <label>
            Tên Hán Việt
            <input
              type="text"
              value={form.sinoVietnamese}
              onChange={event => setField('sinoVietnamese', event.target.value)}
            />
          </label>

          <label>
            Tổng số chương
            <input
              type="number"
              min="0"
              value={form.totalChapters}
              onChange={event => setField('totalChapters', event.target.value)}
            />
          </label>

          <label>
            Trạng thái truyện ở nguồn
            <input
              type="text"
              value={form.status}
              onChange={event => setField('status', event.target.value)}
              placeholder="VD: Đang ra / Hoàn thành"
            />
          </label>

          <label>
            Link truyện gốc
            <input
              type="url"
              value={form.linkRaw}
              onChange={event => setField('linkRaw', event.target.value)}
              placeholder="https://…"
            />
          </label>

          <label>
            Ảnh bìa (URL)
            <input
              type="url"
              value={form.imageUrl}
              onChange={event => setField('imageUrl', event.target.value)}
              placeholder="https://…"
            />
          </label>
        </div>

        <label>
          Mô tả
          <textarea
            rows={4}
            value={form.description}
            onChange={event => setField('description', event.target.value)}
          />
        </label>

        <fieldset>
          <legend>Thể loại</legend>
          {categories.length === 0 ? (
            <small>Chưa có thể loại nào — tạo ở mục “Thể loại” trước.</small>
          ) : (
            <div className="checkbox-grid">
              {categories.map(cat => (
                <label key={cat.id} className="checkbox-item">
                  <input
                    type="checkbox"
                    checked={categoryIds.includes(cat.id)}
                    onChange={() => toggleCategory(cat.id)}
                  />
                  {cat.title}
                </label>
              ))}
            </div>
          )}
        </fieldset>

        <div className="form-actions">
          <button type="button" className="secondary" onClick={() => navigate('/story-sources')}>
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
