import { Link } from 'react-router-dom';
import { worksClient } from '../../api/clients';
import { CrudList } from '../../components/common/CrudList';

function Flag({ on, label }) {
  return on ? <span className="status-badge ok">{label}</span> : null;
}

export function StoryList() {
  return (
    <CrudList
      title="Truyện"
      description="Truyện tạo từ nguồn truyện (AI biên tập tên/giới thiệu). Bấm tên truyện để xem chương."
      basePath="/stories"
      createLabel="Thêm truyện"
      deleteTitle="Xoá truyện"
      columns={[
        {
          header: 'Tên',
          render: item => <Link to={`/stories/${item.id}`}>{item.title || <em>(không tên)</em>}</Link>
        },
        { header: 'Nguồn truyện', render: item => item.storySource?.title || '—' },
        { header: 'Loại video', render: item => item.storyVideoType?.title || '—' },
        { header: 'Tác giả', render: item => item.author || '—' },
        { header: 'Chương đã đăng', render: item => item.chapterUploaded ?? 0 },
        {
          header: 'Trạng thái',
          render: item => (
            <span className="badge-row">
              <Flag on={item.isFull} label="Hoàn thành" />
              <Flag on={item.isPublished} label="Đã đăng" />
              <Flag on={item.isEarning} label="Kiếm tiền" />
            </span>
          )
        }
      ]}
      fetchPage={({ search, page, pageSize }) => worksClient.getWorks(search, page, pageSize)}
      deleteItem={item => worksClient.deleteWork(item.id)}
      editPath={item => `/stories/${item.id}/edit`}
    />
  );
}
