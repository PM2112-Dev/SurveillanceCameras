import { aisClient } from '../../api/clients';
import { CrudList } from '../../components/common/CrudList';
import { AI_PROVIDERS, providerOf } from './aiProviders';

const providerOptions = AI_PROVIDERS.map(provider => ({ value: String(provider.value), label: provider.label }));

export function AIList() {
  return (
    <CrudList
      title="AI"
      description="API key của các nền tảng AI (Gemini, Claude, ChatGPT, DeepSeek, Grok) và model dùng với từng key."
      basePath="/ais"
      createLabel="Thêm AI"
      deleteTitle="Xoá AI"
      filters={[{ name: 'provider', label: 'Nền tảng', options: providerOptions }]}
      columns={[
        { header: 'Tên', render: item => item.title || <em>(không tên)</em> },
        { header: 'Nền tảng', render: item => providerOf(item.provider)?.label ?? '—' },
        { header: 'Model', render: item => (item.model ? <code>{item.model}</code> : '—') },
        {
          header: 'Project / Email',
          render: item => (
            <>
              {item.project || '—'}
              {item.email && <span className="cell-sub">{item.email}</span>}
            </>
          )
        },
        // Server chỉ trả 4 ký tự cuối của key để nhận biết, không trả cả key.
        { header: 'API key', render: item => <code>{item.apiKeyHint ? `••••${item.apiKeyHint}` : '••••'}</code> }
      ]}
      fetchPage={({ search, page, pageSize, filters }) =>
        aisClient.getAIs(search, filters.provider ? Number(filters.provider) : undefined, page, pageSize)}
      deleteItem={item => aisClient.deleteAI(item.id)}
    />
  );
}
