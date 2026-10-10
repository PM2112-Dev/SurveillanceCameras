import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Pencil, Plus, Search, Trash2 } from 'lucide-react';
import { getErrorMessage } from '../../api/clients';
import { PageHeader } from './PageHeader';
import { Pagination } from './Pagination';
import { ConfirmDialog } from './ConfirmDialog';
import { EmptyState, ErrorAlert, LoadingBlock } from './StateBlocks';

const PAGE_SIZE = 20;

/**
 * Trang danh sách CRUD dùng chung: tìm kiếm theo tên, bộ lọc dạng select, phân trang, sửa/xoá.
 *
 * - `fetchPage({ search, page, pageSize, filters })` phải trả về PaginatedList của API.
 * - `columns`: `[{ header, render(item) }]`.
 * - `filters`: `[{ name, label, options: [{ value, label }], allLabel? }]` — giá trị rỗng nghĩa là "Tất cả"
 *   (`allLabel` đổi chữ của lựa chọn rỗng, vd. cho bộ chọn sắp xếp).
 * - `rowActions(item)`: nút thao tác thêm hiển thị trước nút Sửa/Xoá.
 * - Bỏ `createLabel` để ẩn nút Thêm; `editable={false}` để ẩn nút Sửa (danh sách chỉ xem/xoá).
 * - `editPath(item)`: đường dẫn nút Sửa khi khác `${basePath}/${id}` (vd. khi đó là trang chi tiết).
 */
export function CrudList({
  title,
  description,
  basePath,
  createLabel,
  emptyMessage,
  searchPlaceholder = 'Tìm theo tên…',
  columns,
  fetchPage,
  deleteItem,
  deleteTitle,
  filters = [],
  initialFilters = {},
  rowActions,
  extraHeader,
  editable = true,
  editPath
}) {
  const [result, setResult] = useState(null);
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [filterValues, setFilterValues] = useState(initialFilters);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [deleteTarget, setDeleteTarget] = useState(null);
  const [deleting, setDeleting] = useState(false);

  const load = useCallback(() => {
    setLoading(true);
    setError('');

    fetchPage({ search: search || undefined, page, pageSize: PAGE_SIZE, filters: filterValues })
      .then(setResult)
      .catch(err => setError(getErrorMessage(err)))
      .finally(() => setLoading(false));
    // fetchPage thường là hàm inline, chỉ nạp lại khi tham số truy vấn đổi.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, search, filterValues]);

  useEffect(load, [load]);

  const handleSearch = event => {
    event.preventDefault();
    setPage(1);
    setSearch(searchInput.trim());
  };

  const handleFilterChange = (name, value) => {
    setPage(1);
    setFilterValues(prev => ({ ...prev, [name]: value }));
  };

  const handleDelete = () => {
    setDeleting(true);

    deleteItem(deleteTarget)
      .then(() => {
        setDeleteTarget(null);
        load();
      })
      .catch(err => {
        setDeleteTarget(null);
        setError(getErrorMessage(err));
      })
      .finally(() => setDeleting(false));
  };

  const items = result?.items ?? [];

  return (
    <>
      <PageHeader
        title={title}
        description={description}
        actions={
          createLabel && (
            <Link to={`${basePath}/new`} role="button">
              <Plus size={16} aria-hidden="true" /> {createLabel}
            </Link>
          )
        }
      />

      {extraHeader}

      <form className="filter-bar" onSubmit={handleSearch}>
        {filters.map(filter => (
          <select
            key={filter.name}
            aria-label={filter.label}
            value={filterValues[filter.name] ?? ''}
            onChange={event => handleFilterChange(filter.name, event.target.value)}
          >
            <option value="">{filter.allLabel ?? `${filter.label}: tất cả`}</option>
            {filter.options.map(option => (
              <option key={option.value} value={option.value}>{option.label}</option>
            ))}
          </select>
        ))}
        <input
          type="search"
          placeholder={searchPlaceholder}
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
        <EmptyState message={emptyMessage ?? (createLabel ? `Chưa có dữ liệu. Bấm “${createLabel}” để tạo mới.` : 'Chưa có dữ liệu.')} />
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                {columns.map(column => (
                  <th key={column.header}>{column.header}</th>
                ))}
                <th className="col-actions">Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => (
                <tr key={item.id}>
                  {columns.map(column => (
                    <td key={column.header}>{column.render(item)}</td>
                  ))}
                  <td className="col-actions">
                    {rowActions?.(item)}
                    {editable && (
                      <Link to={editPath ? editPath(item) : `${basePath}/${item.id}`} className="icon-btn" title="Sửa">
                        <Pencil size={16} aria-hidden="true" />
                      </Link>
                    )}
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
        title={deleteTitle ?? 'Xoá bản ghi'}
        message={`Xoá “${deleteTarget?.title ?? ''}”? Thao tác này không thể hoàn tác.`}
        busy={deleting}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
      />
    </>
  );
}
