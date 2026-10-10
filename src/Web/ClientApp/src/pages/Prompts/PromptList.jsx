import { promptTypesClient, promptsClient } from '../../api/clients';
import { CrudList } from '../../components/common/CrudList';
import { useOptions } from '../../hooks/useOptions';

export function PromptList() {
  const promptTypes = useOptions(() => promptTypesClient.getPromptTypes(undefined, 1, 100));

  return (
    <CrudList
      title="Prompt"
      description="Nội dung prompt gửi cho AI, nhóm theo loại (tác vụ) dùng prompt."
      basePath="/prompts"
      createLabel="Thêm prompt"
      deleteTitle="Xoá prompt"
      filters={[{ name: 'promptTypeId', label: 'Loại', options: promptTypes }]}
      columns={[
        { header: 'Tên', render: item => item.title || <em>(không tên)</em> },
        { header: 'Loại', render: item => item.promptTypeTitle || '—' },
        {
          header: 'Nội dung',
          render: item => <span className="clamp-2">{item.content || '—'}</span>
        }
      ]}
      fetchPage={({ search, page, pageSize, filters }) =>
        promptsClient.getPrompts(filters.promptTypeId ? Number(filters.promptTypeId) : undefined, search, page, pageSize)}
      deleteItem={item => promptsClient.deletePrompt(item.id)}
    />
  );
}
