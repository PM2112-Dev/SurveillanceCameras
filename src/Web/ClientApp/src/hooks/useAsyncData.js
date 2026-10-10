import { useEffect, useRef, useState } from 'react';
import { getErrorMessage } from '../api/clients';

/**
 * Gọi `loader()` mỗi khi `deps` đổi và chỉ giữ kết quả của lần gọi mới nhất
 * (người dùng chuyển trang nhanh thì response cũ về sau không ghi đè).
 */
export function useAsyncData(loader, deps) {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const latest = useRef(0);

  useEffect(() => {
    const key = ++latest.current;
    setLoading(true);
    setError('');

    loader()
      .then(result => { if (key === latest.current) setData(result); })
      .catch(err => {
        if (key !== latest.current) return;
        setData(null);
        setError(getErrorMessage(err));
      })
      .finally(() => { if (key === latest.current) setLoading(false); });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps);

  return { data, loading, error };
}
