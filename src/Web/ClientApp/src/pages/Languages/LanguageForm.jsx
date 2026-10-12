import { CreateLanguageCommand, UpdateLanguageCommand } from '../../web-api-client';
import { languagesClient } from '../../api/clients';
import { EntityForm, useEntityForm } from '../../components/common/EntityForm';

export function LanguageForm() {
  const state = useEntityForm({
    initial: { title: '', code: '' },
    loadEntity: id => languagesClient.getLanguageById(id),
    toForm: entity => ({ title: entity.title ?? '', code: entity.code ?? '' }),
    save: ({ isEdit, id, form }) =>
      isEdit
        ? languagesClient.updateLanguage(id, new UpdateLanguageCommand({ id, ...form }))
        : languagesClient.createLanguage(new CreateLanguageCommand(form)),
    backTo: '/languages'
  });

  return (
    <EntityForm title={state.isEdit ? 'Sửa ngôn ngữ' : 'Thêm ngôn ngữ'} state={state}>
      <label>
        Tên ngôn ngữ <span className="required">*</span>
        <input
          type="text"
          required
          maxLength={200}
          value={state.form.title}
          onChange={event => state.update({ title: event.target.value })}
          placeholder="VD: Tiếng Anh"
        />
      </label>

      <label>
        Mã <span className="required">*</span>
        <input
          type="text"
          required
          maxLength={20}
          pattern="[A-Za-z0-9_\-]+"
          value={state.form.code}
          onChange={event => state.update({ code: event.target.value })}
          placeholder="VD: en"
        />
        <small>
          Phải trùng với mã TikTok trả về ở thuộc tính textLanguage (vd. en, vi, es) để khi đồng bộ hệ thống nhận ra.
          Đổi mã thì lần đồng bộ sau sẽ tạo ngôn ngữ mới cho mã cũ.
        </small>
      </label>
    </EntityForm>
  );
}
