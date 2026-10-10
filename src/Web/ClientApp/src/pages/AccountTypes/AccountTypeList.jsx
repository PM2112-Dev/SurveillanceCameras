import { accountTypesClient } from '../../api/clients';
import { CrudList } from '../../components/common/CrudList';

export function AccountTypeList() {
  return (
    <CrudList
      title="Loại tài khoản"
      description="Phân loại tài khoản theo nền tảng/trang web (TikTok, YouTube, trang truyện…) và trang dùng để đăng nhập lấy cookie."
      basePath="/account-types"
      createLabel="Thêm loại tài khoản"
      deleteTitle="Xoá loại tài khoản"
      columns={[
        { header: 'Tên', render: item => item.title || <em>(không tên)</em> },
        { header: 'Trang đăng nhập', render: item => item.baseUrl || <em>Chưa có</em> },
        { header: 'Cookie đăng nhập', render: item => (item.loginCookieName ? <code>{item.loginCookieName}</code> : '—') },
        { header: 'Trạng thái', render: item => item.baseStatus }
      ]}
      fetchPage={({ search, page, pageSize }) => accountTypesClient.getAccountTypes(search, page, pageSize)}
      deleteItem={item => accountTypesClient.deleteAccountType(item.id)}
    />
  );
}
