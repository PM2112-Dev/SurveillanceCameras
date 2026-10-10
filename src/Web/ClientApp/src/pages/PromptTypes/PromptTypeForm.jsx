import { CreatePromptTypeCommand, UpdatePromptTypeCommand } from '../../web-api-client';
import { promptTypesClient } from '../../api/clients';
import { EntityForm, useEntityForm } from '../../components/common/EntityForm';
import { PROMPT_TYPE_CODES } from './promptTypeCodes';

const initial = { title: '', code: '', description: '' };

export function PromptTypeForm() {
  const state = useEntityForm({
    initial,
    loadEntity: id => promptTypesClient.getPromptTypeById(id),
    toForm: entity => ({ title: entity.title ?? '', code: entity.code ?? '', description: entity.description ?? '' }),
    save: ({ isEdit, id, form }) =>
      isEdit
        ? promptTypesClient.updatePromptType(id, new UpdatePromptTypeCommand({ id, ...form }))
        : promptTypesClient.createPromptType(new CreatePromptTypeCommand(form)),
    backTo: '/prompt-types'
  });

  const { form, update } = state;

  // Chọn mã chuẩn khi chưa đặt tên thì điền luôn tên gợi ý.
  const changeCode = code => {
    const known = PROMPT_TYPE_CODES.find(item => item.code === code);
    update(known && !form.title ? { code, title: known.label } : { code });
  };

  return (
    <EntityForm title={state.isEdit ? 'Sửa loại prompt' : 'Thêm loại prompt'} state={state}>
      <div className="form-grid">
        <label>
          Mã tác vụ <span className="required">*</span>
          <input
            type="text"
            required
            maxLength={100}
            pattern="[a-zA-Z0-9._\-]+"
            list="prompt-type-codes"
            value={form.code}
            onChange={event => changeCode(event.target.value)}
            placeholder="VD: edit-description"
          />
          <datalist id="prompt-type-codes">
            {PROMPT_TYPE_CODES.map(item => <option key={item.code} value={item.code}>{item.label}</option>)}
          </datalist>
          <small>Hệ thống chọn prompt theo mã này, nên dùng đúng mã có sẵn cho tác vụ tương ứng.</small>
        </label>

        <label>
          Tên loại prompt <span className="required">*</span>
          <input
            type="text"
            required
            maxLength={500}
            value={form.title}
            onChange={event => update({ title: event.target.value })}
            placeholder="VD: Biên tập giới thiệu truyện"
          />
        </label>
      </div>

      <label>
        Dùng khi nào
        <textarea
          rows={4}
          maxLength={2000}
          value={form.description}
          onChange={event => update({ description: event.target.value })}
          placeholder="VD: Viết lại phần giới thiệu truyện (bản dịch máy) cho tự nhiên, hấp dẫn trước khi đăng lên TYT."
        />
      </label>
    </EntityForm>
  );
}
