import { ChevronLeft, ChevronRight } from 'lucide-react';

/** Nhận thẳng shape PaginatedList<T> mà API trả về. */
export function Pagination({ page, totalPages, totalCount, hasPreviousPage, hasNextPage, onChange }) {
  if (!totalCount) return null;

  return (
    <div className="pagination">
      <small>
        Trang {page}/{Math.max(totalPages, 1)} — tổng {totalCount} bản ghi
      </small>

      <div className="pagination-buttons">
        <button
          type="button"
          className="icon-btn"
          disabled={!hasPreviousPage}
          onClick={() => onChange(page - 1)}
          title="Trang trước"
        >
          <ChevronLeft size={18} aria-hidden="true" />
        </button>
        <button
          type="button"
          className="icon-btn"
          disabled={!hasNextPage}
          onClick={() => onChange(page + 1)}
          title="Trang sau"
        >
          <ChevronRight size={18} aria-hidden="true" />
        </button>
      </div>
    </div>
  );
}
