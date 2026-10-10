import { useEffect, useState } from 'react';

/**
 * Tải danh sách lựa chọn (tối đa 100 bản ghi đầu) cho ô select.
 * `loader` trả về PaginatedList; mỗi item được đổi thành `{ value, label }`.
 * Lỗi được bỏ qua có chủ đích: thiếu danh sách lựa chọn không được chặn cả trang.
 */
export function useOptions(loader, labelOf = item => item.title) {
  const [options, setOptions] = useState([]);

  useEffect(() => {
    loader()
      .then(result =>
        setOptions((result.items ?? []).map(item => ({ value: String(item.id), label: labelOf(item) || `#${item.id}` })))
      )
      .catch(() => {});
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return options;
}
