import { useParams } from 'react-router-dom';
import { CreateWorkCommand, UpdateWorkCommand } from '../../web-api-client';
import { accountsClient, worksClient, storySourcesClient, storyVideoTypesClient } from '../../api/clients';
import { EntityForm, useEntityForm } from '../../components/common/EntityForm';
import { useOptions } from '../../hooks/useOptions';

const NUMBER_FIELDS = [
  ['chapterUploaded', 'Số chương đã đăng'],
  ['earnCount', 'Mốc kiếm tiền (earnCount)'],
  ['earnNow', 'Hiện tại (earnNow)'],
  ['adCount', 'Số quảng cáo'],
  ['paid', 'Đã thanh toán']
];

const FLAG_FIELDS = [
  ['isFull', 'Đã hoàn thành'],
  ['isPublished', 'Đã đăng'],
  ['isEarning', 'Bật kiếm tiền'],
  ['isComment', 'Cho phép bình luận']
];

// Trùng enum TytUploadMode ở backend (Domain/Enums/TytUploadMode.cs).
const UPLOAD_MODES = [
  [0, 'Đăng tay (bấm nút đăng chương)'],
  [1, 'Đăng ngay khi chương xử lý xong'],
  [2, 'Đăng theo giờ (mỗi N giờ một lô)'],
  [3, 'Đăng theo ngày (một lô vào giờ cố định)']
];

const initial = {
  title: '',
  storySourceId: '',
  accountId: '',
  storyVideoTypeId: '',
  tytStoryId: '',
  author: '',
  description: '',
  isFull: false,
  isPublished: false,
  isEarning: false,
  isComment: true,
  chapterUploaded: 0,
  earnCount: 0,
  earnNow: 0,
  adCount: 0,
  paid: 0,
  tytUploadMode: 0,
  tytUploadBatchSize: 1,
  tytUploadEveryHours: 1,
  tytUploadDailyTime: ''
};

export function StoryForm() {
  const { id } = useParams();
  const storySources = useOptions(() => storySourcesClient.getStorySources(undefined, undefined, 1, 100));
  const accounts = useOptions(() => accountsClient.getAccounts(undefined, undefined, 1, 100));
  const videoTypes = useOptions(() => storyVideoTypesClient.getStoryVideoTypes(undefined, 1, 100));

  const state = useEntityForm({
    initial,
    loadEntity: id => worksClient.getWorkById(id),
    toForm: entity => ({
      ...initial,
      ...Object.fromEntries(Object.keys(initial).map(key => [key, entity[key] ?? initial[key]])),
      storySourceId: String(entity.storySourceId ?? ''),
      accountId: String(entity.accountId ?? ''),
      storyVideoTypeId: String(entity.storyVideoTypeId ?? '')
    }),
    save: ({ isEdit, id, form }) => {
      const { storySourceId, accountId, storyVideoTypeId, ...rest } = form;
      // Tài khoản đăng có thể gắn sau (truyện do AI tạo từ nguồn truyện chưa có tài khoản).
      const account = { accountId: accountId ? Number(accountId) : undefined };
      // Loại video không bắt buộc: chuỗi rỗng nghĩa là bỏ liên kết.
      const videoType = { storyVideoTypeId: storyVideoTypeId ? Number(storyVideoTypeId) : undefined };

      // Nguồn truyện chỉ chọn được lúc tạo (lệnh cập nhật không nhận trường này).
      return isEdit
        ? worksClient.updateWork(id, new UpdateWorkCommand({ id, ...rest, ...videoType, ...account }))
        : worksClient.createWork(new CreateWorkCommand({
            ...rest,
            ...videoType,
            ...account,
            storySourceId: Number(storySourceId)
          }));
    },
    // Sửa mở từ trang chi tiết nên quay lại đó; thêm mới thì về danh sách.
    backTo: id ? `/stories/${id}` : '/stories'
  });

  const { form, update, isEdit } = state;

  return (
    <EntityForm title={isEdit ? 'Sửa truyện' : 'Thêm truyện'} state={state}>
      <label>
        Tên truyện <span className="required">*</span>
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
          Nguồn truyện <span className="required">*</span>
          <select
            required
            disabled={isEdit}
            value={form.storySourceId}
            onChange={event => update({ storySourceId: event.target.value })}
          >
            <option value="">— Chọn nguồn truyện —</option>
            {storySources.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </label>

        <label>
          Tài khoản đăng
          <select
            value={form.accountId}
            onChange={event => update({ accountId: event.target.value })}
          >
            <option value="">— Chưa gắn tài khoản —</option>
            {accounts.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </label>

        <label>
          Loại video
          <select value={form.storyVideoTypeId} onChange={event => update({ storyVideoTypeId: event.target.value })}>
            <option value="">— Không chọn —</option>
            {videoTypes.map(option => <option key={option.value} value={option.value}>{option.label}</option>)}
          </select>
        </label>

        <label>
          Tác giả
          <input type="text" value={form.author} onChange={event => update({ author: event.target.value })} />
        </label>

        <label>
          ID truyện trên TYT
          <input type="text" value={form.tytStoryId} onChange={event => update({ tytStoryId: event.target.value })} />
        </label>
      </div>

      <label>
        Mô tả
        <textarea rows={5} value={form.description} onChange={event => update({ description: event.target.value })} />
      </label>

      <div className="checkbox-grid">
        {FLAG_FIELDS.map(([name, label]) => (
          <label key={name} className="checkbox-item">
            <input
              type="checkbox"
              checked={form[name]}
              onChange={event => update({ [name]: event.target.checked })}
            />
            {label}
          </label>
        ))}
      </div>

      {isEdit && (
        <fieldset className="form-grid">
          <legend>Đăng chương lên TYT</legend>
          <label>
            Khi nào đăng
            <select value={form.tytUploadMode} onChange={event => update({ tytUploadMode: Number(event.target.value) })}>
              {UPLOAD_MODES.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </label>
          {form.tytUploadMode >= 2 && (
            <label>
              Số chương mỗi lượt
              <input type="number" min={1} max={100} value={form.tytUploadBatchSize}
                onChange={event => update({ tytUploadBatchSize: Number(event.target.value) })} />
            </label>
          )}
          {form.tytUploadMode === 2 && (
            <label>
              Cách nhau (giờ)
              <input type="number" min={1} max={168} value={form.tytUploadEveryHours}
                onChange={event => update({ tytUploadEveryHours: Number(event.target.value) })} />
            </label>
          )}
          {form.tytUploadMode === 3 && (
            <label>
              Giờ đăng mỗi ngày
              <input type="time" value={form.tytUploadDailyTime || '08:00'}
                onChange={event => update({ tytUploadDailyTime: event.target.value })} />
            </label>
          )}
          <small className="hint">
            Chỉ áp dụng khi truyện đã có trên TYT. Chương được đăng theo đúng thứ tự, chương nào chưa biên tập xong hoặc đang lỗi thì các chương sau chờ.
          </small>
        </fieldset>
      )}

      <div className="form-grid">
        {NUMBER_FIELDS.map(([name, label]) => (
          <label key={name}>
            {label}
            <input
              type="number"
              min={0}
              value={form[name]}
              onChange={event => update({ [name]: Number(event.target.value) })}
            />
          </label>
        ))}
      </div>
    </EntityForm>
  );
}
