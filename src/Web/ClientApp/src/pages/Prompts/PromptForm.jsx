import { CreatePromptCommand, UpdatePromptCommand } from '../../web-api-client';
import { promptTypesClient, promptsClient } from '../../api/clients';
import { EntityForm, useEntityForm } from '../../components/common/EntityForm';
import { useAsyncData } from '../../hooks/useAsyncData';

export function PromptForm() {
  // Giữ cả bản ghi loại prompt để hiện mô tả "dùng khi nào" của loại đang chọn.
  const promptTypesPage = useAsyncData(() => promptTypesClient.getPromptTypes(undefined, 1, 100), []);
  const promptTypes = promptTypesPage.data?.items ?? [];

  const state = useEntityForm({
    initial: { title: '', promptTypeId: '', content: '' },
    loadEntity: id => promptsClient.getPromptById(id),
    toForm: entity => ({
      title: entity.title ?? '',
      promptTypeId: entity.promptTypeId ? String(entity.promptTypeId) : '',
      content: entity.content ?? ''
    }),
    save: ({ isEdit, id, form }) => {
      const data = { title: form.title, promptTypeId: Number(form.promptTypeId), content: form.content };

      return isEdit
        ? promptsClient.updatePrompt(id, new UpdatePromptCommand({ id, ...data }))
        : promptsClient.createPrompt(new CreatePromptCommand(data));
    },
    backTo: '/prompts'
  });

  const { form, update } = state;
  const selectedType = promptTypes.find(type => String(type.id) === form.promptTypeId);

  return (
    <EntityForm title={state.isEdit ? 'Sửa prompt' : 'Thêm prompt'} state={state}>
      <div className="form-grid">
        <label>
          Tên prompt <span className="required">*</span>
          <input
            type="text"
            required
            maxLength={500}
            value={form.title}
            onChange={event => update({ title: event.target.value })}
            placeholder="VD: Dịch tiên hiệp"
          />
        </label>

        <label>
          Loại prompt <span className="required">*</span>
          <select required value={form.promptTypeId} onChange={event => update({ promptTypeId: event.target.value })}>
            <option value="">— Chọn loại —</option>
            {promptTypes.map(type => (
              <option key={type.id} value={type.id}>{type.title}{type.code ? ` (${type.code})` : ''}</option>
            ))}
          </select>
          {selectedType?.description && <small>{selectedType.description}</small>}
        </label>
      </div>

      <label>
        Nội dung prompt <span className="required">*</span>
        <textarea
          required
          rows={14}
          value={form.content}
          onChange={event => update({ content: event.target.value })}
          placeholder="Nội dung gửi cho AI…"
        />
      </label>
    </EntityForm>
  );
}
