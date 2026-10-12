import { languagesClient } from '../../api/clients';
import { CrudList } from '../../components/common/CrudList';

export function LanguageList() {
  return (
    <CrudList
      title="Ngôn ngữ"
      description="Ngôn ngữ của phim và tập. Khi đồng bộ drama TikTok, hệ thống gán ngôn ngữ theo textLanguage của video và tự thêm mã chưa khai báo."
      basePath="/languages"
      createLabel="Thêm ngôn ngữ"
      searchPlaceholder="Tìm theo tên hoặc mã…"
      deleteTitle="Xoá ngôn ngữ"
      emptyMessage="Chưa có ngôn ngữ nào. Thêm tay, hoặc đồng bộ phim từ TikTok để hệ thống tự thêm."
      columns={[
        { header: 'Tên', render: item => item.title || <em>(không tên)</em> },
        { header: 'Mã', render: item => <code>{item.code}</code> },
        { header: 'Số phim/truyện', render: item => item.workCount ?? 0 },
        { header: 'Số tập', render: item => item.episodeCount ?? 0 }
      ]}
      fetchPage={({ search, page, pageSize }) => languagesClient.getLanguages(search, page, pageSize)}
      deleteItem={item => languagesClient.deleteLanguage(item.id)}
    />
  );
}
