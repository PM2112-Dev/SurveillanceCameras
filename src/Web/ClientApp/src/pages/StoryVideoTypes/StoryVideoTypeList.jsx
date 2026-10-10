import { storyVideoTypesClient } from '../../api/clients';
import { CrudList } from '../../components/common/CrudList';

export function StoryVideoTypeList() {
  return (
    <CrudList
      title="Loại video truyện"
      description="Phân loại truyện theo kiểu video sẽ sản xuất (audio TTS, video ngắn…)."
      basePath="/story-video-types"
      createLabel="Thêm loại video"
      deleteTitle="Xoá loại video"
      columns={[
        { header: 'Tên', render: item => item.title || <em>(không tên)</em> },
        { header: 'Mã', render: item => item.code || '—' },
        { header: 'Trạng thái', render: item => item.baseStatus }
      ]}
      fetchPage={({ search, page, pageSize }) => storyVideoTypesClient.getStoryVideoTypes(search, page, pageSize)}
      deleteItem={item => storyVideoTypesClient.deleteStoryVideoType(item.id)}
    />
  );
}
