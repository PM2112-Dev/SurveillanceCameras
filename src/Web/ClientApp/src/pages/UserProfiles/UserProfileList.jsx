import { Link } from 'react-router-dom';
import { ExternalLink, Film } from 'lucide-react';
import { userProfilesClient, userProfileTypesClient } from '../../api/clients';
import { CrudList } from '../../components/common/CrudList';
import { useOptions } from '../../hooks/useOptions';

const numberFormat = new Intl.NumberFormat('vi-VN');
const dateFormat = new Intl.DateTimeFormat('vi-VN', { dateStyle: 'short', timeStyle: 'short' });

// Link tới trang kênh theo mã nền tảng; thêm nền tảng mới thì bổ sung ở đây.
const CHANNEL_URL = {
  tiktok: handle => `https://www.tiktok.com/@${handle}`
};

function channelUrl(item) {
  const build = CHANNEL_URL[item.userProfileType?.code];
  return build && item.uniqueId ? build(item.uniqueId) : null;
}

export function UserProfileList() {
  const types = useOptions(() => userProfileTypesClient.getUserProfileTypes(undefined, 1, 100));

  return (
    <CrudList
      title="Kênh đã lưu"
      description="Kênh được lưu tự động mỗi khi tra cứu (vd. từ trang Kênh TikTok của tài khoản)."
      basePath="/user-profiles"
      editable={false}
      emptyMessage="Chưa có kênh nào. Tra cứu một kênh TikTok để hệ thống lưu lại."
      searchPlaceholder="Tìm theo tên kênh hoặc handle…"
      deleteTitle="Xoá kênh đã lưu"
      filters={[{ name: 'userProfileTypeId', label: 'Loại kênh', options: types }]}
      columns={[
        {
          header: 'Kênh',
          render: item => (
            <span className="profile-inline">
              {item.avatarUrl && <img src={item.avatarUrl} alt="" referrerPolicy="no-referrer" />}
              <span>
                {item.title || item.uniqueId || <em>(không tên)</em>}
                {item.uniqueId && <small>@{item.uniqueId}</small>}
              </span>
            </span>
          )
        },
        { header: 'Loại', render: item => item.userProfileType?.title || '—' },
        { header: 'Người theo dõi', render: item => numberFormat.format(item.followerCount ?? 0) },
        { header: 'Video', render: item => numberFormat.format(item.videoCount ?? 0) },
        { header: 'Cập nhật', render: item => (item.lastFetchedAt ? dateFormat.format(item.lastFetchedAt) : '—') }
      ]}
      fetchPage={({ search, page, pageSize, filters }) =>
        userProfilesClient.getUserProfiles(
          search,
          filters.userProfileTypeId ? Number(filters.userProfileTypeId) : undefined,
          undefined,
          page,
          pageSize
        )}
      deleteItem={item => userProfilesClient.deleteUserProfile(item.id)}
      rowActions={item => {
        const url = channelUrl(item);
        return (
          <>
            {item.userProfileType?.code === 'tiktok' && item.secUid && (
              <Link to={`/user-profiles/${item.id}/dramas`} className="icon-btn" title="Drama của kênh — đồng bộ thành phim">
                <Film size={16} aria-hidden="true" />
              </Link>
            )}
            {url && (
              <a href={url} target="_blank" rel="noreferrer" className="icon-btn" title="Mở kênh">
                <ExternalLink size={16} aria-hidden="true" />
              </a>
            )}
          </>
        );
      }}
    />
  );
}
