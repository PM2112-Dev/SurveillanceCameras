import { CreateAccountTypeCommand, UpdateAccountTypeCommand } from '../../web-api-client';
import { accountTypesClient } from '../../api/clients';
import { EntityForm, useEntityForm } from '../../components/common/EntityForm';

const initial = { title: '', baseUrl: '', loginCookieName: '' };

/** Nền tảng thường gặp: điền sẵn trang đăng nhập và cookie báo đã đăng nhập. Chỉ là gợi ý, sửa được. */
const presets = [
  { key: 'tiktok', title: 'TikTok', baseUrl: 'https://www.tiktok.com/login', loginCookieName: 'sessionid' },
  { key: 'facebook', title: 'Facebook', baseUrl: 'https://www.facebook.com/login', loginCookieName: 'c_user' },
  { key: 'youtube', title: 'YouTube (Google)', baseUrl: 'https://accounts.google.com/ServiceLogin?service=youtube', loginCookieName: 'SID' },
  { key: 'instagram', title: 'Instagram', baseUrl: 'https://www.instagram.com/accounts/login/', loginCookieName: 'sessionid' },
  { key: 'x', title: 'X (Twitter)', baseUrl: 'https://x.com/i/flow/login', loginCookieName: 'auth_token' }
];

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
      {!state.isEdit && (
        <label>
          Chọn nền tảng có sẵn
          <select value="" onChange={event => {
            const preset = presets.find(p => p.key === event.target.value);
            if (preset) update({ title: preset.title, baseUrl: preset.baseUrl, loginCookieName: preset.loginCookieName });
          }}>
            <option value="">— Tự nhập bên dưới —</option>
            {presets.map(p => <option key={p.key} value={p.key}>{p.title}</option>)}
          </select>
          <small>Điền sẵn trang đăng nhập và cookie nhận biết đã đăng nhập; kiểm tra lại nếu nền tảng đổi cách đăng nhập.</small>
        </label>
      )}

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
          Bấm “Đăng nhập” ở tài khoản thuộc loại này, máy chủ sẽ mở Chrome tại trang này để bạn đăng nhập và lưu cookie của cả tên miền. Nền tảng nào cũng dùng cách này.
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
