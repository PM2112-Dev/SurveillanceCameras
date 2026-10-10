import { useParams } from 'react-router-dom';
import { CreateEpisodeCommand, UpdateEpisodeCommand } from '../../web-api-client';
import { episodesClient, promptsClient } from '../../api/clients';
import { EntityForm, useEntityForm } from '../../components/common/EntityForm';
import { useOptions } from '../../hooks/useOptions';

const emptyForm = {
  title: '',
  workId: '',
  promptId: '',
  episodeNumber: 1,
  tytChapterId: '',
  nameRaw: '',
  nameEdit: '',
  contentRaw: '',
  contentEdit: '',
  isUploaded: false,
  isPublished: false
};

/** Thêm/sửa chương của một truyện (/stories/:storyId/chapters/new hoặc …/:id/edit). */
export function ChapterForm() {
  const { storyId, id } = useParams();
  const prompts = useOptions(() => promptsClient.getPrompts(undefined, undefined, 1, 100));

  const state = useEntityForm({
    initial: { ...emptyForm, workId: storyId },
    loadEntity: id => episodesClient.getEpisodeById(id),
    toForm: entity => ({
      ...emptyForm,
      ...Object.fromEntries(Object.keys(emptyForm).map(key => [key, entity[key] ?? emptyForm[key]])),
      workId: String(entity.workId ?? storyId),
      promptId: entity.promptId ? String(entity.promptId) : ''
    }),
    save: ({ isEdit, id, form }) => {
      const data = { ...form, workId: Number(storyId), promptId: form.promptId ? Number(form.promptId) : undefined };

      return isEdit
        ? episodesClient.updateEpisode(id, new UpdateEpisodeCommand({ id, ...data }))
        : episodesClient.createEpisode(new CreateEpisodeCommand(data));
    },
    backTo: id ? `/stories/${storyId}/chapters/${id}` : `/stories/${storyId}`
  });

  const { form, update, isEdit } = state;

  return (
    <EntityForm title={isEdit ? 'Sửa chương' : 'Thêm chương'} state={state}>
      <div className="form-grid">
        <label>
          Prompt đã dùng
          <select value={form.promptId} onChange={event => update({ promptId: event.target.value })}>
            <option value="">— Không chọn —</option>
            {prompts.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </label>

        <label>
          Số chương <span className="required">*</span>
          <input
            type="number"
            required
            min={0}
            value={form.episodeNumber}
            onChange={event => update({ episodeNumber: Number(event.target.value) })}
          />
        </label>

        <label>
          ID chương trên TYT
          <input type="text" value={form.tytChapterId} onChange={event => update({ tytChapterId: event.target.value })} />
        </label>
      </div>

      <label>
        Tiêu đề <span className="required">*</span>
        <input
          type="text"
          required
          maxLength={500}
          value={form.title}
          onChange={event => update({ title: event.target.value })}
        />
      </label>

      <div className="form-grid">
        <label>
          Tên chương (bản gốc)
          <input type="text" value={form.nameRaw} onChange={event => update({ nameRaw: event.target.value })} />
        </label>

        <label>
          Tên chương (đã biên tập)
          <input type="text" value={form.nameEdit} onChange={event => update({ nameEdit: event.target.value })} />
        </label>
      </div>

      <div className="form-grid">
        <label>
          Nội dung gốc
          <textarea rows={14} value={form.contentRaw} onChange={event => update({ contentRaw: event.target.value })} />
        </label>

        <label>
          Nội dung đã biên tập (AI)
          <textarea rows={14} value={form.contentEdit} onChange={event => update({ contentEdit: event.target.value })} />
        </label>
      </div>

      <div className="checkbox-grid">
        <label className="checkbox-item">
          <input type="checkbox" checked={form.isUploaded} onChange={event => update({ isUploaded: event.target.checked })} />
          Đã tải lên TYT
        </label>
        <label className="checkbox-item">
          <input type="checkbox" checked={form.isPublished} onChange={event => update({ isPublished: event.target.checked })} />
          Đã đăng
        </label>
      </div>
    </EntityForm>
  );
}
