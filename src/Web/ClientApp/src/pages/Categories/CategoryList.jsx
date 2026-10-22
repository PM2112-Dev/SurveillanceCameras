import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Download, Pencil, Plus, Search, Trash2 } from 'lucide-react';
import { accountsClient, categoriesClient, getErrorMessage } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { Pagination } from '../../components/common/Pagination';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';

const PAGE_SIZE = 20;

export function CategoryList() {
  const [result, setResult] = useState(null);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);
  const [importing, setImporting] = useState(false);
  const [message, setMessage] = useState('');

  // Lấy thể loại từ TYT bằng tài khoản TYT đã đăng nhập (phiên Chrome của tài khoản gọi TYT).
  const importFromTyt = async () => {
    setError('');
    setMessage('');
    setImporting(true);
    try {
      const accounts = await accountsClient.getAccounts(undefined, undefined, 1, 100);
      const tyt = (accounts.items ?? []).filter(item => /tyt/i.test(`${item.title ?? ''} ${item.accountTypeTitle ?? ''}`));
      const candidates = tyt.length > 0 ? tyt : (accounts.items ?? []);
      if (candidates.length === 0) throw new Error('Chưa có tài khoản nào — thêm tài khoản TYT và đăng nhập trước.');
      const choice = candidates.length === 1
        ? candidates[0]
        : candidates[Number(window.prompt(`Chọn tài khoản TYT (nhập số):\n${candidates.map((a, i) => `${i + 1}. ${a.title}`).join('\n')}`, '1')) - 1];
      if (!choice) return;
      const result = await categoriesClient.importTytCategories(choice.id);
      setMessage(`TYT có ${result.total} thể loại: thêm mới ${result.added}, đã có sẵn ${result.existing}.`);
      load();
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setImporting(false);
    }
  };

  const load = useCallback(() => {
    setLoading(true);
    setError('');

    categoriesClient.getCategories(search || undefined, page, PAGE_SIZE)
      .then(setResult)
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setLoading(false));
  }, [page, search]);

  useEffect(load, [load]);

  const handleSearch = event => {
    event.preventDefault();
    setPage(1);
    setSearch(searchInput.trim());
  };

  const handleDelete = () => {
    setDeleting(true);

    categoriesClient.deleteCategory(deleteTarget.id)
      .then(() => {
        setDeleteTarget(null);
        load();
      })
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setDeleting(false));
  };

  const items = result?.items ?? [];

  return (
    <>
      <PageHeader
        title="Thể loại"
        description="Thể loại dùng để phân loại nguồn truyện."
        actions={
          <>
          <button type="button" className="secondary" onClick={importFromTyt} disabled={importing} aria-busy={importing}
            title="Lấy danh sách thể loại của TYT (cần tài khoản TYT đã đăng nhập)">
            <Download size={16} aria-hidden="true" /> Lấy thể loại từ TYT
          </button>
          <Link to="/categories/new" role="button">
            <Plus size={16} aria-hidden="true" /> Thêm thể loại
          </Link>
          </>
        }
      />

      <form className="filter-bar" onSubmit={handleSearch}>
        <input
          type="search"
          placeholder="Tìm theo tên…"
          value={searchInput}
          onChange={event => setSearchInput(event.target.value)}
        />
        <button type="submit">
          <Search size={16} aria-hidden="true" /> Tìm
        </button>
      </form>

      <ErrorAlert message={error} />
      {message && <p className="success" role="status">{message}</p>}

      {loading ? (
        <LoadingBlock />
      ) : items.length === 0 ? (
        <EmptyState message="Chưa có thể loại nào. Bấm “Thêm thể loại” để tạo mới." />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tên</th>
                <th>Số nguồn truyện</th>
                <th>Trạng thái</th>
                <th className="col-actions">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  <td>{item.title || <em>(không tên)</em>}</td>
                  <td>{item.storySources?.length ?? 0}</td>
                  <td>{item.baseStatus}</td>
                  <td className="col-actions">
                    <Link to={`/categories/${item.id}`} className="icon-btn" title="Sửa">
                      <Pencil size={16} aria-hidden="true" />
                    </Link>
                    <button
                      type="button"
                      className="icon-btn danger"
                      title="Xoá"
                      onClick={() => setDeleteTarget(item)}
                    >
                      <Trash2 size={16} aria-hidden="true" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <Pagination
        page={result?.pageNumber ?? page}
        totalPages={result?.totalPages ?? 0}
        totalCount={result?.totalCount ?? 0}
        hasPreviousPage={result?.hasPreviousPage ?? false}
        hasNextPage={result?.hasNextPage ?? false}
        onChange={setPage}
      />

      <ConfirmDialog
        open={Boolean(deleteTarget)}
        title="Xoá thể loại"
        message={`Xoá “${deleteTarget?.title ?? ''}”?`}
        busy={deleting}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
      />
    </>
  );
}
