import { Link } from 'react-router-dom';
import { useState } from 'react';
import { ExternalLink, Film, RefreshCw } from 'lucide-react';
import { SetChannelAutoSyncRequest } from '../../web-api-client';
import { getErrorMessage, userProfilesClient, userProfileTypesClient } from '../../api/clients';
import { ErrorAlert } from '../../components/common/StateBlocks';
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
  const [reloadKey, setReloadKey] = useState(0);
  const [message, setMessage] = useState('');
  const [error, setError] = useState('');

  const toggleAutoSync = item => {
    setError('');
    userProfilesClient.setChannelAutoSync(item.id, new SetChannelAutoSyncRequest({ enabled: !item.autoSyncMovies }))
      .then(() => setReloadKey(key => key + 1))
      .catch(err => setError(getErrorMessage(err)));
  };

  const checkNow = item => {
    setError('');
    setMessage('');
    userProfilesClient.requestChannelSync(item.id)
      .then(() => setMessage(`Đã đưa kênh ${item.title || item.uniqueId} vào hàng đợi kiểm tra phim mới và tập mới.`))
      .catch(err => setError(getErrorMessage(err)));
  };

  return (
    <>
    <ErrorAlert message={error} />
    {message && <p className="cell-sub">{message}</p>}
    <CrudList
      key={reloadKey}
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
        { header: 'Cập nhật', render: item => (item.lastFetchedAt ? dateFormat.format(item.lastFetchedAt) : '—') },
        {
          header: 'Tự đồng bộ phim',
          render: item => item.userProfileType?.code === 'tiktok' && item.secUid ? (
            <>
              <label className="checkbox-item">
                <input type="checkbox" checked={Boolean(item.autoSyncMovies)} onChange={() => toggleAutoSync(item)} />
                {item.autoSyncMovies ? 'Bật' : 'Tắt'}
              </label>
              {item.moviesSyncedAt && <small className="cell-sub">Kiểm tra lúc {dateFormat.format(item.moviesSyncedAt)}</small>}
              {item.moviesSyncError && <small className="cell-sub error">{item.moviesSyncError}</small>}
            </>
          ) : '—'
        }
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
              <button type="button" className="icon-btn" title="Kiểm tra ngay phim mới và tập mới" onClick={() => checkNow(item)}>
                <RefreshCw size={16} aria-hidden="true" />
              </button>
            )}
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
    </>
  );
}
