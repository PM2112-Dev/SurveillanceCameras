import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Pencil, Plus, Search, Trash2 } from 'lucide-react';
import { categoriesClient, getErrorMessage } from '../../api/clients';
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
          <Link to="/categories/new" role="button">
            <Plus size={16} aria-hidden="true" /> Thêm thể loại
          </Link>
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
