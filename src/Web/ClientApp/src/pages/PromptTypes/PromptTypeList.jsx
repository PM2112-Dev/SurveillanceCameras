import { promptTypesClient } from '../../api/clients';
import { CrudList } from '../../components/common/CrudList';

export function PromptTypeList() {
  return (
    <CrudList
      title="Loại prompt"
      description="Prompt dùng cho tác vụ nào: biên tập tên truyện, giới thiệu, nội dung chương…"
      basePath="/prompt-types"
      createLabel="Thêm loại prompt"
      deleteTitle="Xoá loại prompt"
      columns={[
        { header: 'Tên', render: item => item.title || <em>(không tên)</em> },
        { header: 'Mã tác vụ', render: item => (item.code ? <code>{item.code}</code> : <em>Chưa có mã</em>) },
        { header: 'Dùng khi nào', render: item => <span className="clamp-2">{item.description || '—'}</span> },
        { header: 'Số prompt', render: item => item.promptCount ?? 0 }
      ]}
      fetchPage={({ search, page, pageSize }) => promptTypesClient.getPromptTypes(search, page, pageSize)}
      deleteItem={item => promptTypesClient.deletePromptType(item.id)}
    />
  );
}
