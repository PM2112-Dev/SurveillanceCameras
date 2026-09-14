import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Pencil, Plus, Search, Trash2 } from 'lucide-react';
import { storySourcesClient, webSourcesClient, getErrorMessage } from '../../api/clients';
import { PageHeader } from '../../components/common/PageHeader';
import { Pagination } from '../../components/common/Pagination';
import { ConfirmDialog } from '../../components/common/ConfirmDialog';
import { EmptyState, ErrorAlert, LoadingBlock } from '../../components/common/StateBlocks';

const PAGE_SIZE = 20;

export function StorySourceList() {
  const [result, setResult] = useState(null);
  const [webSources, setWebSources] = useState([]);
  const [page, setPage] = useState(1);
  const [filterInput, setFilterInput] = useState({ title: '', webSourceId: '' });
  const [filter, setFilter] = useState({ title: '', webSourceId: '' });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);

  useEffect(() => {
    webSourcesClient.getWebSource(undefined, 1, 100)
      .then(res => setWebSources(res.items ?? []))
      .catch(() => setWebSources([]));
  }, []);

  const load = useCallback(() => {
    setLoading(true);
    setError('');

    storySourcesClient.getStorySources(
      filter.webSourceId ? Number(filter.webSourceId) : undefined,
      filter.title || undefined,
      page,
      PAGE_SIZE
    )
      .then(setResult)
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setLoading(false));
  }, [page, filter]);

  useEffect(load, [load]);

  const handleSearch = event => {
    event.preventDefault();
    setPage(1);
    setFilter({ title: filterInput.title.trim(), webSourceId: filterInput.webSourceId });
  };

  const handleDelete = () => {
    setDeleting(true);

    storySourcesClient.deleteStorySource(deleteTarget.id)
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
        title="Nguồn truyện"
        description="Truyện được crawl về từ các nguồn web."
        actions={
          <Link to="/story-sources/new" role="button">
            <Plus size={16} aria-hidden="true" /> Thêm nguồn truyện
          </Link>
        }
      />

      <form className="filter-bar" onSubmit={handleSearch}>
        <input
          type="search"
          placeholder="Tìm theo tên truyện…"
          value={filterInput.title}
          onChange={event => setFilterInput({ ...filterInput, title: event.target.value })}
        />
        <select
          value={filterInput.webSourceId}
          onChange={event => setFilterInput({ ...filterInput, webSourceId: event.target.value })}
        >
          <option value="">Tất cả nguồn web</option>
          {webSources.map(ws => (
            <option key={ws.id} value={ws.id}>{ws.title}</option>
          ))}
        </select>
        <button type="submit">
          <Search size={16} aria-hidden="true" /> Lọc
        </button>
      </form>

      <ErrorAlert message={error} />

      {loading ? (
        <LoadingBlock />
      ) : items.length === 0 ? (
        <EmptyState message="Không có nguồn truyện nào khớp điều kiện." />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Tên truyện</th>
                <th>Tác giả</th>
                <th>Số chương</th>
                <th>Thể loại</th>
                <th>Trạng thái</th>
                <th className="col-actions">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  <td>{item.title || <em>(không tên)</em>}</td>
                  <td>{item.author || '—'}</td>
                  <td>{item.totalChapters ?? '—'}</td>
                  <td>
                    {item.categories?.length
                      ? item.categories.map(c => c.title).join(', ')
                      : '—'}
                  </td>
                  <td>{item.status || item.baseStatus}</td>
                  <td className="col-actions">
                    <Link to={`/story-sources/${item.id}`} className="icon-btn" title="Sửa">
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
        title="Xoá nguồn truyện"
        message={`Xoá “${deleteTarget?.title ?? ''}”? Thao tác này không thể hoàn tác.`}
        busy={deleting}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
      />
    </>
  );
}
