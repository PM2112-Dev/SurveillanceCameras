import { Link } from 'react-router-dom';
import { languagesClient, moviesClient } from '../../api/clients';
import { useOptions } from '../../hooks/useOptions';
import { CrudList } from '../../components/common/CrudList';
import { formatDuration, numberFormat } from './movieFormat';

// Giá trị của enum MovieSort ở API; rỗng = mới nhất.
const SORT_OPTIONS = [{ value: '1', label: 'Lượt xem: cao → thấp' }];

export function MovieList() {
  const languages = useOptions(() => languagesClient.getLanguages(undefined, 1, 100));

  return (
    <CrudList
      title="Phim"
      description="Phim đồng bộ từ drama của kênh TikTok đã lưu. Vào Kênh đã lưu → biểu tượng phim để chọn drama cần đồng bộ."
      basePath="/movies"
      editable={false}
      emptyMessage="Chưa có phim nào. Vào Kênh đã lưu, mở danh sách drama của kênh TikTok và chọn drama để đồng bộ."
      deleteTitle="Xoá phim"
      filters={[
        { name: 'languageId', label: 'Ngôn ngữ', options: languages },
        { name: 'sort', label: 'Sắp xếp', allLabel: 'Mới đồng bộ trước', options: SORT_OPTIONS }
      ]}
      columns={[
        {
          header: 'Phim',
          render: item => (
            <span className="profile-inline">
              {item.coverUrl && <img src={item.coverUrl} alt="" referrerPolicy="no-referrer" />}
              <span>
                <Link to={`/movies/${item.id}`}>{item.title || <em>(không tên)</em>}</Link>
                {item.isFull && <small className="status-badge ok">Đủ tập</small>}
                {item.isPublished && <small className="status-badge ok">Trên app</small>}
                {item.isVipOnly && <small className="status-badge warn">Phim VIP</small>}
              </span>
            </span>
          )
        },
        { header: 'Kênh', render: item => item.userProfileTitle || '—' },
        { header: 'Ngôn ngữ', render: item => item.languageTitle || item.languageCode || '—' },
        {
          header: 'Lượt xem',
          render: item => (item.viewCount != null ? numberFormat.format(item.viewCount) : '—')
        },
        { header: 'Số tập', render: item => item.episodeCount ?? 0 },
        { header: 'Thời lượng', render: item => formatDuration(item.totalDurationSeconds) }
      ]}
      fetchPage={({ search, page, pageSize, filters }) =>
        moviesClient.getMovies(search, undefined, filters.languageId ? Number(filters.languageId) : undefined,
          Number(filters.sort || 0), page, pageSize)}
      deleteItem={item => moviesClient.deleteMovie(item.id)}
    />
  );
}
