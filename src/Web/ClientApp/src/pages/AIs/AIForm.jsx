import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { Eye, EyeOff, ListRestart } from 'lucide-react';
import { CreateAICommand, GetProviderModelsQuery, UpdateAICommand } from '../../web-api-client';
import { aisClient, getErrorMessage } from '../../api/clients';
import { EntityForm, useEntityForm } from '../../components/common/EntityForm';
import { AI_PROVIDERS, providerOf } from './aiProviders';

const initial = { title: '', provider: '1', model: '', apiKey: '', project: '', email: '' };

export function AIForm() {
  const { id } = useParams();
  const [showKey, setShowKey] = useState(false);
  const [apiKeyHint, setApiKeyHint] = useState('');
  const [fetchedModels, setFetchedModels] = useState(null);
  const [modelsLoading, setModelsLoading] = useState(false);
  const [modelsError, setModelsError] = useState('');

  const state = useEntityForm({
    initial,
    loadEntity: id => aisClient.getAIById(id),
    toForm: entity => {
      setApiKeyHint(entity.apiKeyHint ?? '');
      return {
        title: entity.title ?? '',
        provider: String(entity.provider ?? 1),
        model: entity.model ?? '',
        // API key không được trả về; để trống nghĩa là giữ key đang lưu.
        apiKey: '',
        project: entity.project ?? '',
        email: entity.email ?? ''
      };
    },
    save: ({ isEdit, id, form }) => {
      const data = { ...form, provider: Number(form.provider), apiKey: form.apiKey.trim() || undefined };
      return isEdit
        ? aisClient.updateAI(id, new UpdateAICommand({ id, ...data }))
        : aisClient.createAI(new CreateAICommand(data));
    },
    backTo: '/ais'
  });

  const { form, update, isEdit } = state;
  const provider = providerOf(form.provider);
  const modelOptions = fetchedModels ?? provider?.models.map(id => ({ id })) ?? [];
  const canFetchModels = Boolean(form.apiKey.trim() || (isEdit && apiKeyHint));

  const changeProvider = value => {
    update({ provider: value });
    setFetchedModels(null);
    setModelsError('');
  };

  const fetchModels = () => {
    setModelsLoading(true);
    setModelsError('');

    aisClient.getProviderModels(new GetProviderModelsQuery({
      provider: Number(form.provider),
      apiKey: form.apiKey.trim() || undefined,
      // Chưa nhập key mới khi sửa: server dùng key đang lưu của AI này.
      aiId: isEdit && !form.apiKey.trim() ? Number(id) : undefined
    }))
      .then(models => setFetchedModels(models))
      .catch(err => setModelsError(getErrorMessage(err)))
      .finally(() => setModelsLoading(false));
  };

  return (
    <EntityForm title={isEdit ? 'Sửa AI' : 'Thêm AI'} state={state}>
      <div className="form-grid">
        <label>
          Tên <span className="required">*</span>
          <input
            type="text"
            required
            maxLength={500}
            value={form.title}
            onChange={event => update({ title: event.target.value })}
            placeholder="VD: Gemini key #1"
          />
        </label>

        <label>
          Nền tảng <span className="required">*</span>
          <select required value={form.provider} onChange={event => changeProvider(event.target.value)}>
            {AI_PROVIDERS.map(item => <option key={item.value} value={item.value}>{item.label}</option>)}
          </select>
        </label>
      </div>

      <label>
        API key {!isEdit && <span className="required">*</span>}
        <div className="input-with-action">
          <input
            type={showKey ? 'text' : 'password'}
            required={!isEdit}
            autoComplete="off"
            value={form.apiKey}
            onChange={event => update({ apiKey: event.target.value })}
            placeholder={isEdit && apiKeyHint ? `Để trống để giữ key đang lưu (••••${apiKeyHint})` : undefined}
          />
          <button
            type="button"
            className="icon-btn"
            title={showKey ? 'Ẩn API key' : 'Hiện API key'}
            onClick={() => setShowKey(value => !value)}
          >
            {showKey ? <EyeOff size={16} aria-hidden="true" /> : <Eye size={16} aria-hidden="true" />}
          </button>
        </div>
      </label>

      <label>
        Model <span className="required">*</span>
        <div className="input-with-action">
          <input
            type="text"
            required
            list="ai-model-options"
            value={form.model}
            onChange={event => update({ model: event.target.value })}
            placeholder={provider?.models[0] ? `VD: ${provider.models[0]}` : undefined}
          />
          <button
            type="button"
            className="secondary"
            disabled={!canFetchModels || modelsLoading}
            aria-busy={modelsLoading}
            title={canFetchModels ? 'Lấy danh sách model mà API key dùng được' : 'Nhập API key trước'}
            onClick={fetchModels}
          >
            {!modelsLoading && <ListRestart size={16} aria-hidden="true" />} Lấy danh sách model
          </button>
        </div>
        <datalist id="ai-model-options">
          {modelOptions.map(model => <option key={model.id} value={model.id}>{model.displayName}</option>)}
        </datalist>
        <small className={modelsError ? 'error' : undefined}>
          {modelsError
            || (fetchedModels
              ? `Đã lấy ${fetchedModels.length} model từ ${provider?.label} — gõ để lọc.`
              : 'Gợi ý sẵn có thể đã cũ; bấm “Lấy danh sách model” để lấy danh sách mới nhất (cũng là cách kiểm tra key).')}
        </small>
      </label>

      <div className="form-grid">
        <label>
          {provider?.projectLabel ?? 'Project'}
          <input
            type="text"
            maxLength={200}
            value={form.project}
            onChange={event => update({ project: event.target.value })}
            placeholder="Project chứa API key này"
          />
        </label>

        <label>
          Email tài khoản
          <input
            type="email"
            maxLength={320}
            value={form.email}
            onChange={event => update({ email: event.target.value })}
            placeholder="Tài khoản sở hữu API key"
          />
        </label>
      </div>
    </EntityForm>
  );
}
