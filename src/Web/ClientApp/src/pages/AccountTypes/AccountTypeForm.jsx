import { CreateAccountTypeCommand, UpdateAccountTypeCommand } from '../../web-api-client';
import { accountTypesClient } from '../../api/clients';
import { EntityForm, useEntityForm } from '../../components/common/EntityForm';

const initial = { title: '', baseUrl: '', loginCookieName: '' };

export function AccountTypeForm() {
  const state = useEntityForm({
    initial,
    loadEntity: id => accountTypesClient.getAccountTypeById(id),
    toForm: entity => ({
      title: entity.title ?? '',
      baseUrl: entity.baseUrl ?? '',
      loginCookieName: entity.loginCookieName ?? ''
    }),
    save: ({ isEdit, id, form }) =>
      isEdit
        ? accountTypesClient.updateAccountType(id, new UpdateAccountTypeCommand({ id, ...form }))
        : accountTypesClient.createAccountType(new CreateAccountTypeCommand(form)),
    backTo: '/account-types'
  });

  const { form, update } = state;

  return (
    <EntityForm title={state.isEdit ? 'Sửa loại tài khoản' : 'Thêm loại tài khoản'} state={state}>
      <label>
        Tên loại tài khoản <span className="required">*</span>
        <input
          type="text"
          required
          maxLength={500}
          value={form.title}
          onChange={event => update({ title: event.target.value })}
          placeholder="VD: TikTok"
        />
      </label>

      <label>
        Base URL (trang đăng nhập)
        <input
          type="url"
          maxLength={2000}
          value={form.baseUrl}
          onChange={event => update({ baseUrl: event.target.value })}
          placeholder="VD: https://www.tiktok.com/login"
        />
        <small>
          Bấm “Mở Chrome” ở tài khoản thuộc loại này, máy chủ sẽ mở trang này để đăng nhập và lưu cookie của cả tên miền.
        </small>
      </label>

      <label>
        Cookie nhận biết đã đăng nhập
        <input
          type="text"
          maxLength={200}
          value={form.loginCookieName}
          onChange={event => update({ loginCookieName: event.target.value })}
          placeholder="VD: sessionid"
        />
        <small>
          Cookie chỉ xuất hiện sau khi đăng nhập (TikTok là <code>sessionid</code>). Có thì hệ thống biết lúc bị đăng xuất
          và không ghi đè cookie cũ; bỏ trống thì mọi thay đổi cookie đều được lưu.
        </small>
      </label>
    </EntityForm>
  );
}
