import { createStorageAccount, getStorageAccount, updateStorageAccount } from '../../api/storageAccounts';
import { EntityForm, useEntityForm } from '../../components/common/EntityForm';
import { GB } from './storageFormat';

// Hạn mức mặc định thấp hơn mức miễn phí 10 GB của R2 một chút để không chạm trần.
const initial = {
  title: '', cloudflareAccountId: '', accessKeyId: '', secretAccessKey: '', bucket: '', publicBaseUrl: '',
  quotaGb: '9.5', priority: '0', isEnabled: true
};

export function StorageAccountForm() {
  const state = useEntityForm({
    initial,
    loadEntity: getStorageAccount,
    toForm: entity => ({
      title: entity.title ?? '',
      cloudflareAccountId: entity.cloudflareAccountId ?? '',
      accessKeyId: entity.accessKeyId ?? '',
      // API không trả khoá bí mật; để trống nghĩa là giữ khoá đang lưu.
      secretAccessKey: '',
      bucket: entity.bucket ?? '',
      publicBaseUrl: entity.publicBaseUrl ?? '',
      quotaGb: String(Math.round((entity.quotaBytes / GB) * 100) / 100),
      priority: String(entity.priority ?? 0),
      isEnabled: Boolean(entity.isEnabled)
    }),
    save: ({ isEdit, id, form }) => {
      const { quotaGb, priority, ...rest } = form;
      const account = { ...rest, quotaBytes: Math.round(Number(quotaGb) * GB), priority: Number(priority) || 0 };

      return isEdit ? updateStorageAccount(id, account) : createStorageAccount(account);
    },
    backTo: '/storage-accounts'
  });

  const { form, update, isEdit } = state;

  return (
    <EntityForm title={isEdit ? 'Sửa tài khoản lưu trữ' : 'Thêm tài khoản lưu trữ'} state={state}>
      <label>
        Tên tài khoản <span className="required">*</span>
        <input type="text" required maxLength={500} value={form.title}
          onChange={event => update({ title: event.target.value })} placeholder="VD: R2 số 1" />
      </label>

      <label>
        Account ID của Cloudflare <span className="required">*</span>
        <input type="text" required maxLength={64} value={form.cloudflareAccountId}
          onChange={event => update({ cloudflareAccountId: event.target.value })} />
        <small>Xem ở trang R2 của Cloudflare (mục Account Details).</small>
      </label>

      <label>
        Access Key ID <span className="required">*</span>
        <input type="text" required maxLength={200} value={form.accessKeyId} autoComplete="off"
          onChange={event => update({ accessKeyId: event.target.value })} />
      </label>

      <label>
        Secret Access Key {!isEdit && <span className="required">*</span>}
        <input type="password" required={!isEdit} maxLength={500} value={form.secretAccessKey} autoComplete="new-password"
          onChange={event => update({ secretAccessKey: event.target.value })}
          placeholder={isEdit ? 'Để trống để giữ khoá đang lưu' : ''} />
        <small>
          Tạo ở R2 → Manage API Tokens với quyền <strong>Object Read &amp; Write</strong> cho bucket. Khoá được mã hoá khi
          lưu và không hiển thị lại.
        </small>
      </label>

      <label>
        Bucket <span className="required">*</span>
        <input type="text" required maxLength={63} value={form.bucket}
          onChange={event => update({ bucket: event.target.value })} placeholder="VD: phim-01" />
      </label>

      <label>
        Địa chỉ công khai của bucket <span className="required">*</span>
        <input type="url" required maxLength={500} value={form.publicBaseUrl}
          onChange={event => update({ publicBaseUrl: event.target.value })} placeholder="VD: https://pub-xxxx.r2.dev" />
        <small>
          Bật Public Access cho bucket (r2.dev hoặc domain riêng) rồi dán địa chỉ vào đây — link phát của từng tập được
          ghép từ địa chỉ này.
        </small>
      </label>

      <label>
        Hạn mức (GB) <span className="required">*</span>
        <input type="number" required min="0.01" step="0.01" value={form.quotaGb}
          onChange={event => update({ quotaGb: event.target.value })} />
        <small>
          Đầy tới mức này thì hệ thống chuyển sang tài khoản kế tiếp. Mức miễn phí của R2 là 10 GB, tính như Cloudflare
          (1 GB = 1 tỷ byte).
        </small>
      </label>

      <label>
        Thứ tự
        <input type="number" step="1" value={form.priority} onChange={event => update({ priority: event.target.value })} />
        <small>Tài khoản có số nhỏ hơn được dùng trước.</small>
      </label>

      <label>
        <input type="checkbox" checked={form.isEnabled} onChange={event => update({ isEnabled: event.target.checked })} />
        {' '}Đang bật (nhận video mới)
      </label>
    </EntityForm>
  );
}
