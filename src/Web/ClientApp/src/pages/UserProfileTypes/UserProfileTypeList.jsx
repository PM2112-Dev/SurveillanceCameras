import { userProfileTypesClient } from '../../api/clients';
import { CrudList } from '../../components/common/CrudList';

export function UserProfileTypeList() {
  return (
    <CrudList
      title="Loại kênh"
      description="Nền tảng của kênh đã lưu (TikTok, YouTube…). Mã dùng để hệ thống nhận diện nền tảng."
      basePath="/user-profile-types"
      createLabel="Thêm loại kênh"
      deleteTitle="Xoá loại kênh"
      columns={[
        { header: 'Tên', render: item => item.title || <em>(không tên)</em> },
        { header: 'Mã', render: item => (item.code ? <code>{item.code}</code> : '—') },
        { header: 'Trạng thái', render: item => item.baseStatus }
      ]}
      fetchPage={({ search, page, pageSize }) => userProfileTypesClient.getUserProfileTypes(search, page, pageSize)}
      deleteItem={item => userProfileTypesClient.deleteUserProfileType(item.id)}
    />
  );
}
