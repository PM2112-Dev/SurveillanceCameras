import { useState } from 'react';
import { ChevronLeft, ChevronRight } from 'lucide-react';

/** Phân trang cho dữ liệu lấy từ trang nguồn — tổng số trang có thể không biết (`totalPages` rỗng). */
export function CrawlPager({ page, totalPages, hasPreviousPage, hasNextPage, onChange, disabled }) {
  const [target, setTarget] = useState('');

  if (!hasPreviousPage && !hasNextPage) return null;

  const goTo = event => {
    event.preventDefault();
    const value = Number(target);
    if (value >= 1 && (!totalPages || value <= totalPages)) {
      onChange(value);
      setTarget('');
    }
  };

  return (
    <div className="pagination">
      <small>Trang {page}{totalPages ? `/${totalPages}` : ''}</small>

      <div className="pagination-buttons">
        {totalPages > 2 && (
          <form className="page-jump" onSubmit={goTo}>
            <input
              type="number"
              min={1}
              max={totalPages}
              placeholder="Tới trang"
              aria-label="Tới trang"
              value={target}
              onChange={event => setTarget(event.target.value)}
            />
          </form>
        )}
        <button
          type="button"
          className="icon-btn"
          disabled={disabled || !hasPreviousPage}
          onClick={() => onChange(page - 1)}
          title="Trang trước"
        >
          <ChevronLeft size={18} aria-hidden="true" />
        </button>
        <button
          type="button"
          className="icon-btn"
          disabled={disabled || !hasNextPage}
          onClick={() => onChange(page + 1)}
          title="Trang sau"
        >
          <ChevronRight size={18} aria-hidden="true" />
        </button>
      </div>
    </div>
  );
}

/** Số trang hợp lệ (≥ 1) đọc từ query string. */
export const pageFromParams = (params, name = 'page') => Math.max(1, Number(params.get(name)) || 1);
