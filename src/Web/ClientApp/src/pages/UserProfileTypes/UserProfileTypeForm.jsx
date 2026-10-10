import { CreateUserProfileTypeCommand, UpdateUserProfileTypeCommand } from '../../web-api-client';
import { userProfileTypesClient } from '../../api/clients';
import { EntityForm, useEntityForm } from '../../components/common/EntityForm';

export function UserProfileTypeForm() {
  const state = useEntityForm({
    initial: { title: '', code: '' },
    loadEntity: id => userProfileTypesClient.getUserProfileTypeById(id),
    toForm: entity => ({ title: entity.title ?? '', code: entity.code ?? '' }),
    save: ({ isEdit, id, form }) =>
      isEdit
        ? userProfileTypesClient.updateUserProfileType(id, new UpdateUserProfileTypeCommand({ id, ...form }))
        : userProfileTypesClient.createUserProfileType(new CreateUserProfileTypeCommand(form)),
    backTo: '/user-profile-types'
  });

  return (
    <EntityForm title={state.isEdit ? 'Sửa loại kênh' : 'Thêm loại kênh'} state={state}>
      <label>
        Tên loại kênh <span className="required">*</span>
        <input
          type="text"
          required
          maxLength={500}
          value={state.form.title}
          onChange={event => state.update({ title: event.target.value })}
          placeholder="VD: TikTok"
        />
      </label>

      <label>
        Mã nền tảng <span className="required">*</span>
        <input
          type="text"
          required
          maxLength={100}
          value={state.form.code}
          onChange={event => state.update({ code: event.target.value })}
          placeholder="VD: tiktok"
        />
        <small>
          Hệ thống dùng mã để nhận diện nền tảng (TikTok là <code>tiktok</code>). Đổi mã của loại đang có kênh
          sẽ khiến lần tra cứu sau tạo loại mới.
        </small>
      </label>
    </EntityForm>
  );
}
