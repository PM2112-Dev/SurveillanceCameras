import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Pencil, Plus, Search, Trash2 } from 'lucide-react';
import { webSourcesClient, getErrorMessage } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { Pagination } from '../../components/common/Pagination';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';

const PAGE_SIZE = 20;

export function WebSourceList() {
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

    webSourcesClient.getWebSource(search || undefined, page, PAGE_SIZE)
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

    webSourcesClient.deleteWebSource(deleteTarget.id)
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
        title="Nguồn web"
        description="Các website nguồn dùng để crawl truyện."
        actions={
          <Link to="/web-sources/new" role="button">
            <Plus size={16} aria-hidden="true" /> Thêm nguồn web
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
        <EmptyState message="Chưa có nguồn web nào. Bấm “Thêm nguồn web” để tạo mới." />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tên</th>
                <th>Base URL</th>
                <th>Trạng thái</th>
                <th className="col-actions">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  <td>{item.title || <em>(không tên)</em>}</td>
                  <td>
                    {item.baseUrl ? (
                      <a href={item.baseUrl} target="_blank" rel="noreferrer">{item.baseUrl}</a>
                    ) : '—'}
                  </td>
                  <td>{item.baseStatus}</td>
                  <td className="col-actions">
                    <Link to={`/web-sources/${item.id}`} className="icon-btn" title="Sửa">
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
        title="Xoá nguồn web"
        message={`Xoá “${deleteTarget?.title ?? ''}”? Thao tác này không thể hoàn tác.`}
        busy={deleting}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
      />
    </>
  );
}
