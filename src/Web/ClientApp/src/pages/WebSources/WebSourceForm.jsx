import { CreateWebSourceCommand, UpdateWebSourceCommand } from '../../web-api-client';
import { accountsClient, webSourcesClient } from '../../api/clients';
import { EntityForm, useEntityForm } from '../../components/common/EntityForm';
import { useOptions } from '../../hooks/useOptions';
import { WEB_SOURCE_TYPES } from './webSourceTypes';
import { CRAWL_PRESETS } from './crawlPresets';

/** Lỗi cú pháp JSON của cấu hình (chuỗi rỗng là hợp lệ: nguồn chưa có cấu hình). */
function jsonError(text) {
  if (!text.trim()) return '';
  try {
    JSON.parse(text);
    return '';
  } catch (err) {
    return err.message;
  }
}

const initial = { title: '', baseUrl: '', type: '', isCookie: false, accountId: '', isNeedEdit: true, tytAccountId: '', crawlConfig: '' };

export function WebSourceForm() {
  const accounts = useOptions(() => accountsClient.getAccounts(undefined, undefined, 1, 100));

  const state = useEntityForm({
    initial,
    loadEntity: id => webSourcesClient.getWebSourceById(id),
    toForm: entity => ({
      title: entity.title ?? '',
      baseUrl: entity.baseUrl ?? '',
      type: entity.type ? String(entity.type) : '',
      isCookie: Boolean(entity.isCookie),
      accountId: entity.accountId ? String(entity.accountId) : '',
      isNeedEdit: entity.isNeedEdit ?? true,
      tytAccountId: entity.tytAccountId ? String(entity.tytAccountId) : '',
      crawlConfig: entity.crawlConfig ?? ''
    }),
    save: ({ isEdit, id, form }) => {
      const payload = {
        title: form.title,
        baseUrl: form.baseUrl,
        type: form.type ? Number(form.type) : undefined,
        isCookie: form.isCookie,
        // Không cần cookie thì không gửi tài khoản (server cũng bỏ qua).
        accountId: form.isCookie && form.accountId ? Number(form.accountId) : undefined,
        isNeedEdit: form.isNeedEdit,
        tytAccountId: form.tytAccountId ? Number(form.tytAccountId) : undefined,
        crawlConfig: form.crawlConfig.trim() || undefined
      };

      return isEdit
        ? webSourcesClient.updateWebSource(id, new UpdateWebSourceCommand({ id, ...payload }))
        : webSourcesClient.createWebSource(new CreateWebSourceCommand(payload));
    },
    backTo: '/web-sources'
  });

  const { form, update, isEdit } = state;
  const configError = jsonError(form.crawlConfig);

  const applyPreset = preset => {
    if (form.crawlConfig.trim() && !window.confirm(`Thay cấu hình hiện tại bằng mẫu ${preset.label}?`)) return;
    update({ crawlConfig: JSON.stringify(preset.config, null, 2) });
  };

  const formatConfig = () => {
    if (!configError && form.crawlConfig.trim()) update({ crawlConfig: JSON.stringify(JSON.parse(form.crawlConfig), null, 2) });
  };

  return (
    <EntityForm title={isEdit ? 'Sửa nguồn web' : 'Thêm nguồn web'} state={state}>
      <label>
        Tên nguồn <span className="required">*</span>
        <input
          type="text"
          required
          maxLength={500}
          value={form.title}
          onChange={event => update({ title: event.target.value })}
          placeholder="VD: Wikidich"
        />
      </label>

      <div className="form-grid">
        <label>
          URL trang danh sách truyện <span className="required">*</span>
          <input
            type="url"
            required
            maxLength={2000}
            value={form.baseUrl}
            onChange={event => update({ baseUrl: event.target.value })}
            placeholder="VD: https://wikidichzz.com/danh-sach/truyen-full"
          />
        </label>

        <label>
          Cách crawl
          <select value={form.type} onChange={event => update({ type: event.target.value })}>
            <option value="">— Chưa chọn —</option>
            {WEB_SOURCE_TYPES.map(type => <option key={type.value} value={type.value}>{type.label}</option>)}
          </select>
        </label>
      </div>

      <label className="checkbox-item">
        <input
          type="checkbox"
          checked={form.isCookie}
          onChange={event => update({ isCookie: event.target.checked })}
        />
        Trang cần cookie đăng nhập để gọi API
      </label>

      {form.isCookie && (
        <label>
          Tài khoản cung cấp cookie <span className="required">*</span>
          <select required value={form.accountId} onChange={event => update({ accountId: event.target.value })}>
            <option value="">— Chọn tài khoản —</option>
            {accounts.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
          <small>Cookie được lấy từ tài khoản này mỗi lần crawl, nên cập nhật cookie ở tài khoản là đủ.</small>
        </label>
      )}

      <label className="checkbox-item">
        <input
          type="checkbox"
          checked={form.isNeedEdit}
          onChange={event => update({ isNeedEdit: event.target.checked })}
        />
        Truyện từ nguồn này cần AI biên tập lại
      </label>
      <small>
        Bật với nguồn dịch máy word-by-word (VD: Wikicv) để AI biên tập tên, giới thiệu và nội dung chương.
        Tắt khi nội dung nguồn đã đọc được (VD: Wikidich): truyện giữ nguyên nội dung gốc, không tốn lượt gọi AI.
      </small>

      <label>
        Tài khoản TYT đăng truyện
        <select value={form.tytAccountId} onChange={event => update({ tytAccountId: event.target.value })}>
          <option value="">— Không tự đăng lên TYT —</option>
          {accounts.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
        </select>
        <small>
          Có chọn thì mỗi truyện tạo từ nguồn này được tự động tạo trên TYT bằng tài khoản này và lưu lại ID truyện TYT.
          Tài khoản phải đã đăng nhập TYT (mở phiên đăng nhập ở trang Tài khoản).
        </small>
      </label>

      <fieldset className="crawl-config">
        <legend>Cấu hình crawl</legend>
        <div className="chip-row">
          <small>Chèn mẫu:</small>
          {CRAWL_PRESETS.map(preset => (
            <button key={preset.key} type="button" className="chip-button" title={preset.note} onClick={() => applyPreset(preset)}>
              {preset.label}
            </button>
          ))}
          <button type="button" className="chip-button" disabled={Boolean(configError)} onClick={formatConfig}>
            Định dạng JSON
          </button>
        </div>
        <textarea
          rows={18}
          spellCheck={false}
          value={form.crawlConfig}
          aria-invalid={configError ? 'true' : undefined}
          onChange={event => update({ crawlConfig: event.target.value })}
          placeholder='{ "storyList": { … }, "story": { … }, "chapterList": { … }, "chapter": { … } }'
        />
        <small>
          {configError
            ? `JSON không hợp lệ: ${configError}`
            : 'Selector CSS: "css" lấy chữ, "css@attr" lấy thuộc tính, "@href" lấy trên chính phần tử. '
              + 'Template {…}: biến url, origin, page, offset, pageSize và hàm sha256(), md5(), rotate(). '
              + 'Base URL là trang danh sách truyện. Lưu xong bấm tên nguồn web ở danh sách để xem thử.'}
        </small>
      </fieldset>
    </EntityForm>
  );
}
